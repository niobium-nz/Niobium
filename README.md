# Niobium Platform for .NET

Start with the [developer documentation](docs/README.md) for architecture, conventions, security, tokens, resource grants, file access, messaging and examples from Store, Notification and Invoicing.

Read [rich domains and data access](docs/domain-data-access.md) before business or persistence changes.
Read [HTTP client configuration](docs/http-clients.md) before changing outbound transports.
Read [package releases and consumer upgrades](docs/package-upgrades.md) before changing framework package references.

**Niobium** is a .NET framework designed to accelerate the development of modern cloud-based applications. It provides integrations with PaaS services including Azure Storage Table, Azure Service Bus, Azure Functions and IoT Hub, alongside identity, messaging, file and other application abstractions.

The snippets below are introductory examples, not a complete production security configuration or compiled sample application. Use the detailed guides and the APIs of your resolved packages when implementing a consumer. In particular, [identity middleware does not automatically authorize every endpoint](docs/security-and-tokens.md#authentication-middleware-is-not-authorization-middleware), and [server credentials and delegated resource tokens are different access paths](docs/resource-access.md).

---

# Database Support with Azure Storage Table

## Nuget Package

```bash
Install-Package Niobium.Platform.StorageTable
```

## Getting Started

```csharp
// Configure in Program.cs:
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDatabase(options =>
        {
            // Leave the server endpoint unset for the resource-token client path.
            // That path separately requires an authenticator and authorized grants.

            // Set on the server to select DefaultAzureCredential authentication.
            options.FullyQualifiedDomainName = "test.table.core.windows.net";

            // A Key, when needed by the resource-token issuer, is a server-only secret.
            // It is not the ordinary repository authentication method in current source.
        })

        // Optionally enable interactive authentication in development environment on server side
        .PostConfigure<StorageTableOptions>(opt => opt.EnableInteractiveIdentity = context.Configuration.IsDevelopmentEnvironment());
    })
    .Build()
    .Run();

// CRUD Example
class User
{
    [EntityKey(EntityKeyKind.PartitionKey)] public required string Prefix { get; set; }

    [EntityKey(EntityKeyKind.RowKey)] public required Guid ID { get; set; }

    // Timestamp is optional. Can be useful for auditing purposes
    [EntityKey(EntityKeyKind.Timestamp)] public DateTimeOffset? Timestamp { get; set; }

    // A mapped ETag is needed for the conditional update path; see the data-access guide.
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

For meaningful business mutations, use a rich domain rather than copying direct CRUD into every flow. Verify ETag read/write behavior and concurrency requirements in [rich domains and data access](docs/domain-data-access.md).

---

# Messaging Support with Azure Service Bus

## Nuget Package

```bash
Install-Package Niobium.Platform.ServiceBus
```

## Getting Started

See [messaging and delivery](docs/messaging.md) before selecting queue names, credentials, stable message IDs, parsing or acknowledgement behavior. The adapter supports configured-namespace server credentials and a separate resource-token path; development mode can select a logging-only broker.

```csharp
// Configure in Program.cs:
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddMessaging(options: options =>
        {
            // Select DefaultAzureCredential for this namespace; provision data-plane rights.
            options.FullyQualifiedNamespace = "test.servicebus.windows.net";

            // Select AMQP over WebSockets when required by the environment.
            options.UseWebSocket = true;
        })

        // Optionally enable interactive authentication in development environment
        .PostConfigure<ServiceBusOptions>(opt => opt.EnableInteractiveIdentity = context.Configuration.IsDevelopmentEnvironment());
    })
    .Build()
    .Run();
```

Use a message contract implementing `IDomainEvent`, a string message ID stable across retries, and `IMessagingBroker<T>`. The [consumer guide](docs/consumer-patterns.md) links concrete `NotifyCommand` and `IssueInvoiceCommand` producers and consumers. A successful send is not proof that an external business action completed, and domain callbacks are not a transactional outbox.

---

# Email Notification via Resend

## Nuget Package

```bash
Install-Package Niobium.Platform.Notification.Email.Resend
```

## Getting Started

```csharp
// Configure in Program.cs:
Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddNotification(options =>
        {
            // Supply through the server's approved secret configuration.
            options.GlobalAPIKey = context.Configuration["ResendApiKey"];
        });
    })
    .Build()
    .Run();

