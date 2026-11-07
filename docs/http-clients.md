# HTTP client transport configuration

Use `Niobium.Platform` for shared HTTP transport configuration. Applications choose
the policy for each client; they should not replace the factory's primary handler
just to change one setting. Replacing it can discard pooling, proxy, cookie or
certificate settings established elsewhere.

## Automatic redirects

`ConfigureAutoRedirect` configures the existing `SocketsHttpHandler` or
`HttpClientHandler`. It supports named clients, typed clients and shared defaults:

```csharp
using Niobium.Platform;

services.AddHttpClient<IProvider, Provider>()
    .ConfigureAutoRedirect(false);

// Optional application-wide policy, with an explicit exception for one client.
services.ConfigureHttpClientDefaults(http => http.ConfigureAutoRedirect(false));
services.AddHttpClient("redirects-required").ConfigureAutoRedirect(true);
```

Existing clients keep their current redirect behavior unless this method is used.
The policy applies after client-specific primary-handler selection, even when a
client selects its custom transport after shared defaults are registered. Explicit
client settings override shared defaults in either registration order; the last
setting at the same scope wins. An unsupported handler fails during client creation
rather than silently ignoring
the requested policy. Delegating handlers, service discovery, connection lifetime
and other transport settings remain intact.

Redirects and retries are separate policies. Disabling redirects prevents a 307/308
response from silently resubmitting a POST to another destination. It does not
disable Niobium's default resilience handlers. Durable operations that own every
attempt must separately remove HTTP resilience retries from their client.

In the current Microsoft.Extensions.Http.Resilience 10.9.0 dependency,
`RemoveAllResilienceHandlers()` is marked experimental (`EXTEXP0001`). Notification
uses the stable `ConfigureAdditionalHttpMessageHandlers` hook to remove
`ResilienceHandler` instances for its durable provider only. Keep that independent
of `ConfigureAutoRedirect(false)`; the framework test explicitly verifies that
redirect configuration preserves an existing resilience pipeline.

## Notification adoption and release

This addition addresses [Notification PR #2's transport review](https://github.com/niobium-nz/Niobium.Notification/pull/2#discussion_r4500908254).
[Framework PR #2](https://github.com/niobium-nz/Niobium/pull/2) was merged as
`f5b1b46a4a8c83e8771d7a3e9ae9dcc7044b62f0`, and
[Publish run 42358398851](https://github.com/niobium-nz/Niobium/actions/runs/42358398851)
released Platform and affected dependents at 4.2.70. Notification PR #2 adopts
`ConfigureAutoRedirect(false)` through its upgraded existing integration references,
preserving the default `SocketsHttpHandler` and separate retry suppression. See
[package upgrades](package-upgrades.md) for the dependency graph and current metadata
limitation; an extra direct Platform reference in Server is unnecessary.

Run composition and provider tests against the actual resolved packages.
Do not treat a source checkout or local candidate package as a published dependency.

The framework PR workflow builds and tests without publication. Merging framework
source changes triggers the existing publish workflow, including dependent packages
selected by its dependency graph. That release requires its own approved PR.

## Verification

`tests/Niobium.Platform.Tests` exercises both supported transports, preservation of
factory connection lifetime and existing settings, named/default policy isolation,
shared defaults with later custom transport selection and explicit client overrides,
resilience-handler preservation and unsupported transports. A real loopback HTTP
server verifies that a redirected POST reaches its destination only when enabled.

```sh
dotnet build tests/Niobium.Platform.Tests/Niobium.Platform.Tests.csproj --configuration Release
dotnet run --project tests/Niobium.Platform.Tests --configuration Release --no-build -- --report-trx
```

The publish workflow runs this suite before its existing publication conditions.
The tests use synthetic requests to loopback only and publish no packages.
