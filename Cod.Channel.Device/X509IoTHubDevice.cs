using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Provisioning.Client;
using Microsoft.Azure.Devices.Provisioning.Client.Transport;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Logging;

namespace Cod.Channel.Device
{
    public class X509IoTHubDevice : IDevice
    {
        private static readonly TimeSpan interval = TimeSpan.FromMilliseconds(500);
        private readonly string provisioningEndpoint;
        private readonly string provisioningIDScope;
        private string id;
        private string pfxCertificatePath;
        private string pfxCertificatePassword;
        private string secondaryPFXCertificatePath;
        private string secondaryPFXCertificatePassword;
        private readonly ILogger logger;
        private readonly SemaphoreSlim initSemaphore = new(1, 1);
        private volatile DeviceClient deviceClient;
        private volatile ConnectionStatus connectionStatus = ConnectionStatus.Disconnected;
        private CancellationTokenSource sendingTaskCancellation;
        private CancellationTokenSource ensureConnectivityTaskCancellation;
        private Task sendingTask;
        private Task ensureConnectivityTask;
        private long lastTwinVersion = long.MinValue;
        private bool disposed;

        protected ConcurrentQueue<ITimestampable> Events { get; set; } = new ConcurrentQueue<ITimestampable>();

        protected bool IsDeviceConnected => this.Status == DeviceConnectionStatus.Connected;

        protected string AssignedHub { get; private set; }

        protected DeviceClient DeviceClient => this.deviceClient;

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
            this.AssignedHub = assignedHub;
        }

        public async Task ConnectAsync() => await ConnectAsync(CancellationToken.None);

