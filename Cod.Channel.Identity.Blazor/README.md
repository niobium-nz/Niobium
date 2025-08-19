# Cod.Channel.Identity.Blazor

Cod.Channel.Identity.Blazor provides Blazor UI integration for the Cod identity and authentication framework. It delivers ready-to-use Blazor components, view models, and helpers for secure, user-friendly login and authentication flows in Blazor WebAssembly and Server applications.

## What is this project about?
- Implements Blazor components for email, TOTP, and passwordless login experiences.
- Supplies view models and helpers for authentication, navigation, and state management.
- Used by consumer projects to enable modern, secure login UIs and integrate with Cod's identity services.

## Getting Started

### 1. Install the NuGet Package
Add the package to your Blazor project:

```
dotnet add package Cod.Channel.Identity.Blazor
```

### 2. Register Services in Dependency Injection
Add Cod.Channel.Identity.Blazor to your DI container (typically in `Program.cs`):

```csharp
using Cod.Channel.Identity.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddChannelIdentityBlazor();
```

### 3. Use Login Components in Your Blazor Pages
Import the namespace in your `_Imports.razor`:

```razor
@using Cod.Channel.Identity.Blazor
```

Add the login component to your page:

```razor
<EmailLogin />
```

- `EmailLogin` provides a two-step email/OTP login experience.
- `RedirectToLogin` can be used to redirect unauthenticated users to the login page.

### 4. Integration with Other Cod Packages
Cod.Channel.Identity.Blazor is designed to work seamlessly with:
- `Cod.Channel.Identity` for backend authentication
- `Cod.Channel.Blazor` for navigation and platform services
- All other Cod.* packages

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
