# Niobium.Channel.Identity

Niobium.Channel.Identity provides core identity and authentication services for the Niobium framework. It supplies abstractions, commands, and helpers for secure user authentication, authorization, and identity management in .NET and Blazor applications.

## What is this project about?
- Implements authentication schemes (e.g., Basic, TOTP, OAuth) and login commands for business applications.
- Provides extensible interfaces for authenticators, identity helpers, and login flows.
- Used by consumer projects to enable secure login, user management, and integration with Niobium's event-driven architecture.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET or Blazor project:

```
dotnet add package Niobium.Channel.Identity
```

### 2. Register Services in Dependency Injection
Add Niobium.Channel.Identity to your DI container (typically in `Program.cs`):

```csharp
using Niobium.Channel.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddIdentity(options =>
{
    options.App = "YourAppName";
    // Configure additional options as needed
});
```

### 3. Use Authentication Services in Your Application
Inject and use the provided services in your application:

```csharp
@inject IAuthenticator Authenticator
```

- `IAuthenticator` provides methods for login, logout, and authentication state.
- Use login commands (e.g., `TOTPLoginCommand`) for advanced scenarios.

### 4. Integration with Other Niobium Packages
Niobium.Channel.Identity is designed to work seamlessly with:
- `Niobium.Channel.Identity.Blazor` for Blazor UI integration
- `Niobium.Channel.Profile` for user profile management
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
