# Niobium.Platform.Captcha.ReCaptcha

Niobium.Platform.Captcha.ReCaptcha provides Google reCAPTCHA v3/v2 integration and risk assessment for .NET applications using the Niobium.Platform framework. It enables secure, tenant-aware bot protection and visitor risk analysis for APIs and web applications.

## What is this project about?

- Implements the `IVisitorRiskAssessor` interface for Google reCAPTCHA, supporting token validation and risk scoring.
- Provides a DI module for easy registration and configuration in .NET and Azure Functions projects.
- Supports multi-tenant secret management and development-mode bypass.
- Enables secure, extensible bot protection and risk assessment for consumer projects.

## Getting Started

### 1. Install the NuGet Package

```sh
dotnet add package Niobium.Platform.Captcha.ReCaptcha
```

### 2. Register Captcha Services

In your application's DI setup, use the provided extension method:

```csharp
using Niobium.Platform.Captcha.ReCaptcha;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCaptcha(options =>
{
    options.IsDisabled = false;
    options.Secrets = new Dictionary<string, string>
    {
        ["tenant1"] = "<your-recaptcha-secret-for-tenant1>",
        ["tenant2"] = "<your-recaptcha-secret-for-tenant2>"
    };
});
```

Or, for Azure Functions:

```csharp
using Niobium.Platform.Captcha.ReCaptcha;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        worker.Services.AddCaptcha(options =>
        {
            options.IsDisabled = false;
            options.Secrets = new Dictionary<string, string>
            {
                ["tenant1"] = "<your-recaptcha-secret-for-tenant1>",
                ["tenant2"] = "<your-recaptcha-secret-for-tenant2>"
            };
        });
    })
    .Build();
```

### 3. Configure Captcha Options

Add your reCAPTCHA secrets and settings to your configuration (e.g., appsettings.json):

```json
{
  "CaptchaOptions": {
    "IsDisabled": false,
    "Secrets": {
      "tenant1": "<your-recaptcha-secret-for-tenant1>",
      "tenant2": "<your-recaptcha-secret-for-tenant2>"
    }
  }
}
```

### 4. Use the Visitor Risk Assessor

**Example: Assessing a Captcha Token**

```csharp
var riskAssessor = serviceProvider.GetRequiredService<IVisitorRiskAssessor>();
bool isLowRisk = await riskAssessor.AssessAsync(token, requestID: "req-123", tenant: "tenant1", clientIP: "1.2.3.4");
```

If `IsDisabled` is true or in development mode, all requests are considered low risk.

## How Niobium.Platform.Captcha.ReCaptcha is Consumed

Consumer projects (such as Niobium.Invoicing) use Niobium.Platform.Captcha.ReCaptcha to:

- Register and use Google reCAPTCHA risk assessment via DI.
- Secure APIs and endpoints with tenant-aware bot protection.
- Integrate risk assessment into business logic and validation flows.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
