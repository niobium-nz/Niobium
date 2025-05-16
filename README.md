# COD Platform for .NET

**COD** is a .NET framework designed to accelerate the development of modern cloud-based applications. It provides ready-to-use integrations with popular PaaS services like Azure Storage Table, Azure Service Bus, Azure Functions, IoT Hub, and more. The framework also features built-in role-based authentication via JWT backed by a cloud-based identity service.

---

# Database Support with Azure Storage Table

## Nuget Package

```bash
Install-Package Cod.Platform.StorageTable
```

## Getting Started

```csharp
// Configure in Program.cs:
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDatabase(options =>
        {
            // Nothing needs to be set here if on client side to use resource token based authentication

            // Optionally set on service side to use RBAC based authentication
            options.FullyQualifiedDomainName = "test.table.core.windows.net";

            // Optionally set on service side to use Key based authentication
            options.Key = "test.table.core.windows.net";
        })

        // Optionally enable interactive authentication in development environment on server side
        .PostConfigure<StorageTableOptions>(opt => opt.EnableInteractiveIdentity = context.Configuration.IsDevelopmentEnvironment());
    })
    .Build()
    .Run();

// CURD Example
class User
{
    [EntityKey(EntityKeyKind.PartitionKey)] public required string Prefix { get; set; }

    [EntityKey(EntityKeyKind.RowKey)] public required Guid ID { get; set; }

    // Timestamp is optional. Can be useful for auditing purposes
    [EntityKey(EntityKeyKind.Timestamp)] public DateTimeOffset? Timestamp { get; set; }

    // ETag is optional. Can be useful for optimistic concurrency control
    [EntityKey(EntityKeyKind.ETag)] public string? ETag { get; set; }

    public string Name { get; set; }

    public int Age { get; set; }

    public bool Disabled { get; set; }
}

// Get the repository for User from dependency injection container
var repo = host.Services.GetRequiredService<IRepository<User>>();

// Create a new user
var user = await repo.CreateAsync(new User { Prefix = "A", ID = Guid.NewGuid(), Name = "Alice", Age = 30 });

// Update the user
user.Age = 31;
await repo.UpdateAsync(user);

// Query for the user
var result = await repo.GetAsync("A", user.ID);

// Delete the user
await repo.DeleteAsync(user);
```

---

# Messaging Support with Azure Service Bus

## Nuget Package

```bash
Install-Package Cod.Platform.ServiceBus
```

## Getting Started

```csharp
// Configure in Program.cs:
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMessaging(options =>
        {
            // RBAC should be correctly configured and it is the only supported authentication method
            options.FullyQualifiedNamespace = "test.servicebus.windows.net";

            // Use Web Socket transport in case if this is a Blazor.NET app, or it's server environment but you are behind a firewall
            options.UseWebSocket = true;
        })

        // Optionally enable interactive authentication in development environment
        .PostConfigure<ServiceBusOptions>(opt => opt.EnableInteractiveIdentity = context.Configuration.IsDevelopmentEnvironment());
    })
    .Build()
    .Run();

// Send message example
class UserCreated { public Guid ID; public string Name; public int Age; }

// Get the sender for UserCreated from dependency injection container
var sender = host.Services.GetRequiredService<IMessagingBroker<UserCreated>>();

// Create and send a new message
await sender.EnqueueAsync(new MessagingEntry<UserCreated>
{
    ID = Guid.NewGuid(),
    Value = new UserCreated { ID = Guid.NewGuid(), Name = "Alice", Age = 30 }
});
```

---

# Email Notification via Resend

## Nuget Package

```bash
Install-Package Cod.Platform.Notification.Email.Resend
```

## Getting Started

```csharp
// Configure in Program.cs:
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddNotification(options =>
        {
            options.GlobalAPIKey = "YOUR RESEND API KEY";
        });
    })
    .Build()
    .Run();

// Get the email sender from dependency injection container
var sender = host.Services.GetRequiredService<IEmailNotificationClient>();

// Send a simple email
await sender.SendAsync("noreply@example.com", ["recipient@example.com"], "Hello", "<strong>Hi</strong>");
```

---

# Authentication & Authorization

COD provides a full authentication flow with support for:

- **ID Tokens** (issued by central Identity API)
- **Access Tokens** (issued by tenant applications)
- **Resource Tokens** (for resource-level access control)

## Server-side Integration (Azure Function)

### Nuget Package

```bash
Install-Package Cod.Platform.Identity
```

### Configure in `Program.cs`

