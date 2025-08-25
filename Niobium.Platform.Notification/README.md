# Niobium.Platform.Notification

Niobium.Platform.Notification provides a unified abstraction and infrastructure for sending notifications (email, SMS, push, etc.) in .NET applications. It supports multiple notification channels, templates, and providers, enabling scalable and extensible notification workflows for business and platform scenarios.

## What is this project about?
- Defines interfaces and base types for notification channels and services (email, SMS, push, etc.).
- Implements channel repositories, notification context, and template management.
- Provides integration with providers like SendGrid for email and supports SMS and push notifications.
- Includes a DI dependency module for easy registration and extension.
- Used by Niobium.* and consumer projects to standardize and extend notification delivery.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Platform.Notification
```

### 2. Register Notification Services
In your DI setup, use the provided dependency module:

```csharp
using Niobium.Platform.Notification;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCodPlatformNotification(options =>
{
    // Configure notification options, e.g., default channels, providers, etc.
});
```

### 3. Use Notification Services
- **Send Notification:**
  ```csharp
  var notificationService = serviceProvider.GetRequiredService<INotificationService>();
  await notificationService.SendAsync(
      channel: "Email", // or "SMS", "Push"
      parameters: new NotificationParameters { /* ... */ },
      cancellationToken);
  ```
- **Custom Channels:**
  Implement `INotificationChannel` for custom notification logic and register it via DI.

### 4. Email, SMS, and Push Integration
- **Email:**
  Use built-in SendGrid integration or extend with your own provider.
- **SMS:**
  Use `SMSNotificationChannel` and related types for SMS delivery.
- **Push:**
  Use `PushNotificationChannel` for push notification scenarios.

## How Niobium.Platform.Notification is Consumed
Consumer projects (e.g., Niobium.Invoicing.Core) use Niobium.Platform.Notification to:
- Register and use notification services and channels via DI.
- Send transactional and workflow notifications across multiple channels.
- Extend notification logic with custom channels and templates.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