// Get the email sender from dependency injection container
var sender = host.Services.GetRequiredService<IEmailNotificationClient>();

// Send a simple email
await sender.SendAsync("noreply@example.com", ["recipient@example.com"], "Hello", "<strong>Hi</strong>");
```

This is provider integration, not the Notification application's durable-submission policy. See [Notification's consumer pattern](docs/consumer-patterns.md#notification-file-backed-rendering-and-explicit-attempt-ownership) and [HTTP client configuration](docs/http-clients.md) for attempt ownership, redirects and retries.

---

# Authentication & Authorization

Read [security, authentication and tokens](docs/security-and-tokens.md) before implementation. Niobium distinguishes:

- **ID tokens**, presented to an application for access-token exchange.
- **Application access tokens**, carrying identity, roles and entitlements.
- **Resource tokens**, granting direct backend access within the issued scope.

Service-principal tokens and application-specific invoice-link capabilities are separate credentials. Installing an identity package does not deploy the central identity provider or make every endpoint private.

## Server-side Integration (Azure Functions)

### Nuget Package

```bash
Install-Package Niobium.Platform.Identity
```

Register identity services and install `UsePlatformIdentity` in the actual host pipeline. [Invoicing's composition](docs/architecture-and-conventions.md#composition-register-services-and-install-middleware) demonstrates the Functions builder and middleware-host pattern. Configure explicit issuer/audience values, trusted ID-token public key and the server-only application signing secret through the approved configuration mechanism. Do not copy a sample secret or assume a generic default audience isolates applications.

The exchange middleware returns an access token in the Authorization response header. Resource-token middleware returns a `StorageSignature` JSON response. See [the protocol and current limitations](docs/security-and-tokens.md) for validation, lifetimes, role handling, browser state and logout concerns.

### Placeholder Functions

Register placeholder Functions routes matching the configured access-token and resource-token paths, accounting for the deployed route prefix and proxy arrangement. For example:

```csharp
public class Placeholder
{
    [Function(nameof(Auth))]
    public IActionResult Auth(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get",
            Route = Niobium.Identity.Constants.DefaultAccessTokenEndpoint)] HttpRequest req)
        => new OkResult();
}
```

The placeholder's OK result does not prove token middleware intercepted the request. Test the expected token response and negative authentication cases. Protected business endpoints still need explicit principal, tenant, permission and ownership checks.

### Resource Token Support

[Resource authorization](docs/resource-access.md) documents `AddDatabaseResourceTokenSupport`, `AddBlobResourceTokenSupport` and `AddServiceBusResourceTokenSupport`, their entitlement descriptors, control/issuer responsibilities and credential prerequisites.

Invoicing provides examples of personalized Table grants and ownership-controlled child data. The default Blob issuer grants a container, not a single file. Review the actual scope and known cache/configuration limitations before exposing direct backend access.

---

## Frontend Integration with Blazor WebAssembly

### Nuget Package

```bash
Install-Package Niobium.Channel.Identity.Blazor
```

### Configure in `Program.cs`

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddIdentityBlazor(options =>
{
    options.App = Guid.Parse("<same-as-IDTokenAudience>");
    options.IDTokenHost = "<URL-to-identity-service>";
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
@using Niobium.Channel.Identity
@using Niobium.Channel.Identity.Blazor
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

Client route visibility does not authorize backend operations. Review [client token state](docs/security-and-tokens.md#client-side-token-state), credential-bearing browser storage and the [security checklist](docs/security-review-checklist.md), including non-remembered login, logout, account switching and SDK-client caches.

---

## License

This project is released under the MIT License.
