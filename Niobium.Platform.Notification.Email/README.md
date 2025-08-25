# Niobium.Platform.Notification.Email

Niobium.Platform.Notification.Email provides abstractions, clients, and helpers for sending email notifications in .NET applications. It supports both development and production scenarios, enabling easy integration of email delivery into business workflows.

## What is this project about?
- Implements generic and development email notification clients.
- Defines abstractions for email address and notification sending.
- Used by Niobium.* and consumer projects to standardize and simplify email notification delivery.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Platform.Notification.Email
```

### 2. Register Email Notification Services
In your DI setup, register the desired email notification client:

```csharp
using Niobium.Platform.Notification.Email;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddSingleton<IEmailNotificationClient, GenericEmailNotificationClient>();
// Or for development/testing:
services.AddSingleton<IEmailNotificationClient, DevelopmentEmailNotificationClient>();
```

### 3. Send Email Notifications
- **Send Email:**
  ```csharp
  var emailClient = serviceProvider.GetRequiredService<IEmailNotificationClient>();
  var result = await emailClient.SendAsync(
      new EmailAddress { DisplayName = "Sender", Address = "sender@example.com" },
      new[] { "recipient@example.com" },
      "Subject",
      "Body HTML",
      cancellationToken);
  ```

## How Niobium.Platform.Notification.Email is Consumed
Consumer projects (e.g., Niobium.Invoicing.Core) use Niobium.Platform.Notification.Email to:
- Register and use email notification clients via DI.
- Send transactional and notification emails as part of business workflows.
- Support both production and development/test email delivery scenarios.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
