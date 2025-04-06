# Introduction 
COD is a .NET framework for building modern cloud-based web applications. With COD, you can easily get yourself start consuming some popular cloud-based PaaS products, such as Azure Storage Table, Azure Storage Queue, Azure Service Bus, Azure Functions, IoT Hub, etc. This framework also has a built-in support for authentication with role based access control based on cloud-based database backed up by JWT.

# Getting Started

## Azure Storage Table as Database Backend
```nuget
Package Manager: Install-Package Cod.Platform.StorageTable
```
Example of enabling StorageTable support
```csharp
class User
{
    [EntityKey(EntityKeyKind.PartitionKey)]
    public required string Prefix { get; set; }

    [EntityKey(EntityKeyKind.RowKey)]
    public required Guid ID { get; set; }

    // Timestamp is optional
    [EntityKey(EntityKeyKind.Timestamp)]
    public DateTimeOffset? Timestamp { get; set; }

    // ETag is optional so it is only required for optimistic concurrency
    [EntityKey(EntityKeyKind.ETag)]
    public string? ETag { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }

    public bool Disabled { get; set; }
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDatabase(options =>
        { 
            // RBAC should be correctly configured and it is the only supported authentication method
            options.FullyQualifiedDomainName = "test.table.core.windows.net";
        })

        // Optionally enable interactive authentication to Storage Account in development environment
        .PostConfigure<StorageTableOptions>(opt => opt.EnableInteractiveIdentity = context.Configuration.IsDevelopmentEnvironment());
    })
    .UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .Build();

// Get the repository for User from dependency injection container
var repo = host.Services.GetRequiredService<IRepository<User>>();

// Create a new user
var user1 = await repo.CreateAsync(new User 
{
    Prefix = "A",
    ID = Guid.NewGuid(),
    Name = "Alice",
    Age = 30,
    Disabled = false
});

// Update the user
user1.Age = 31;
await repo.UpdateAsync(user1);

// Query the user
var user2 = await repo.GetAsync("A", user1.ID);

// Delete the user
await repo.DeleteAsync(user1);

// Don't forget to run your host
host.Run();
```


## Azure Service Bus as Messaging Backend
```nuget
Package Manager: Install-Package Cod.Platform.ServiceBus
```
Example of enabling Service Bus support
```csharp
class UserCreated
{
    public Guid ID { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMessaging(options =>
        { 
            // RBAC should be correctly configured and it is the only supported authentication method
            options.FullyQualifiedNamespace = "test.servicebus.windows.net";

            // Use Web Socket transport in case if this is a Blazor.NET app, or it's server environment but you are behind a firewall
            options.UseWebSocket = true
        })

        // Optionally enable interactive authentication to Storage Account in development environment
        .PostConfigure<ServiceBusOptions>(opt => opt.EnableInteractiveIdentity = context.Configuration.IsDevelopmentEnvironment());
    })
    .UseDefaultServiceProvider((_, options) =>
    {
        options.ValidateScopes = true;
        options.ValidateOnBuild = true;
    })
    .Build();

// Get the message sender for UserCreated from dependency injection container
var sender = host.Services.GetRequiredService<IMessagingBroker<UserCreated>>();

// Send a new message
await sender.EnqueueAsync(new MessagingEntry<UserCreated>
{
    ID = Guid.NewGuid(),
    Value = new UserCreated 
    {
        ID = Guid.NewGuid(),
        Name = "Alice",
        Age = 30,
    }
});

// Don't forget to run your host
host.Run();
```
