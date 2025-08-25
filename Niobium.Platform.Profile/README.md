# Niobium.Platform.Profile

Niobium.Platform.Profile provides user profile management infrastructure and DI modules for .NET applications. It enables seamless integration of user profile data, preferences, and extensibility points, supporting both platform and business-specific profile scenarios.

## What is this project about?
- Implements platform-level profile services and dependency modules.
- Provides abstractions for profile retrieval, update, and extension.
- Used by Niobium.* and consumer projects to standardize user profile management and integration.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Niobium.Platform.Profile
```

### 2. Register Profile Services
In your DI setup, use the provided dependency module:

```csharp
using Niobium.Platform.Profile;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCodPlatformProfile(options =>
{
    // Configure profile options as needed
});
```

### 3. Use Platform Profile Services
- **Profile Service:**
  ```csharp
  var profileService = serviceProvider.GetRequiredService<IPlatformProfileService>();
  var profile = await profileService.GetProfileAsync(userId);
  ```
- **Dependency Module:**
  The `DependencyModule` registers all required services for platform profile management.

## How Niobium.Platform.Profile is Consumed
Consumer projects (e.g., Niobium.Invoicing.Core) use Niobium.Platform.Profile to:
- Register and use platform profile services via DI.
- Retrieve and update user profile data in a standardized way.
- Extend profile management for business-specific needs.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
