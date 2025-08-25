# Niobium.Platform

Niobium.Platform provides foundational infrastructure, utilities, and extension points for building scalable, maintainable .NET business applications. It includes abstractions and helpers for dependency injection, configuration, caching, error handling, analytics, and HTTP operations, making it easier for consumer projects to integrate with modern cloud and serverless platforms.

## What is this project about?
- Offers extension methods for `IServiceCollection` to register platform services and middleware.
- Provides abstractions for caching, configuration, and error retrieval.
- Includes helpers for HTTP request/response handling, analytics (App Insights), and validation.
- Used as a core dependency by Niobium.* and consumer projects (e.g., Niobium.Invoicing.*) to standardize platform integration and reduce boilerplate.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Platform
```

### 2. Register Platform Services
In your application's startup or DI configuration, use the provided extension methods:

```csharp
using Niobium.Platform;

var builder = WebApplication.CreateBuilder(args);

// Register platform services
builder.Services.AddCodPlatform();

var app = builder.Build();
// ...
```

### 3. Use Platform Abstractions
- **Caching:**
  ```csharp
  var cache = serviceProvider.GetRequiredService<ICacheStore>();
  await cache.SetAsync("key", value);
  ```
- **Configuration:**
  ```csharp
  var config = serviceProvider.GetRequiredService<IConfigurationProvider>();
  var setting = config["MySetting"];
  ```
- **Error Handling:**
  ```csharp
  var errorRetriever = serviceProvider.GetRequiredService<IErrorRetriever>();
  var error = errorRetriever.GetError(404);
  ```
- **Analytics:**
  ```csharp
  var analytics = serviceProvider.GetRequiredService<IAppInsights>();
  await analytics.TrackEventAsync("InvoiceCreated");
  ```

### 4. Middleware and HTTP Helpers
- Use provided middleware for error handling and context access:
  ```csharp
  app.UseMiddleware<ErrorHandlingMiddleware>();
  app.UseMiddleware<FunctionContextAccessorMiddleware>();
  ```
- Use HTTP helpers for proxying, signature issuance, and response formatting.

## How Niobium.Platform is Consumed
Projects like Niobium.Invoicing.Core reference Niobium.Platform to:
- Register and use shared infrastructure (caching, config, analytics).
- Standardize error handling and validation.
- Simplify HTTP and function middleware integration.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