        public async Task ConnectAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (ShouldClientBeInitialized(this.connectionStatus) && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        // Allow a single thread to dispose and initialize the client instance.
                        await this.initSemaphore.WaitAsync(cancellationToken);

                        if (ShouldClientBeInitialized(this.connectionStatus) && !cancellationToken.IsCancellationRequested)
                        {
                            this.logger.LogInformation($"Attempting to initialize the client instance, current status={this.connectionStatus}");

                            if (this.AssignedHub == null)
                            {
                                await this.ProvisioningAsync(cancellationToken);
                            }

                            if (this.AssignedHub != null)
                            {
                                await this.DisconnectAsync(false);
                                using var certificate = this.LoadCertificate();
                                var auth = new DeviceAuthenticationWithX509Certificate(this.id, certificate);
                                this.deviceClient = DeviceClient.Create(this.AssignedHub, auth, TransportType.Mqtt);
                                this.DeviceClient.SetConnectionStatusChangesHandler(this.ConnectionStatusChangeHandler);

                                // Force connection now.
                                // OpenAsync() is an idempotent call, it has the same effect if called once or multiple times on the same client.
                                await this.DeviceClient.OpenAsync(cancellationToken);
                                this.logger.LogInformation($"Opened the client instance.");
                                return;
                            }
                        }
                    }
                    catch (UnauthorizedException)
                    {
                        // Handled by the ConnectionStatusChangeHandler
                    }
                    catch (Exception e)
                    {
                        this.logger.LogError(e, e.Message);
                    }
                    finally
                    {
                        this.initSemaphore.Release();
                    }
                }
            }
        }

        public async Task SendAsync(ITimestampable data) => await SendAsync(data, CancellationToken.None);

        public async Task SendAsync(ITimestampable data, CancellationToken cancellationToken)
        {
            this.Events.Enqueue(data);
            await this.SaveAsync(cancellationToken);
        }

        public async Task ReportPropertyChangesAsync(IReadOnlyDictionary<string, object> properties) => await this.UpdateReportedPropertiesAsync(properties);

        protected virtual async Task UpdateReportedPropertiesAsync(IReadOnlyDictionary<string, object> reportedProperties)
        {
            var properties = new TwinCollection();
            foreach (var key in reportedProperties.Keys)
            {
                properties[key] = reportedProperties[key];
            }

            await this.DeviceClient.UpdateReportedPropertiesAsync(properties);
        }

        protected async Task DisconnectAsync(bool cancelAutoReconnection)
        {
            if (cancelAutoReconnection)
            {
                this.CancelTask(this.ensureConnectivityTaskCancellation, this.ensureConnectivityTask);
                this.ensureConnectivityTaskCancellation = null;
                this.ensureConnectivityTask = null;
            }

            this.CancelTask(this.sendingTaskCancellation, this.sendingTask);
            this.sendingTaskCancellation = null;
            this.sendingTask = null;

            // If the device client instance has been previously initialized, then dispose it.
            if (this.DeviceClient != null)
            {
                this.logger.LogInformation($"Previous deivce client in place, disposing...");
                using (DeviceClient)
                {
                    if (this.IsDeviceConnected)
                    {
                        try
                        {
                            await this.UnregisterDirectMethodsAsync(CancellationToken.None);
                            await this.DeviceClient.SetDesiredPropertyUpdateCallbackAsync(null, null);
                            await this.DeviceClient.SetReceiveMessageHandlerAsync(null, null);
                            await this.DeviceClient.CloseAsync();
                        }
                        catch (Exception e)
                        {
                            this.logger.LogError(e, e.Message);
                        }
                        this.logger.LogInformation($"Previous deivce client has been closed.");
                    }
                }

                this.deviceClient = null;
            }
        }

        protected virtual Task RegisterDirectMethodsAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task UnregisterDirectMethodsAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual async Task<string> ProvisioningAsync(CancellationToken cancellationToken)
        {
            using var certificate = this.LoadCertificate();
            using var security = new SecurityProviderX509Certificate(certificate);
            using var transport = new ProvisioningTransportHandlerMqtt();
            var provClient = ProvisioningDeviceClient.Create(this.provisioningEndpoint, this.provisioningIDScope, security, transport);

            try
            {
                var result = await provClient.RegisterAsync(cancellationToken);

                if (result.Status != ProvisioningRegistrationStatusType.Assigned || result.AssignedHub == null)
                {
                    this.logger.LogError($"Provisioning failed with status {result.Status}.");
                    return null;
                }

                this.logger.LogInformation($"Device {result.DeviceId} provisioning to {result.AssignedHub}.");
                this.AssignedHub = result.AssignedHub;
                await this.SaveAsync(cancellationToken);
                return result.AssignedHub;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, e.Message);
                return null;
            }
        }

        protected virtual Task SaveAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        protected async virtual Task SendCoreAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (this.IsDeviceConnected && this.Events.Count > 0)
                {
                    var sending = new List<ITimestampable>();

                    try
                    {
                        while (this.Events.TryDequeue(out var data))
                        {
                            data.SetTimestamp(DateTimeOffset.UtcNow);
                            sending.Add(data);
                        }

                        var json = JsonSerializer.SerializeObject(sending);
                        using var message = new Message(Encoding.UTF8.GetBytes(json))
                        {
                            ContentEncoding = "utf-8",
                            ContentType = "application/json",
                        };

                        await this.DeviceClient.SendEventAsync(message, cancellationToken);
                        await this.OnSentAsync(this, sending, cancellationToken);
                        await this.SaveAsync(cancellationToken);
                        sending.Clear();
                        continue;
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
                    finally
                    {
                        if (sending.Count > 0)
                        {
                            await this.OnSendFailedAsync(this, sending, cancellationToken);

                            foreach (var item in sending)
                            {
                                this.Events.Enqueue(item);
                            }
                            sending.Clear();
                            await this.SaveAsync(cancellationToken);
                        }

                        // wait and retry
                        await Task.Delay(interval, cancellationToken);
                    }
                }
                else
                {
                    await Task.Delay(interval, cancellationToken);
                }
            }
        }

        protected virtual Task OnReceivedAsync(CloudToDeviceMessage message) => Task.CompletedTask;

        protected virtual Task OnSentAsync(object sender, List<ITimestampable> messages, CancellationToken cancellationToken) => Task.CompletedTask;

        protected virtual Task OnSendFailedAsync(object sender, List<ITimestampable> messages, CancellationToken cancellationToken) => Task.CompletedTask;

        protected async virtual Task ReceiveAsync(Message receivedMessage, object _)
        {
            using (receivedMessage)
            {
                this.logger.LogTrace($"{DateTime.Now}> C2D message callback - message received with Id={receivedMessage.MessageId}.");
                using var reader = new StreamReader(receivedMessage.BodyStream, Encoding.UTF8);
                await this.OnReceivedAsync(new CloudToDeviceMessage
                {
                    JSONBody = await reader.ReadToEndAsync(),
                    CorrelationID = receivedMessage.CorrelationId,
                    Created = receivedMessage.CreationTimeUtc.Year > 2020 ? new DateTimeOffset(receivedMessage.CreationTimeUtc) : DateTimeOffset.UtcNow,
                    DeliveryCount = receivedMessage.DeliveryCount,
                    Enqueued = receivedMessage.EnqueuedTimeUtc.Year > 2020 ? new DateTimeOffset(receivedMessage.EnqueuedTimeUtc) : DateTimeOffset.UtcNow,
                    Expires = receivedMessage.Properties.ContainsKey(nameof(CloudToDeviceMessage.Expires)) ? DateTimeOffset.Parse(receivedMessage.Properties[nameof(CloudToDeviceMessage.Expires)]) : DateTimeOffset.MaxValue,
                    Valids = receivedMessage.Properties.ContainsKey(nameof(CloudToDeviceMessage.Valids)) ? DateTimeOffset.Parse(receivedMessage.Properties[nameof(CloudToDeviceMessage.Valids)]) : DateTimeOffset.MinValue,
                });
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
                    if (this.sendingTaskCancellation == null)
                    {
                        this.sendingTaskCancellation = new CancellationTokenSource();
                        this.sendingTask = this.SendCoreAsync(this.sendingTaskCancellation.Token);

                        await this.DeviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertyChangedAsync, null, this.sendingTaskCancellation.Token);
                        await this.DeviceClient.SetReceiveMessageHandlerAsync(ReceiveAsync, this.DeviceClient, this.sendingTaskCancellation.Token);
                        await this.RegisterDirectMethodsAsync(this.sendingTaskCancellation.Token);
                    }

                    var twin = await this.DeviceClient.GetTwinAsync(this.sendingTaskCancellation.Token);
                    await this.OnDesiredPropertyChangedAsync(twin.Properties.Desired, null);
                    this.logger.LogInformation("### The DeviceClient is CONNECTED; all operations will be carried out as normal.");

                    this.CancelTask(this.ensureConnectivityTaskCancellation, this.ensureConnectivityTask);
                    this.ensureConnectivityTaskCancellation = null;
                    this.ensureConnectivityTask = null;
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
                            this.logger.LogTrace("### The supplied credentials are invalid. Update the parameters and run again.");
                            this.SwapSecondaryCredentials();
                            break;

                        case ConnectionStatusChangeReason.Device_Disabled:
                            this.logger.LogTrace("### The device has been deleted or marked as disabled (on your hub instance)." +
                                "\nFix the device status in Azure and then create a new device client instance.");
                            break;

                        case ConnectionStatusChangeReason.Retry_Expired:
                            this.logger.LogTrace("### The DeviceClient has been disconnected because the retry policy expired." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                            break;

                        case ConnectionStatusChangeReason.Communication_Error:
                            this.logger.LogTrace("### The DeviceClient has been disconnected due to a non-retry-able exception. Inspect the exception for details." +
                                "\nIf you want to perform more operations on the device client, you should dispose (DisposeAsync()) and then open (OpenAsync()) the client.");
                            break;

                        default:
                            this.logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                            break;

                    }

                    this.AssignedHub = null;

                    if (this.ensureConnectivityTaskCancellation == null)
                    {
                        this.ensureConnectivityTaskCancellation = new CancellationTokenSource();
                        this.ensureConnectivityTask = this.ConnectAsync(this.ensureConnectivityTaskCancellation.Token);
                    }
                    break;

                default:
                    this.logger.LogError("### This combination of ConnectionStatus and ConnectionStatusChangeReason is not expected, contact the client library team with logs.");
                    break;
            }
        }

        protected virtual async Task OnDesiredPropertyChangedAsync(TwinCollection desiredProperties, object userContext)
        {
            if (this.lastTwinVersion >= desiredProperties.Version)
            {
                // do not proceed on any update that's older on its version
                return;
            }

            var properties = new Dictionary<string, object>();
            foreach (KeyValuePair<string, object> desiredProperty in desiredProperties)
            {
                properties.Add(desiredProperty.Key, desiredProperty.Value);
            }

            if (properties.Count > 0)
            {
                await this.OnDesiredPropertyUpdated(properties);
            }

            this.lastTwinVersion = desiredProperties.Version;
        }

        protected virtual Task OnDesiredPropertyUpdated(IReadOnlyDictionary<string, object> properties) => Task.CompletedTask;

        private void CancelTask(CancellationTokenSource source, Task task)
        {
            try
            {
                if (source != null)
                {
                    using (source)
                    {
                        if (!source.IsCancellationRequested
                            && (source.Token != null
                            && source.Token.CanBeCanceled))
                        {
                            this.logger.LogInformation($"Canceling task...");
                            source.Cancel();
                        }

                        if (task != null)
                        {
                            Task.WaitAll(new[] { task }, 5000);
                        }
                    }

                    this.logger.LogInformation($"Task cancelled.");
                }
            }
            catch (Exception e)
            {
                this.logger.LogError(e, e.Message);
            }
        }

        private void SwapSecondaryCredentials()
        {
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

            return certificate;
        }

        // If the client reports Connected status, it is already in operational state.
        // If the client reports Disconnected_retrying status, it is trying to recover its connection.
        // If the client reports Disconnected status, you will need to dispose and recreate the client.
        // If the client reports Disabled status, you will need to dispose and recreate the client.
        private static bool ShouldClientBeInitialized(ConnectionStatus connectionStatus)
            => connectionStatus == ConnectionStatus.Disconnected || connectionStatus == ConnectionStatus.Disabled;

        public async ValueTask DisposeAsync()
        {
            if (!this.disposed)
            {
                await this.DisposeAsync(true);
            }

            this.disposed = true;
        }


        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (disposing)
            {
                await this.DisconnectAsync(true);
                await this.SaveAsync(CancellationToken.None);
            }
        }
    }
}
