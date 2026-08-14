# Niobium.Platform.Functions

Niobium.Platform.Functions provides foundational infrastructure, utilities, and extension points for building scalable, maintainable .NET business function apps. It includes abstractions and helpers for dependency injection, configuration, caching, error handling, analytics, and HTTP operations, making it easier for consumer projects to integrate with modern cloud and serverless platforms.

## What is this project about?
- Offers extension methods for `IServiceCollection` to register platform services and middleware.
- Provides abstractions for caching, configuration, and error retrieval.
- Includes helpers for HTTP request/response handling, analytics (App Insights), and validation.
- Used as a core dependency by Niobium.* and consumer projects (e.g., Niobium.Invoicing.*) to standardize platform integration and reduce boilerplate.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET function app project:

```
dotnet add package Niobium.Platform.Functions
```

### 2. Register Platform Services
In your application's startup or DI configuration, use the provided extension methods:

```csharp
using Niobium.Platform.Functions;

var builder = FunctionsApplication.CreateBuilder(args);

// Register platform services
builder.AddPlatform();
builder.UsePlatform();

var app = builder.Build();
// ...
```

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
