# Niobium.Channel.Blazor

Niobium.Channel.Blazor provides Blazor-specific integration for the Niobium framework, enabling seamless navigation, browser utilities, and platform services for Blazor WebAssembly and Server applications. It is designed to be used alongside other Niobium.* packages to build modern, modular, and scalable business applications.

## What is this project about?
- Supplies Blazor-specific services such as navigation (`INavigator`), browser detection (`IBrowser`), and platform integration.
- Bridges Niobium's domain-driven/event-driven backend with Blazor's frontend.
- Used by consumer projects to enable navigation, browser utilities, and Niobium-based DI in Blazor apps.

## Getting Started

### 1. Install the NuGet Package
Add the package to your Blazor project:

```
dotnet add package Niobium.Channel.Blazor
```

### 2. Register Services in Dependency Injection
Add Niobium.Channel.Blazor to your DI container (typically in `Program.cs`):

```csharp
using Niobium.Channel.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddChannelBlazor();
```

### 3. Use Provided Services in Your Components
Inject and use the provided services in your Blazor components:

```razor
@inject INavigator Navigator
@inject IBrowser Browser
```

- `INavigator` provides navigation and query string utilities for Blazor.
- `IBrowser` provides browser/user agent detection and localization utilities.

### 4. Integration with Other Niobium Packages
Niobium.Channel.Blazor is designed to work seamlessly with:
- `Niobium.Channel.Blazor.Fluent` for advanced UI components
- `Niobium.Channel.Identity.Blazor` for authentication and authorization
- All other Niobium.* packages

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
