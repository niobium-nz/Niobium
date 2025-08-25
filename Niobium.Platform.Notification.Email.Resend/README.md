# Niobium.Platform.Notification.Email.Resend

Niobium.Platform.Notification.Email.Resend provides an implementation of the IEmailNotificationClient for sending emails via the Resend transactional email service. It enables .NET applications to deliver reliable, scalable, and trackable email notifications using the Resend API.

## What is this project about?
- Implements ResendEmailNotificationClient for integration with the Resend email delivery platform.
- Provides configuration options for Resend API keys and sender details.
- Includes a DI dependency module for easy registration in .NET applications.
- Used by Niobium.* and consumer projects to enable production-grade email delivery with Resend.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Platform.Notification.Email.Resend
```

### 2. Register Resend Email Notification Client
In your DI setup, use the provided dependency module or register the client directly:

```csharp
using Niobium.Platform.Notification.Email.Resend;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCodPlatformNotificationEmailResend(options =>
{
    options.ApiKey = "<your-resend-api-key>";
    options.SenderAddress = "noreply@yourdomain.com";
    // Configure additional options as needed
});
```

### 3. Send Email via Resend
- **Send Email:**
  ```csharp
  var emailClient = serviceProvider.GetRequiredService<IEmailNotificationClient>();
  var result = await emailClient.SendAsync(
      new EmailAddress { DisplayName = "Sender", Address = "noreply@yourdomain.com" },
      new[] { "recipient@example.com" },
      "Subject",
      "Body HTML",
      cancellationToken);
  ```

## How Niobium.Platform.Notification.Email.Resend is Consumed
Consumer projects (e.g., Niobium.Invoicing.Core) use Niobium.Platform.Notification.Email.Resend to:
- Register the Resend email client via DI for production email delivery.
- Send transactional and notification emails using the Resend API.
- Leverage Resend's deliverability, analytics, and scalability features.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
