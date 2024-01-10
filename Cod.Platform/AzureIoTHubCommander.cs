using System.Text;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace Cod.Platform
{
    public class AzureIoTHubCommander : IIoTCommander
    {
        private const int MaxRetry = 5;
        private const string DirectMethodName = "Execute";
        private static readonly TimeSpan DefaultDirectMethodTimeout = TimeSpan.FromSeconds(5);
        private const string JSONContentType = "application/json";
        private const string DefaultContentEncoding = "utf-8";
        private readonly ServiceClient serviceClient;
        private readonly ILogger logger;

        public AzureIoTHubCommander(IConfigurationProvider configuration, ILogger logger)
        {
            var conn = configuration.GetSettingAsString("IOT_HUB_CONN");
            this.serviceClient = ServiceClient.CreateFromConnectionString(conn);
            this.logger = logger;
        }

        public async Task<IoTCommandResult> ExecuteAsync(string device, object command, bool fireAndForget = true)
        {
            var msg = command is string str ? str : JsonSerializer.SerializeObject(command);
            if (!fireAndForget)
            {
                await this.SendCloudToDeviceMessageAsync(device, msg);
                return null;
            }

            return await this.ExecuteDirectMethodAsync(device, msg);
        }

        protected virtual Task OnDeviceOfflineAsync(string device) => Task.CompletedTask;

        private async Task<IoTCommandResult> ExecuteDirectMethodAsync(string device, string message)
        {
            var methodInvocation = new CloudToDeviceMethod(DirectMethodName)
            {
                ResponseTimeout = DefaultDirectMethodTimeout,
            }.SetPayloadJson(message);

            for (var i = 0; i < MaxRetry; i++)
            {
                try
                {
                    var response = await this.serviceClient.InvokeDeviceMethodAsync(device, methodInvocation);
                    return new IoTCommandResult
                    {
                        Status = response.Status,
                        ExecutedAt = DateTime.UtcNow,
                        PayloadJSON = response.GetPayloadAsJson(),
                    };
                }
                catch (DeviceNotFoundException)
                {
                    this.logger.LogWarning($"Device not found: {device}");
                }
                catch (IotHubException e)
                {
                    // REMARK (5he11) Timed out waiting for the response from device.
                    if (e.Message.Contains("504101"))
                    {
                        if (i == MaxRetry - 1)
                        {
                            await this.OnDeviceOfflineAsync(device);
                        }
                        else
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                catch (Exception e)
                {
                    this.logger.LogError(e, $"Failed to deliver {message} to {device}: {e.Message}");
                    await Task.Delay(100);
                }
            }

            return null;
        }

        private async Task SendCloudToDeviceMessageAsync(string device, string msg)
        {
            using var c2dmessage = new Message(Encoding.UTF8.GetBytes(msg))
            {
                ContentType = JSONContentType,
                ContentEncoding = DefaultContentEncoding,
                CreationTimeUtc = DateTime.UtcNow,
            };
            try
            {
                await this.serviceClient.SendAsync(device, c2dmessage);
            }
            catch (UnauthorizedException)
            {
                this.logger.LogError($"Unauthorized exception while sending {msg} to {device}");
                throw;
            }
            catch (DeviceNotFoundException)
            {
                this.logger.LogWarning($"Device not found: {device}");
            }
            catch (DeviceMaximumQueueDepthExceededException)
            {
                // REMARK (5he11) Needs RegistryWrite permission at here
                _ = await this.serviceClient.PurgeMessageQueueAsync(device);
                await this.serviceClient.SendAsync(device, c2dmessage);
            }
        }
    }
}
