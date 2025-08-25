# Niobium.Platform.Notification.Apple

Niobium.Platform.Notification.Apple provides an Apple Push Notification (APNs) channel implementation for the Niobium notification framework. It enables .NET applications to send push notifications to iOS/macOS devices using Apple¡¯s secure APNs infrastructure, supporting both alert and background notifications.

## What is this project about?

- Implements an APNs push notification channel for Niobium.Platform.Notification.
- Supports both alert and background push notifications.
- Handles JWT-based authentication with Apple using ES256 keys.
- Provides abstractions for credentials, notification payloads, and channel configuration.
- Designed for extensibility and integration with Niobium's notification infrastructure.

## Getting Started

### 1. Install the NuGet Package

Add the package to your .NET project:

```
dotnet add package Niobium.Platform.Notification.Apple
```

### 2. Register the Apple Push Notification Channel

In your DI setup, register your implementation of the Apple push notification channel:

```csharp
using Niobium.Platform.Notification.Apple;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<ApplePushNotificationChannel, MyApplePushNotificationChannel>();
```

> Replace `MyApplePushNotificationChannel` with your own implementation that provides credentials and message formatting.

### 3. Configure Apple Push Credentials

Implement the logic to provide your Apple Team ID, Key ID, and private key (in PKCS8 format):

```csharp
public class MyApplePushNotificationChannel : ApplePushNotificationChannel
{
    // ... constructor ...

    protected override Task<ApplePushCredential> GetCredentialAsync(NotificationContext context)
    {
        return Task.FromResult(new ApplePushCredential
        {
            TeamID = "<your-team-id>",
            KeyID = "<your-key-id>",
            Key = "<your-base64-private-key>"
        });
    }

    protected override Task<IEnumerable<ApplePushNotification>> GetMessagesAsync(
        string brand, int templateID, NotificationContext context, IReadOnlyDictionary<string, object> parameters)
    {
        // Build and return ApplePushNotification objects
    }
}
```

### 4. Send Push Notifications

Use the notification service to send push notifications:

```csharp
var notificationService = serviceProvider.GetRequiredService<INotificationService>();
await notificationService.SendAsync(
    channel: "ApplePush",
    parameters: new NotificationParameters { /* ... */ },
    cancellationToken);
```

### 5. Alert and Background Notifications

- For alert notifications, set `Background = false` and provide a string message.
- For background notifications, set `Background = true` and provide a payload object.

## How Niobium.Platform.Notification.Apple is Consumed

Consumer projects use Niobium.Platform.Notification.Apple to:

- Register and use Apple push notification channels via DI.
- Send transactional and workflow push notifications to iOS/macOS devices.
- Integrate APNs delivery into multi-channel notification workflows.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
