# Niobium.Channel.Profile

Niobium.Channel.Profile provides user profile management services and abstractions for the Niobium framework. It enables secure, extensible, and event-driven management of user profile data in .NET and Blazor applications.

## What is this project about?
- Implements profile storage, retrieval, and update logic for user-centric business applications.
- Supplies interfaces and base classes for profile entities, events, and repositories.
- Used by consumer projects to manage user profile data, preferences, and related events.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET or Blazor project:

```
dotnet add package Niobium.Channel.Profile
```

### 2. Register Services in Dependency Injection
Add Niobium.Channel.Profile to your DI container (typically in `Program.cs`):

```csharp
using Niobium.Channel.Profile;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddChannelProfile();
```

### 3. Use Profile Services in Your Application
Inject and use the provided services in your application:

```csharp
@inject IProfileService ProfileService
```

- `IProfileService` provides methods for retrieving and updating user profile data.
- Extend or implement profile entities and events as needed for your domain.

### 4. Integration with Other Niobium Packages
Niobium.Channel.Profile is designed to work seamlessly with:
- `Niobium.Channel.Identity` for authentication and user context
- `Niobium.Channel` for event-driven business logic
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
