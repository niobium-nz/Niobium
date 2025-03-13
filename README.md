# Introduction 
COD is a .NET framework for building modern cloud-based web applications. With COD, you can easily get yourself start consuming some popular cloud-based PaaS products, such as Azure Storage Table, Azure Storage Queue, Azure Service Bus, Azure Functions, IoT Hub, etc. This framework also has a built-in support for authentication with role based access control based on cloud-based database backed up by JWT.

# Getting Started

## Azure Storage Table as Database Backend
```nuget
Package Manager: Install-Package Cod.Platform.StorageTable -Version 2.2.11
```
Example of enabling StorageTable support
```csharp

Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Enable interactive authentication to Storage Account in development environment
        var isDevelopment = context.Configuration.IsDevelopmentEnvironment();
        services.AddDatabase(context.Configuration.GetRequiredSection(nameof(StorageTableOptions)))
                .PostConfigure<StorageTableOptions>(opt => opt.EnableInteractiveIdentity = isDevelopment);
    })
    .UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .Build()
    .Run();
```
