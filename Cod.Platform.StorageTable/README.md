# Cod.Platform.StorageTable

Cod.Platform.StorageTable provides abstractions, dependency injection modules, and helpers for integrating Azure Table Storage (and compatible storage) into .NET applications. It enables secure, role-based, and personalized access to storage tables, supporting entitlement management, SAS token issuance, and repository patterns.

## What is this project about?
- Implements DI modules for registering Azure Table Storage repositories and controls.
- Provides entitlement descriptors for role-based and personalized access to storage tables.
- Supplies helpers for SAS token issuance and resource control.
- Used by Cod.* and consumer projects to standardize, secure, and extend Azure Table Storage integration.

## Getting Started

### 1. Install the NuGet Package
Add the package to your .NET project:

```
dotnet add package Cod.Platform.StorageTable
```

### 2. Register Storage Table Services
In your DI setup, use the provided dependency module:

```csharp
using Cod.Platform.StorageTable;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddDatabase(options =>
{
    options.FullyQualifiedDomainName = "<your-storage-account.table.core.windows.net>";
    options.Key = "<your-storage-account-key>";
    // Configure additional options as needed
});
```

### 3. Enable Resource Token Support (Optional)
To enable SAS token issuance and resource control:

```csharp
services.AddDatabaseResourceTokenSupport(options =>
{
    options.SignatureSecret = "<your-signature-secret>";
});
```

### 4. Grant Entitlements
- **Role-based Entitlement:**
  ```csharp
  services.GrantDatabaseEntitlementTo(
      sp => "MyRole",
      DatabasePermissions.Query,
      sp => "MyTable",
      sp => "<your-storage-account.table.core.windows.net>");
  ```
- **Personalized Entitlement:**
  ```csharp
  services.GrantDatabasePersonalizedEntitlementTo(
      sp => "MyRole",
      DatabasePermissions.Query,
      sp => "MyTable",
      sp => "<your-storage-account.table.core.windows.net>");
  ```

### 5. Use the Repository
- **Query Table:**
  ```csharp
  var repo = serviceProvider.GetRequiredService<IQueryableRepository<MyEntity>>();
  var results = await repo.QueryAsync(...);
  ```

## How Cod.Platform.StorageTable is Consumed
Consumer projects (e.g., Niobium.Invoicing.Core) use Cod.Platform.StorageTable to:
- Register and use Azure Table Storage repositories via DI.
- Secure access to tables with role-based and personalized entitlements.
- Issue SAS tokens for controlled access to storage resources.

## Contributing

Contributions are welcome! To contribute:
1. Fork the repository
2. Create a feature branch
3. Make your changes with clear commit messages
4. Submit a pull request

Please ensure your code follows the existing style and includes appropriate tests and documentation.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
