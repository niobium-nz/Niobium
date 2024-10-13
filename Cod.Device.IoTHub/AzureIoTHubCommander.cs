using Cod.Storage.Messaging;
using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Cod.Device.IoTHub
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
            string conn = configuration.GetSettingAsString("IOT_HUB_CONN");
            serviceClient = ServiceClient.CreateFromConnectionString(conn);
            this.logger = logger;
        }

        public async Task<IoTCommandResult> ExecuteAsync(string device, object command, bool fireAndForget = true)
        {
            string msg = command is string str ? str : JsonSerializer.SerializeObject(command);
            if (fireAndForget)
            {
                await SendCloudToDeviceMessageAsync(device, msg);
                return null;
            }

            return await ExecuteDirectMethodAsync(device, msg);
        }

        protected virtual Task OnDeviceOfflineAsync(string device)
        {
            return Task.CompletedTask;
        }

        private async Task<IoTCommandResult> ExecuteDirectMethodAsync(string device, string message)
        {
            CloudToDeviceMethod methodInvocation = new CloudToDeviceMethod(DirectMethodName)
            {
                ResponseTimeout = DefaultDirectMethodTimeout,
            }.SetPayloadJson(message);

            for (int i = 0; i < MaxRetry; i++)
            {
                try
                {
                    CloudToDeviceMethodResult response = await serviceClient.InvokeDeviceMethodAsync(device, methodInvocation);
                    return new IoTCommandResult
                    {
                        Status = response.Status,
                        ExecutedAt = DateTime.UtcNow,
                        PayloadJSON = response.GetPayloadAsJson(),
                    };
                }
                catch (DeviceNotFoundException)
                {
                    logger.LogWarning($"Device not found: {device}");
                }
                catch (IotHubException e)
                {
                    // REMARK (5he11) Timed out waiting for the response from device.
                    if (e.Message.Contains("504101"))
                    {
                        if (i == MaxRetry - 1)
                        {
                            await OnDeviceOfflineAsync(device);
                        }
                        else
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e, $"Failed to deliver {message} to {device}: {e.Message}");
                    await Task.Delay(100);
                }
            }

            return null;
        }

        private async Task SendCloudToDeviceMessageAsync(string device, string msg)
        {
            using Message c2dmessage = new(Encoding.UTF8.GetBytes(msg))
            {
                ContentType = JSONContentType,
                ContentEncoding = DefaultContentEncoding,
                CreationTimeUtc = DateTime.UtcNow,
            };
            try
            {
                await serviceClient.SendAsync(device, c2dmessage);
            }
            catch (UnauthorizedException)
            {
                logger.LogError($"Unauthorized exception while sending {msg} to {device}");
                throw;
            }
            catch (DeviceNotFoundException)
            {
                logger.LogWarning($"Device not found: {device}");
            }
            catch (DeviceMaximumQueueDepthExceededException)
            {
                // REMARK (5he11) Needs RegistryWrite permission at here
                await serviceClient.PurgeMessageQueueAsync(device);
                await serviceClient.SendAsync(device, c2dmessage);
            }
        }
    }
}
