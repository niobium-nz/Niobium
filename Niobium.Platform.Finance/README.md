# Niobium.Platform.Finance

Niobium.Platform.Finance provides a robust, extensible infrastructure for financial operations, payment processing, and accounting in .NET applications. It offers abstractions, middleware, and helpers for integrating payment services, managing account balances, and handling transactions in a secure and auditable way.

## What is this project about?

- Implements the `AccountableDomain<T>` base class for account and transaction management.
- Provides interfaces and implementations for payment services (`IPaymentService`, `IPaymentProcessor`), accounting auditors, and deposit recorders.
- Supplies middleware for payment request and webhook handling in both ASP.NET Core and Azure Functions.
- Enables extensible integration with multiple payment providers (e.g., Stripe, WeChat, Alipay) via the `IPaymentProcessor` interface.
- Used by consumer projects to standardize and accelerate the implementation of financial and payment features.

## Getting Started

### 1. Install the NuGet Package

```sh
dotnet add package Niobium.Platform.Finance
```

### 2. Register Finance Services

In your application's DI setup, use the provided extension method:

```csharp
using Niobium.Platform.Finance;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddFinance();
```

Or, for Azure Functions:

```csharp
using Niobium.Platform.Finance;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(worker =>
    {
        worker.UsePlatformPayment<MyDepositHandler, MyAccountableDomain, MyAccountableEntity>();
    })
    .Build();
```

### 3. Configure Payment Options

Add your payment configuration (e.g., API keys) to your appsettings.json or configuration provider:

```json
{
  "PaymentServiceOptions": {
    "PaymentWebHookEndpoint": "payments/webhook",
    "PaymentRequestEndpoint": "payments/init",
    "SecretAPIKey": "<your-api-key>",
    "SecretHashKey": "<your-hash-key>"
  }
}
```

### 4. Use Payment Service and Middleware

**Example: Initiating a Payment Request**

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

**Example: Adding Middleware in ASP.NET Core**

```csharp
app.UsePlatformPayment<MyDepositHandler, MyAccountableDomain, MyAccountableEntity>();
```

This will automatically add the payment request and webhook middleware to your pipeline.

### 5. Implementing a Custom Payment Processor

To support a new payment provider, implement the `IPaymentProcessor` interface and register it with DI.

```csharp
public class MyPaymentProcessor : IPaymentProcessor
{
    public Task<OperationResult<ChargeResult>> RetrieveChargeAsync(string transaction, PaymentChannels paymentChannel) { ... }
    public Task<OperationResult<ChargeResponse>> ChargeAsync(ChargeRequest request) { ... }
    public Task<OperationResult<ChargeResult>> ReportAsync(string notificationJSON) { ... }
}
```

## How Niobium.Platform.Finance is Consumed

Consumer projects (such as Niobium.Invoicing) use Niobium.Platform.Finance to:

- Register and use shared payment and accounting infrastructure.
- Standardize transaction and account management via `AccountableDomain<T>`.
- Integrate payment request and webhook endpoints with minimal code.
- Extend payment support by implementing custom `IPaymentProcessor`s.

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
