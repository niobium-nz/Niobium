# Architecture and development conventions

See the [review baseline](README.md#evidence-baseline) for the source and consumer versions behind this guide. This document describes the recurring structure in the code, not a requirement that every application use every integration.

## Package and application boundaries

Niobium separates business-facing contracts from adapters and host composition:

| Area | Responsibility and examples |
| --- | --- |
| `Niobium.Abstractions` | Domains, repositories, events, keys, validation/error and permission shapes. `GenericDomain<TEntity>` is the rich-domain base. |
| Feature contracts | `Niobium.Identity`, `Niobium.File`, `Niobium.Messaging`, `Niobium.Profile` and `Niobium.Finance` provide feature-facing contracts and helpers. |
| Backend adapters | `Niobium.Database.StorageTable`, `Niobium.File.Blob`, `Niobium.Messaging.ServiceBus` translate those contracts into backend operations. |
| `Niobium.Platform.*` | Server composition, middleware, credential/resource issuance and provider integrations. Functions adapts the middleware model to the Functions worker. |
| `Niobium.Channel.*` | Client-side identity, presentation and Blazor integration. Client state and route visibility are not server authorization. |
| Consumer `Contracts` | Application wire/storage models, request validation and stable commands/events. |
| Consumer `Core` | Rich domains, application flows, event adaptations and business policies. |
| Consumer `Server` / `Functions` | Trigger bindings, entry validation and authorization, options binding, transport/provider composition and telemetry. |
| Consumer infrastructure | Tables, containers, queues, identities, permissions, application settings and deployment. Registration does not provision a complete environment. |

The repository also contains speech, device, notification, payment and other integrations. Their presence is not evidence that the three reviewed consumers use all of them. Start with the relevant package README, then inspect the actual registration and implementation.

## The recurring execution pattern

An entry point validates the transport input and establishes its authority. A flow coordinates domains and external services. A domain owns meaningful mutations and persistence through `IRepository<TEntity>`. Adapters own SDK details. Commands and external events cross service boundaries through `IMessagingBroker<T>`.

For example, [Store's OrderFlow](https://github.com/niobium-nz/Niobium.Store/blob/db600f117002509c932d8d60321f9892f8cb7263/src/Niobium.Store.Core/Flows/OrderFlow.cs) obtains a quote, asks the order domain to create the order and construct a charge, then calls the payment boundary. Its [OrderDomain](https://github.com/niobium-nz/Niobium.Store/blob/db600f117002509c932d8d60321f9892f8cb7263/src/Niobium.Store.Core/Domains/OrderDomain.cs) owns order transitions and notification/invoice command construction. These examples establish responsibility placement; they do not establish atomic payment or duplicate suppression.

Keep mutable persistence snapshots separate from the domain API. Prefer operations such as `PrepareAttemptAsync`, `EnsureAccessTokenAsync` or `SettleAsync` to a flow setting arbitrary entity properties and saving. Read [rich domains and data access](domain-data-access.md) for initialization, caching, key mapping, ETags and force-save semantics.

Do not create application-specific copies of Table mappers or repository implementations merely to fill a missing shared feature. First examine the abstraction and adapter. A backend-specific optimization needs an explicit boundary, tests and compatibility decision rather than leaking SDK types throughout business code.

## Composition: register services and install middleware

`Add...` registration and `Use...` pipeline installation are different operations. Calling `AddIdentity` alone does not cause Functions requests to pass through the identity middleware.

The inspected [Invoicing Program.cs](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/Program.cs) follows this order:

1. Create the Functions application builder and configure ASP.NET Core integration.
2. Call its application composition method, which registers Functions support, identity, profile, messaging, database and Core.
3. Install `UsePlatformIdentity` through `ToMiddlewareHost()`.
4. Build and run.

Use [Identity DependencyModule](../Niobium.Platform.Identity/DependencyModule.cs), [Functions DependencyModule](../Niobium.Platform.Functions/DependencyModule.cs), and the actual consumer startup as the source of truth for overloads. Identity installs platform error handling, bearer principal population, access-token exchange and resource-token middleware. Endpoint authorization remains explicit; see [security](security-and-tokens.md).

Functions token routes need registered placeholder functions. Their paths must agree with `AccessTokenEndpoint` and `ResourceTokenEndpoint` and with the deployed route-prefix/proxy arrangement. A placeholder returning OK is not evidence that exchange middleware intercepted the request. Test for the expected token header or resource-token body.

`RegisterDomainComponents` discovers flows, domains and handlers in the supplied assembly; it does not discover arbitrary assemblies automatically. The host still selects an `IRepository<>` backend. See the existing [registration explanation](domain-data-access.md#initialization-and-registration).

### Registration lifetime caveat

Several `DependencyModule` classes have process-wide static guards such as `loaded`, `added` or `used`. This is the current implementation, not per-service-collection idempotency. A second independently created host in one process can skip registration. Factory and token caches can also be static. Test multi-host and multi-identity composition explicitly, or isolate tests by process until those lifetimes are changed centrally. Do not register domains as shared locks or reuse cached domain objects as a cross-request consistency mechanism.

## Configuration conventions

Consumer modules bind sections named after options classes. Examples include `StorageTableOptions`, `ServiceBusOptions`, `IdentityServiceOptions`, `NotificationOptions`, `BillingOptions`, and the consumer-specific `NotificationQueueOptions` / `InvoiceQueueOptions`.

Keep related business settings together; keep provider credentials at the provider boundary. A per-command broker options object chooses a destination namespace while the broker derives the queue name from the message type. Do not assume an identically named options class in different namespaces is interchangeable: Channel and Platform both define identity options with different purposes.

Some adapters also consult conventional configuration keys. Read their constants and factory rather than guessing the binding behavior. Development detection changes credentials and can select a logging-only messaging broker. Check the actual environment classification before diagnosing missing messages.

Recommended practice: require production-critical configuration at startup, supply secrets through the deployment's approved secret mechanism, and fail clearly on an incomplete environment. The framework options validators do not enforce every security policy, issuer/audience choice or infrastructure prerequisite.

## Coding and compatibility rules

Follow the repository `.editorconfig` and existing local style. The established domain guide also specifies:

- Use enums for finite business states/outcomes, with deliberate compatible persisted values. Do not assume the Table mapper serializes arbitrary enums correctly.
- Execute I/O and state-changing operations before an `if`, name the result, then test it. Pure predicates may remain in conditions.
- Keep public contracts, key formatting, table names, message names and stored representations stable unless a migration is scoped.
- Propagate cancellation where supported and distinguish a cancelled/ambiguous external operation from a proven failed write.

A class rename can change a default table or queue name. A property addition can change Table mapping. A new command field can affect an idempotency fingerprint. Review wire, storage and infrastructure contracts together, not only C# compilation.

When adding XML documentation, explain invariants, authority requirements, side effects, failure behavior and ownership of returned resources. Do not describe desired behavior as implemented or repeat a method name without explaining its contract.

## Errors, logging and outbound calls

[ErrorHandlingMiddleware](../Niobium.Platform/ErrorHandlingMiddleware.cs) catches Niobium `ApplicationException`, chooses a status code and writes a generic or validation payload. It is not a catch-all for arbitrary SDK or runtime exceptions. Its public payload can contain `Message` and `Reference`; its logging also includes `InternalMessage`. Keep credentials, raw tokens, sensitive bodies and personal data out of all of them.

HTTP validation helpers and queue consumers can follow different failure paths. A queue handler that logs and returns is not the same as one that throws. Define each boundary's retry/dead-letter policy explicitly instead of assuming the error middleware supplies it.

[Platform registration](../Niobium.Platform/DependencyModule.cs) adds standard HTTP resilience and service discovery. Redirects and retries are separate policies. Follow [HTTP client configuration](http-clients.md) when a durable flow owns provider attempts, as Notification does.

The ASP.NET Core platform registration clears known proxy/network collections in forwarded-header options. Recommended practice: verify trusted ingress and network restrictions before treating forwarded addresses as trustworthy audit or risk signals. This is not a statement that every Functions host executes that same ASP.NET pipeline.

## Development and review workflow

The framework workflow uses .NET 10 and the repository solution; inspect `global.json` and the workflow before running commands. Build/test commands are documented in [HTTP client configuration](http-clients.md) and the affected consumer's own guides. Unit tests should exercise domain decisions with fakes; adapter tests should cover real mapping, conditional writes and failures; host tests should verify middleware, principal propagation and negative authorization cases.

The documentation review did not execute those suites. A future implementation PR should report commands actually run, the packages actually resolved, and skipped live-service checks. Do not mark a fake-backed test as proof of Azure authorization or message delivery.

Keep release, consumer upgrades and deployment explicit. Existing integration references should be upgraded coherently after publication, without redundant lower-level pins or silent local `ProjectReference` substitution. See [package upgrades](package-upgrades.md) and the [security checklist](security-review-checklist.md).
