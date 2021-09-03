using Cod;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cod.Channel.IoT
{
    class X509IoTHubDevice : IDevice
    {
        private static readonly TimeSpan interval = TimeSpan.FromMilliseconds(500);
        private readonly string provisioningEndpoint;
        private readonly string provisioningIDScope;
        private string assignedHub;
        private string id;
        private string pfxCertificatePath;
        private string pfxCertificatePassword;
        private string secondaryPFXCertificatePath;
        private string secondaryPFXCertificatePassword;
        private readonly ILogger logger;
        private readonly ConcurrentQueue<ITimestampable> events = new ConcurrentQueue<ITimestampable>();
        private readonly SemaphoreSlim initSemaphore = new SemaphoreSlim(1, 1);
        private volatile DeviceClient deviceClient;
        protected DeviceClient DeviceClient => this.deviceClient;
        private volatile ConnectionStatus connectionStatus = ConnectionStatus.Disconnected;
        private CancellationTokenSource sendingTaskCancellation;
        private Task sendingTask;
        private bool disposed;

        private bool IsDeviceConnected => this.Status == DeviceConnectionStatus.Connected;

        public DeviceConnectionStatus Status { get => (DeviceConnectionStatus)(int)this.connectionStatus; }

        public X509IoTHubDevice(
            string provisioningEndpoint,
            string provisioningIDScope,
            string assignedHub,
            string pfxCertificatePath,
            string pfxCertificatePassword,
            string secondaryPFXCertificatePath,
            string secondaryPFXCertificatePassword,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(provisioningEndpoint))
            {
                throw new ArgumentException($"'{nameof(provisioningEndpoint)}' cannot be null or whitespace.", nameof(provisioningEndpoint));
            }

            if (string.IsNullOrWhiteSpace(provisioningIDScope))
            {
                throw new ArgumentException($"'{nameof(provisioningIDScope)}' cannot be null or whitespace.", nameof(provisioningIDScope));
            }

            if (string.IsNullOrWhiteSpace(pfxCertificatePath))
            {
                throw new ArgumentException($"'{nameof(pfxCertificatePath)}' cannot be null or whitespace.", nameof(pfxCertificatePath));
            }

            if (string.IsNullOrWhiteSpace(pfxCertificatePassword))
            {
                throw new ArgumentException($"'{nameof(pfxCertificatePassword)}' cannot be null or whitespace.", nameof(pfxCertificatePassword));
            }

            this.pfxCertificatePath = pfxCertificatePath;
            this.pfxCertificatePassword = pfxCertificatePassword;
            this.secondaryPFXCertificatePath = secondaryPFXCertificatePath;
            this.secondaryPFXCertificatePassword = secondaryPFXCertificatePassword;
            this.logger = logger;
            this.provisioningEndpoint = provisioningEndpoint;
            this.provisioningIDScope = provisioningIDScope;
            this.assignedHub = assignedHub;
        }

        public async Task ConnectAsync()
        {
            if (ShouldClientBeInitialized(this.connectionStatus))
            {
                // Allow a single thread to dispose and initialize the client instance.
                await this.initSemaphore.WaitAsync();
                try
                {
                    if (ShouldClientBeInitialized(this.connectionStatus))
                    {
                        this.logger.LogTrace($"Attempting to initialize the client instance, current status={this.connectionStatus}");
                        await this.DisconnectAsync();

                        if (this.assignedHub == null)
                        {
                            await this.ProvisioningAsync();
                        }

                        using var certificate = this.LoadCertificate();
                        var auth = new DeviceAuthenticationWithX509Certificate(this.id, certificate);
                        this.deviceClient = DeviceClient.Create(this.assignedHub, auth, TransportType.Mqtt);
                        this.DeviceClient.SetConnectionStatusChangesHandler(ConnectionStatusChangeHandler);
                        await this.DeviceClient.SetReceiveMessageHandlerAsync(ReceiveAsync, this.DeviceClient);
                        this.logger.LogTrace($"Initialized the client instance.");

                        await this.RegisterDirectMethodsAsync();

                        this.sendingTaskCancellation = new CancellationTokenSource();
                        this.sendingTask = this.SendCoreAsync(this.sendingTaskCancellation.Token);
                    }
                }
                finally
                {
                    this.initSemaphore.Release();
                }
            }

            try
            {
                // Force connection now.
                // OpenAsync() is an idempotent call, it has the same effect if called once or multiple times on the same client.
                await this.DeviceClient.OpenAsync();
                this.logger.LogTrace($"Opened the client instance.");
            }
            catch (UnauthorizedException)
            {
                // Handled by the ConnectionStatusChangeHandler
            }
        }

        public Task SendAsync(ITimestampable data)
        {
            this.events.Enqueue(data);
            return Task.CompletedTask;
        }

        protected async Task DisconnectAsync()
        {
            if (this.sendingTaskCancellation != null)
            {
                using (this.sendingTaskCancellation)
                {
                    this.sendingTaskCancellation.Cancel();
                    Task.WaitAll(new[] { this.sendingTask }, TimeSpan.FromSeconds(5));
                }

                this.sendingTaskCancellation = null;
                this.sendingTask = null;
            }

            // If the device client instance has been previously initialized, then dispose it.
            if (this.DeviceClient != null)
            {
                using (DeviceClient)
                {
                    if (this.IsDeviceConnected)
                    {
                        await this.UnregisterDirectMethodsAsync();
                        await this.DeviceClient.SetReceiveMessageHandlerAsync(null, null);
                        await this.DeviceClient.CloseAsync();
                    }
                }

                this.deviceClient = null;
            }
        }

        protected virtual Task RegisterDirectMethodsAsync() => Task.CompletedTask;
        protected virtual Task UnregisterDirectMethodsAsync() => Task.CompletedTask;

        protected virtual async Task<string> ProvisioningAsync()
        {
            using var certificate = this.LoadCertificate();
            using var security = new SecurityProviderX509Certificate(certificate);
            using var transport = new ProvisioningTransportHandlerMqtt();
            var provClient = ProvisioningDeviceClient.Create(this.provisioningEndpoint, this.provisioningIDScope, security, transport);
            var result = await provClient.RegisterAsync();

            if (result.Status != ProvisioningRegistrationStatusType.Assigned)
            {
                this.logger.LogError($"Provisioning failed with status {result.Status}.");
                return null;
            }

            this.logger.LogInformation($"Device {result.DeviceId} provisioning to {result.AssignedHub}.");
            this.assignedHub = result.AssignedHub;
            return result.AssignedHub;
        }

        protected virtual Task SaveAsync() => Task.CompletedTask;

        protected async Task SendCoreAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (this.IsDeviceConnected && this.events.Count > 0)
                {
                    ITimestampable data = null;
                    try
                    {
                        if (this.events.TryDequeue(out data))
                        {
                            data.SetTimestamp(DateTimeOffset.UtcNow);
                            var json = JsonSerializer.SerializeObject(data);
                            using var message = new Message(Encoding.UTF8.GetBytes(json))
                            {
                                ContentEncoding = "utf-8",
                                ContentType = "application/json",
                            };

                            await this.DeviceClient.SendEventAsync(message);
                            await this.SaveAsync();
                            continue;
                        }
                    }
                    catch (IotHubException ex) when (ex.IsTransient)
                    {
                        // Inspect the exception to figure out if operation should be retried, or if user-input is required.
                        this.logger.LogWarning($"An IotHubException was caught, but will try to recover and retry: {ex}");
                    }
                    catch (Exception ex) when (ExceptionHelper.IsNetworkExceptionChain(ex))
                    {
                        this.logger.LogWarning($"A network related exception was caught, but will try to recover and retry: {ex}");
                    }
                    catch (Exception ex)
                    {
                        this.logger.LogError($"Unexpected error {ex}");
                    }

                    if (data != null)
                    {
                        this.events.Enqueue(data);
                        await this.SaveAsync();
                    }

                    // wait and retry
                    await Task.Delay(interval);
                }
                else
                {
                    await Task.Delay(interval);
                }
            }
        }

        protected async virtual Task<MethodResponse> DirectMethodAsync(MethodRequest methodRequest, object userContext)
        {
            Console.WriteLine($"\t *** {methodRequest.Name} was called with parameter {methodRequest.DataAsJson}.");
            var retValue = new MethodResponse(Encoding.UTF8.GetBytes(methodRequest.DataAsJson), 200);
            await Task.Delay(5000);
            return retValue;
        }

        protected async virtual Task ReceiveAsync(Message receivedMessage, object _)
        {
            using (receivedMessage)
            {
                this.logger.LogTrace($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
                await this.DeviceClient.CompleteAsync(receivedMessage);
                this.logger.LogTrace($"{DateTime.Now}> Completed C2D message with Id={receivedMessage.MessageId}.");
            }
        }

        // It is not good practice to have async void methods, however, DeviceClient.SetConnectionStatusChangesHandler() event handler signature has a void return type.
        // As a result, any operation within this block will be executed unmonitored on another thread.
        // To prevent multi-threaded synchronization issues, the async method InitializeClientAsync being called in here first grabs a lock
        // before attempting to initialize or dispose the device client instance.
        protected async virtual void ConnectionStatusChangeHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            this.logger.LogInformation($"Connection status changed: status={status}, reason={reason}");
            this.connectionStatus = status;

            switch (status)
            {
                case ConnectionStatus.Connected:
                    this.logger.LogTrace("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");
                    break;

                case ConnectionStatus.Disconnected_Retrying:
                    this.logger.LogTrace("### The DeviceClient is retrying based on the retry policy. Do NOT close or open the DeviceClient instance");
                    break;

                case ConnectionStatus.Disabled:
                    this.logger.LogTrace("### The DeviceClient has been closed gracefully." +
                        "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                    break;

                case ConnectionStatus.Disconnected:
                    switch (reason)
                    {
                        case ConnectionStatusChangeReason.Bad_Credential:
                            // When getting this reason, the current certificate being used is not valid.
                            // If we had a backup, we can try using that.
                            if (!String.IsNullOrWhiteSpace(this.secondaryPFXCertificatePath) && !String.IsNullOrWhiteSpace(this.secondaryPFXCertificatePassword))
                            {
                                var swapPfx = this.pfxCertificatePath;
                                var swapPassword = this.pfxCertificatePassword;

                                this.logger.LogWarning($"The current connection string is invalid. Trying another.");
                                this.pfxCertificatePath = this.secondaryPFXCertificatePath;
                                this.pfxCertificatePassword = this.secondaryPFXCertificatePassword;
                                this.secondaryPFXCertificatePath = swapPfx;
                                this.secondaryPFXCertificatePassword = swapPassword;
                            }

                            this.logger.LogWarning("### The supplied credentials are invalid. Update the parameters and run again.");
                            await this.ConnectAsync();
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            this.logger.LogWarning("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            this.logger.LogWarning("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await this.ConnectAsync();
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            this.logger.LogWarning("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");

                            await this.ConnectAsync();
                            break;

                        default:
                            this.logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                            break;

                    }

                    break;

                default:
                    this.logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                    break;
            }
        }

        private X509Certificate2 LoadCertificate()
        {
            var certificate = LoadProvisioningCertificate(this.pfxCertificatePath, this.pfxCertificatePassword);

            if (this.id == null)
            {
                this.id = certificate.GetNameInfo(X509NameType.SimpleName, false);
            }

            return certificate;
        }

        private static X509Certificate2 LoadProvisioningCertificate(string pfxCertificatePath, string pfxCertificatePassword)
        {
            var certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(pfxCertificatePath, pfxCertificatePassword, X509KeyStorageFlags.UserKeySet);

            X509Certificate2 certificate = null;

            foreach (X509Certificate2 element in certificateCollection)
            {
                Console.WriteLine($"Found certificate: {element?.Thumbprint} {element?.Subject}; PrivateKey: {element?.HasPrivateKey}");
                if (certificate == null && element.HasPrivateKey)
                {
                    certificate = element;
                }
                else
                {
                    element.Dispose();
                }
            }

            if (certificate == null)
            {
                throw new FileNotFoundException($"{pfxCertificatePath} did not contain any certificate with a private key.");
            }

            Console.WriteLine($"Using certificate {certificate.Thumbprint} {certificate.Subject}");

            return certificate;
        }

        // If the client reports Connected status, it is already in operational state.
        // If the client reports Disconnected_retrying status, it is trying to recover its connection.
        // If the client reports Disconnected status, you will need to dispose and recreate the client.
        // If the client reports Disabled status, you will need to dispose and recreate the client.
        private static bool ShouldClientBeInitialized(ConnectionStatus connectionStatus)
        {
            return connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Disabled;
        }

        public async ValueTask DisposeAsync()
        {
            if (!this.disposed)
            {
                await this.DisconnectAsync();
            }

            this.disposed = true;
        }
    }
}
