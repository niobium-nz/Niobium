# Niobium.Platform.Finance.Stripe

Niobium.Platform.Finance.Stripe provides seamless integration with the Stripe payment gateway for .NET applications using the Niobium.Platform.Finance framework. It implements payment processing, refunds, setup intents, and webhook handling, enabling secure and extensible Stripe-based payment flows.

## What is this project about?

- Implements the `IPaymentProcessor` interface for Stripe, supporting charge, refund, authorize, validate, and complete operations.
- Provides a `StripeIntegration` service for direct Stripe API operations (PaymentIntent, SetupIntent, Refund, etc.).
- Handles Stripe webhook events (charge, refund, setup intent) and maps them to Niobium.Finance domain events.
- Supplies DI modules for easy registration and configuration in .NET and Azure Functions projects.
- Supports localization and custom error mapping for Stripe-specific payment errors.

## Getting Started

### 1. Install the NuGet Package

```sh
dotnet add package Niobium.Platform.Finance.Stripe
```

### 2. Register Stripe Payment Services

In your application's DI setup, use the provided extension method:

```csharp
using Niobium.Platform.Finance.Stripe;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFinance(options =>
{
    options.SecretAPIKey = "<your-stripe-secret-key>";
    options.SecretHashKey = "<your-hash-key>";
    // ...other options...
});
```

Or, for Azure Functions:

```csharp
using Niobium.Platform.Finance.Stripe;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        // Register Stripe payment processor
        worker.Services.AddFinance(options =>
        {
            options.SecretAPIKey = "<your-stripe-secret-key>";
            options.SecretHashKey = "<your-hash-key>";
        });
    })
    .Build();
```

### 3. Configure Stripe API Keys

Add your Stripe API key and hash key to your configuration (e.g., appsettings.json):

```json
{
  "PaymentServiceOptions": {
    "SecretAPIKey": "<your-stripe-secret-key>",
    "SecretHashKey": "<your-hash-key>"
  }
}
```

### 4. Use the Payment Service

**Example: Initiating a Stripe Payment**

```csharp
var paymentService = serviceProvider.GetRequiredService<IPaymentService>();
var chargeRequest = new ChargeRequest
{
    TargetKind = ChargeTargetKind.User,
    Target = "user-123",
    Channel = PaymentChannels.Cards,
    Operation = PaymentOperationKind.Charge,
    Amount = 1000,
    Currency = Currency.USD,
    IP = "127.0.0.1"
};
var result = await paymentService.ChargeAsync(chargeRequest);
```

**Example: Handling Stripe Webhooks**

The Stripe payment processor will handle webhook events (charge, refund, setup intent) and update your domain model accordingly. You can extend this by subscribing to domain events or customizing the webhook middleware.

### 5. Extending and Customizing

- Implement your own error retriever or localization by extending `InternalErrorRetriever` and `R`.
- Customize payment flows by extending or replacing `StripePaymentProcessor` or `StripeIntegration`.

## How Niobium.Platform.Finance.Stripe is Consumed

Consumer projects (such as Niobium.Invoicing) use Niobium.Platform.Finance.Stripe to:

- Register Stripe as a payment processor via DI.
- Handle all payment, refund, and setup intent flows through a unified interface.
- Process Stripe webhook events and map them to business transactions.
- Localize and customize error handling for Stripe-specific scenarios.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
