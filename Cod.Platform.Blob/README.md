# Cod.Platform.Blob

Cod.Platform.Blob provides abstractions, entitlement descriptors, and DI modules for secure, role-based, and personalized access to Azure Blob Storage in .NET applications. It enables fine-grained access control, signature issuance, and resource management for cloud file storage.

## What is this project about?
- Implements DI modules for registering blob storage controls and entitlement management.
- Provides role-based and personalized entitlement descriptors for secure access.
- Supplies helpers for signature issuance and resource control.
- Used by consumer projects to standardize and secure Azure Blob Storage integration.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Cod.Platform.Blob
```

### 2. Register Blob Storage Services in Dependency Injection
In your DI setup:

```csharp
using Cod.Platform.Blob;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddBlob(options =>
{
    options.FullyQualifiedDomainName = "<your-storage-account.blob.core.windows.net>";
    options.Key = "<your-storage-account-key>";
    // Configure additional options as needed
});
```

### 3. Grant Entitlements
- **Role-based Entitlement:**
  ```csharp
  services.GrantBlobEntitlementTo(
      sp => "MyRole",
      Cod.File.FilePermissions.Read,
      sp => ["container1", "container2"],
      sp => "<your-storage-account.blob.core.windows.net>");
  ```
- **Personalized Entitlement:**
  ```csharp
  services.GrantBlobPersonalizedEntitlementTo(
      sp => "MyRole",
      Cod.File.FilePermissions.Read,
      sp => ["container1"],
      sp => "<your-storage-account.blob.core.windows.net>");
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
