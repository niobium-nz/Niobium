# Cod.Platform.Notification.Firebase

Cod.Platform.Notification.Firebase provides a Firebase Cloud Messaging (FCM) push notification channel implementation for the Cod notification framework. It enables .NET applications to send push notifications to Android devices using Google¡¯s secure FCM infrastructure, supporting both data and notification payloads.

## What is this project about?

- Implements an FCM push notification channel for Cod.Platform.Notification.
- Supports both notification and data payloads for Android devices.
- Handles OAuth2 authentication with Google using service account credentials.
- Provides abstractions for credentials, notification payloads, and channel configuration.
- Designed for extensibility and integration with Cod¡¯s notification infrastructure.

## Getting Started

### 1. Install the NuGet Package

Add the package to your .NET project:
```
dotnet add package Cod.Platform.Notification.Firebase
```

### 2. Register the Firebase Push Notification Channel

In your DI setup, register your implementation of the Firebase push notification channel:

```csharp
using Cod.Platform.Notification.Firebase;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<FirebasePushNotificationChannel, MyFirebasePushNotificationChannel>();
```

> Replace `MyFirebasePushNotificationChannel` with your own implementation that provides credentials and message formatting.

### 3. Configure Google Service Account Credentials

Implement the logic to provide your Google service account credentials:

```csharp
public class MyFirebasePushNotificationChannel : FirebasePushNotificationChannel
{
    // ... constructor ...

    protected override Task<GoogleCredential> GetCredentialAsync(NotificationContext context)
    {
        // Load and return GoogleCredential from your service account JSON
    }

    protected override Task<ProjectScopeFirebaseMessage> GetMessageAsync(
        string brand, int templateID, NotificationContext context, IReadOnlyDictionary<string, object> parameters)
    {
        // Build and return ProjectScopeFirebaseMessage
    }
}
```

### 4. Send Push Notifications

Use the notification service to send push notifications:

```csharp
var notificationService = serviceProvider.GetRequiredService<INotificationService>();
await notificationService.SendAsync(
    channel: "FirebasePush",
    parameters: new NotificationParameters { /* ... */ },
    cancellationToken);
```

### 5. Notification and Data Payloads

- For notification payloads, set the `Notification` property in `FirebaseMessage`.
- For data payloads, set the `Data` property in `FirebaseMessage`.

## How Cod.Platform.Notification.Firebase is Consumed

Consumer projects use Cod.Platform.Notification.Firebase to:

- Register and use Firebase push notification channels via DI.
- Send transactional and workflow push notifications to Android devices.
- Integrate FCM delivery into multi-channel notification workflows.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
