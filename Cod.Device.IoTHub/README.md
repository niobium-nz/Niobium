# Cod.Device.IoTHub

Cod.Device.IoTHub provides a ready-to-use implementation of the `IIoTCommander` interface for Azure IoT Hub, enabling .NET applications to send commands to IoT devices using Azure's secure, scalable device messaging infrastructure.

## What is this project about?

- Implements `AzureIoTHubCommander`, an `IIoTCommander` for Azure IoT Hub.
- Provides dependency injection modules for easy integration.
- Handles both direct method invocation and cloud-to-device messaging.
- Supports fire-and-forget and request/response command patterns.
- Integrates with the Cod.Device abstraction for seamless device command execution.

## Getting Started

### 1. Install the NuGet Package

Add the package to your .NET project:

```
dotnet add package Cod.Device.IoTHub
```

### 2. Register Azure IoT Hub Commander in Dependency Injection

In your DI setup (e.g., `Program.cs`):

```csharp
using Cod.Device.IoTHub;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddDevice();
```

This registers `AzureIoTHubCommander` as the default `IIoTCommander`.

### 3. Configure Azure IoT Hub Connection

Set the `IOT_HUB_CONN` configuration key to your Azure IoT Hub connection string (e.g., via environment variable or appsettings):

```
IOT_HUB_CONN=HostName=your-iothub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=...
```

### 4. Execute Commands on Devices

```csharp
using Cod.Device;

public class DeviceService
{
    private readonly IIoTCommander _commander;
    public DeviceService(IIoTCommander commander)
    {
        _commander = commander;
    }

    public async Task<bool> TurnOnDeviceAsync(string deviceId)
    {
        var command = new { Action = "TurnOn" };
        var result = await _commander.ExecuteAsync(deviceId, command, fireAndForget: false);
        if (result != null && result.Status == 200)
        {
            var payload = result.GetPayload<MyDeviceResponse>();
            // Use payload as needed
            return true;
        }
        return false;
    }
}

public class MyDeviceResponse
{
    public string Result { get; set; }
}
```

### 5. Fire-and-Forget Commands

```csharp
await _commander.ExecuteAsync("device-id", new { Action = "Ping" }, fireAndForget: true);
```

### 6. Advanced: Handling Device Offline/Retry

`AzureIoTHubCommander` automatically retries on transient errors and logs device offline events. You can override `OnDeviceOfflineAsync` for custom handling in a derived class.

### 7. Extending/Customizing

- You can inherit from `AzureIoTHubCommander` to customize retry logic, logging, or device offline handling.
- You can register your own `IIoTCommander` implementation if you need to support other protocols or device types.

## How Cod.Device.IoTHub is Consumed

Consumer projects use Cod.Device.IoTHub to:
- Register and use Azure IoT Hub device command senders via DI.
- Standardize command execution and result handling for Azure-connected IoT devices.
- Rapidly prototype and deploy device integration features in business applications.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
