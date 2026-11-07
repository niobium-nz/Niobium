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
For a custom primary handler, register the handler before applying this setting.
An unsupported handler fails during client creation rather than silently ignoring
the requested policy. Delegating handlers, service discovery, connection lifetime
and other transport settings remain intact.

Redirects and retries are separate policies. Disabling redirects prevents a 307/308
response from silently resubmitting a POST to another destination. It does not
disable Niobium's default resilience handlers. Durable operations that own every
attempt must separately remove HTTP resilience retries from their client.

## Notification adoption and release

This addition addresses [Notification PR #2's transport review](https://github.com/niobium-nz/Niobium.Notification/pull/2#discussion_r4500908254).
Notification currently consumes Niobium.Platform 4.0.60, which has no shared redirect
setting. Its durable provider must keep redirects disabled until it can consume a
published version containing this extension. Removing the local handler first would
restore automatic redirects and weaken the submission protocol.

After the framework change is reviewed and released, pin the released package in
Notification, replace its primary-handler factory with `ConfigureAutoRedirect(false)`,
and retain the separate suppression of HTTP retries. Run its composition and provider
tests against the actual packages, including the default `SocketsHttpHandler`.
Do not treat a source checkout or local candidate package as a published dependency.

The framework PR workflow builds and tests without publication. Merging framework
source changes triggers the existing publish workflow, including dependent packages
selected by its dependency graph. That release requires its own approved PR.

## Verification

`tests/Niobium.Platform.Tests` exercises both supported transports, preservation of
factory connection lifetime and existing settings, named/default policy isolation,
resilience-handler preservation and unsupported transports. A real loopback HTTP
server verifies that a redirected POST reaches its destination only when enabled.

```sh
dotnet build tests/Niobium.Platform.Tests/Niobium.Platform.Tests.csproj --configuration Release
dotnet run --project tests/Niobium.Platform.Tests --configuration Release --no-build -- --report-trx
```

The publish workflow runs this suite before its existing publication conditions.
The tests use synthetic requests to loopback only and publish no packages.
