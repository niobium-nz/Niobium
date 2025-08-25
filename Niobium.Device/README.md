# Niobium.Device

Niobium.Device provides abstractions and helpers for sending commands to IoT devices in .NET applications. It enables a consistent, testable, and extensible way to execute device commands and process their results, supporting both synchronous and fire-and-forget scenarios.

## What is this project about?

- Defines the `IIoTCommander` interface for sending commands to IoT devices.
- Provides the `IoTCommandResult` type for standardized command result handling.
- Includes extension methods for deserializing command payloads.
- Designed to be used by Niobium.* and consumer projects to standardize IoT device command execution and result processing.
- Enables rapid development of device integration features in business and platform applications.

## Getting Started

### 1. Install the NuGet Package

Add the package to your .NET project:

```
dotnet add package Niobium.Device
```

### 2. Implement and Register an IoT Commander

You must provide your own implementation of `IIoTCommander` that knows how to communicate with your devices. Register it in your DI container:

```csharp
using Niobium.Device;
using Microsoft.Extensions.DependencyInjection;

public class MyIoTCommander : IIoTCommander
{
    public async Task<IoTCommandResult?> ExecuteAsync(string device, object command, bool fireAndForget = true)
    {
        // Implement device communication logic here
        // Example: send command to device, wait for result, return IoTCommandResult
        return new IoTCommandResult
        {
            Status = 0,
            PayloadJSON = "{\"result\": \"ok\"}",
            ExecutedAt = DateTimeOffset.UtcNow
        };
    }
}

var services = new ServiceCollection();
services.AddSingleton<IIoTCommander, MyIoTCommander>();
```

### 3. Execute Commands on Devices

```csharp
using Niobium.Device;

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
        if (result != null && result.Status == 0)
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

### 4. Fire-and-Forget Commands

If you do not need to wait for a result, set `fireAndForget: true`:

```csharp
await _commander.ExecuteAsync("device-id", new { Action = "Ping" }, fireAndForget: true);
```

### 5. Handling Command Results

- `IoTCommandResult.Status`: Status code returned by the device or command handler.
- `IoTCommandResult.PayloadJSON`: JSON-serialized payload from the device.
- `IoTCommandResult.ExecutedAt`: Timestamp of command execution.
- Use the `GetPayload<T>()` extension method to deserialize the payload to your expected type.

### 6. Example: Unit Testing with Mocks

You can easily mock `IIoTCommander` for unit testing:

```csharp
using Moq;
using Niobium.Device;

var mockCommander = new Mock<IIoTCommander>();
mockCommander.Setup(c => c.ExecuteAsync(It.IsAny<string>(), It.IsAny<object>(), false))
    .ReturnsAsync(new IoTCommandResult
    {
        Status = 0,
        PayloadJSON = "{\"result\": \"ok\"}",
        ExecutedAt = DateTimeOffset.UtcNow
    });

// Inject mockCommander.Object into your service for testing
```

## How Niobium.Device is Consumed

Consumer projects use Niobium.Device to:

- Register and use IoT command senders via DI.
- Standardize command execution and result handling for IoT devices.
- Extend or implement their own device communication logic while relying on a common contract.
- Enable rapid prototyping and integration of device features in business applications.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---