```csharp
Host.CreateDefaultBuilder(args)
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UsePlatformIdentity();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddIdentity(options =>
        {
            // Optional. The URL for hosting the endpoint in the current web app so clients cloud request for access token. Default value: "token".
            options.AccessTokenEndpoint = "token";

            // Optional. The Role claim apart from the access token issued. Default value: "User".
            options.DefaultRole = "User";

            // Optional. The URL for hosting the endpoint in the current web app so clients cloud request for resource token. Default value: "rsas". For resource authorization only.
            options.ResourceTokenEndpoint = "rsas";

            // The audience of the ID token issued. It should be defined as a unique ID that could be used to identify an unique tenant application to the centralized Cod.Platform.Identity.API service.
            options.IDTokenAudience = "00000000-1111-2222-3333-444444444444";

            // Optional. The audience of the access token issued to the client of the current application. Default value: "cod.client".
            options.AccessTokenAudience = "myapp.client";

            // Optional. The issuer of the access token issued to the client of the current application. Default value: "cod.platform".
            options.AccessTokenIssuer = "myapp.API";

            // The secret used to issue access token to the client.
            options.AccessTokenSecret = "01223456789012234567890122345678912";

            // Please use this value when integrating to the Cod.Platform.Identity.API service. This public key is used to verify the ID token issued by the Cod.Platform.Identity.API service to the client of the current application to exchange for access token issued by the current application.
            options.IDTokenPublicKey = "-----BEGIN PUBLIC KEY-----MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAlmuIHhRUT+IvTdTajuOEdPZ75pqJrajOyDdJqf42siofTKHCPCZqtWaftL8Jae5VMHUNCflXzm9aKTLgerAxrGyCL3BkZj/dj4KjbeFoY1Akz5EJd5qjRNtzdoqUkDsuaigJTDDhyHvdVKRHzYAJSKgiw9CLRkzyKDZAp1Fxm2xICzRoF9OfH0jv5Qn11gkMHrkVkB6nv7QD3EGS2rnDwKudjnlsvgjpoNMMNvY7EHqkuxjHdGh4Hy7M3UXl50Bft9ky3gKeCgpvVYRPXHdT6k3e+81J7liEJYdVb5taPeNf3wB8WCP8Xc7FO6vod9ziDIkJv+xqUWYtlyGZBGTBrQIDAQAB-----END PUBLIC KEY-----";
        });
    })
    .Build()
    .Run();
```

### Placeholder Functions

Define your own authentication endpoint as a placeholder for Azure Function app runtime to register this route. No implementation is needed, just make sure the Route matches to what's configured above.

```csharp
public class Placeholder
{
    [Function(nameof(Auth))]
    public IActionResult Auth([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Cod.Identity.Constants.DefaultAccessTokenEndpoint)] HttpRequest req) => new OkResult();
}
```

### Resource Token Support with Azure Storage Table (Optional)

Install the package (same as for StorageTable), then enable in startup:

```csharp
services.AddDatabase(...dbOptions...)
    .AddDatabaseResourceTokenSupport(...identityOptions...)

    // Optionally grant query permission to a specific role to query a specific table on records where PartitionKey == <user-id>
    .GrantDatabasePersonalizedEntitlementTo(
        "<role-to-grant-permission-to>",
        Cod.DatabasePermissions.Query,
        "<table-to-grant-permission-to>",
        "<fully-qualified-domain-name-to-storage-account>");
```

Define your own resource token endpoint as a placeholder for Azure Function app runtime to register this route. No implementation is needed, just make sure the Route matches to what's configured above.

```csharp
public class Placeholder
{
    [Function(nameof(RSAS))]
    public IActionResult RSAS([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = Cod.Identity.Constants.DefaultResourceTokenEndpoint)] HttpRequest req) => new OkResult();
}
```

---

## Frontend Integration with Blazor WebAssembly

### Nuget Package

```bash
Install-Package Cod.Channel.Identity.Blazor
```

### Configure in `Program.cs`

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddIdentityBlazor(options =>
{
    options.App = Guid.Parse("<same-as-IDTokenAudience>");
    options.IDTokenHost = "<URL-to-Cod.Platform.Identity.API>";
    options.AccessTokenHost = "<tenant-server-url>";
    options.ResourceTokenHost = "<tenant-server-url>";
});

var app = builder.Build();
await app.Services.InitializeAsync();
await app.RunAsync();
```

### Update `_Imports.razor`

```razor
@using Microsoft.AspNetCore.Authorization
@using Microsoft.AspNetCore.Components.Authorization
@using Cod.Channel.Identity
@using Cod.Channel.Identity.Blazor
@attribute [Authorize]
```

### Update `App.razor`

```razor
<CascadingAuthenticationState>
    <Router AppAssembly="@typeof(App).Assembly">
        <Found Context="routeData">
            <AuthorizeRouteView RouteData="@routeData">
                <NotAuthorized>
                    <RedirectToLogin />
                </NotAuthorized>
            </AuthorizeRouteView>
            <FocusOnNavigate RouteData="@routeData" Selector="h1" />
        </Found>
        <NotFound>
            <p>Sorry, nothing found.</p>
        </NotFound>
    </Router>
</CascadingAuthenticationState>
```

### Create login page (e.g., `Login.razor`)

```razor
@page "/login"
@attribute [AllowAnonymous]
<EmailLogin />
```

---

## License

This project is released under the MIT License.