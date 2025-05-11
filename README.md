# Introduction 
COD is a .NET framework for building modern cloud-based web applications. With COD, you can easily get yourself start consuming some popular cloud-based PaaS products, such as Azure Storage Table, Azure Storage Queue, Azure Service Bus, Azure Functions, IoT Hub, etc. This framework also has a built-in support for authentication with role based access control based on cloud-based database backed up by JWT.

# Getting Started

## Enable Database support by using Azure Storage Table as backend implementation
```nuget
Package Manager: Install-Package Cod.Platform.StorageTable
```
Example of configuration and CURD
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

## Enable Messaging support by using Azure Service Bus as backend implementation
```nuget
Package Manager: Install-Package Cod.Platform.ServiceBus
```
Example of configuration and sending a message
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

## Enable Email Notification support by using Resend as backend implementation
```nuget
Package Manager: Install-Package Cod.Platform.Notification.Email.Resend
```
Example of configuration and sending an email
```csharp
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        .AddNotification(options =>
        {
            options.GlobalAPIKey = "YOUR RESEND API KEY";
        });
    })
    .Build();

// Get the email sender from dependency injection container
var sender = host.Services.GetRequiredService<IEmailNotificationClient>();

// Send an email
await sender.SendAsync(
    "noreply@example.com", // Email address where the email is sent from
    ["recipient1@exmaple.com", "recipient2@exmaple.com", "recipient2@exmaple.com"], // Email address(es) where the email is sent to
    "Hello World!", // Email subject
    "<strong>hello</strong>"); // Email body in HTML format

// Don't forget to run your host
host.Run();
```

## Enable SSO Authentication support by integrating to Cod.Platform.Identity.API
```nuget
Package Manager: Install-Package Cod.Platform.Identity
```
Terminology
* ID Token: The token issued by the Cod.Platform.Identity.API service to the clients of the tenant applications to exchange for access token issued by the tenant applications. The tenant application verifies the signature of the ID token on whether it's issued by the Cod.Platform.Identity.API service. The ID token contains the global user ID in context of the Cod.Platform.Identity.API service so tenant applications can use this ID to identify the user in their own context.
* Access Token: The token issued by the tenant application to the its clients for authentication purposes in scope of the tenant application. The access token contains the global user ID derived from the ID token, along with other tenant specific claims.
* Resource Token: The token issued by the tenant application to the clients of the tenant application for authorization purposes in scope of the tenant application. The resource token contains resource-specific authorization information (such as shared access signature) so clients can use such information to access specific resource directly with specific access limitations set by the resource token.

Example of configuration and sending an email
```csharp

// for Azure Function App
Host.CreateDefaultBuilder(args)
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UsePlatformIdentity();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddIdentity(options => {
            options.AccessTokenEndpoint = "token" // Optional. The URL for hosting the endpoint in the current web app so clients cloud request for access token. Default value: "token".
            options.DefaultRole = "User" // Optional. The Role claim apart from the access token issued. Default value: "User".
            options.ResourceTokenEndpoint = "rsas" // Optional. The URL for hosting the endpoint in the current web app so clients cloud request for resource token. Default value: "rsas".
            options.IDTokenAudience = "00000000-1111-2222-3333-444444444444" // The audience of the ID token issued. It should be defined as a unique ID that could be used to identify an unique tenant application to the centralized Cod.Platform.Identity.API service.
            options.AccessTokenAudience = "myapp.client" // Optional. The audience of the access token issued to the client of the current application. Default value: "cod.client".
            options.AccessTokenIssuer = "myapp.API" // Optional. The issuer of the access token issued to the client of the current application. Default value: "cod.platform".
            options.AccessTokenSecret = "01223456789012234567890122345678912" // The secret used to issue access token to the client.
            options.IDTokenPublicKey = "-----BEGIN PUBLIC KEY-----MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAlmuIHhRUT+IvTdTajuOEdPZ75pqJrajOyDdJqf42siofTKHCPCZqtWaftL8Jae5VMHUNCflXzm9aKTLgerAxrGyCL3BkZj/dj4KjbeFoY1Akz5EJd5qjRNtzdoqUkDsuaigJTDDhyHvdVKRHzYAJSKgiw9CLRkzyKDZAp1Fxm2xICzRoF9OfH0jv5Qn11gkMHrkVkB6nv7QD3EGS2rnDwKudjnlsvgjpoNMMNvY7EHqkuxjHdGh4Hy7M3UXl50Bft9ky3gKeCgpvVYRPXHdT6k3e+81J7liEJYdVb5taPeNf3wB8WCP8Xc7FO6vod9ziDIkJv+xqUWYtlyGZBGTBrQIDAQAB-----END PUBLIC KEY-----" // Please use this value when integrating to the Cod.Platform.Identity.API service. This public key is used to verify the ID token issued by the Cod.Platform.Identity.API service to the client of the current application to exchange for access token issued by the current application.
        });
    })
    .Build()
    .Run();

```
