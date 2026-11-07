# Security, authentication and tokens

This guide describes the [inspected framework baseline](README.md#evidence-baseline). It distinguishes application authentication, authorization and backend credentials. A valid token is not, by itself, permission to operate on any tenant or record.

## Do not conflate token types

| Credential | Purpose | Validation/enforcement boundary |
| --- | --- | --- |
| Niobium ID token | Identity evidence presented to an application for access-token exchange | `PrincipalParser.ParseIDPrincipalAsync` uses the configured RSA public key, issuer and supplied audience. |
| Niobium application access token | Application principal, roles and entitlement claims | `PrincipalParser.ParseAsync` uses the application's symmetric secret, issuer and audience. Endpoint policy must still bind the principal to the requested action/resource. |
| Resource token | Delegated direct access to an Azure Table, Blob container or Service Bus queue | `IResourceControl` authorizes a grant; `ISignatureIssuer` signs a backend credential. The backend enforces that credential's scope. This is not another Niobium JWT. |
| Azure service-principal/managed-identity access token | Server-to-service authentication | Backend or target service policy. Not interchangeable with a Niobium application access token. |
| Invoicing invoice-link token | Possession-based access to one invoice's public HTML route | Invoicing's saved-token policy, separate from Niobium Identity and Azure SAS. |

The framework contains identity client/exchange support and token-building primitives. Installing `Niobium.Platform.Identity` does not deploy the central identity provider, its login endpoints, user database or operational policies. Those are outside the four-repository review.

## ID token to access token

[AccessTokenMiddleware](../Niobium.Platform.Identity/AccessTokenMiddleware.cs) intercepts the configured access-token path, default `/token`. The client [IdentityService](../Niobium.Channel.Identity/IdentityService.cs) sends a GET with the ID token in `Authorization: Bearer <ID-token>`.

The middleware validates the ID token using `IDTokenAudience`, then requires GUID values for `ClaimTypes.Sid` (user) and `ClaimTypes.GroupSid` (tenant). It loads the application's role record, starts with `DefaultRole`, adds stored roles, asks registered `IEntitlementDescriptor` implementations for grants, groups resource claims, and calls `ITokenBuilder`.

The [Role model](../Niobium.Platform.Identity/Role.cs) maps tenant to PartitionKey and user to RowKey. Roles are stored as a comma-separated string; its mutation methods uppercase assigned roles. Default-role and descriptor comparisons have their own casing behavior. Use a consistent role vocabulary and test the exact stored and configured strings. Do not assume all role matching is case-insensitive.

[BearerTokenBuilder](../Niobium.Platform.Identity/BearerTokenBuilder.cs) writes `Sid`, `GroupSid`, a `kid` claim, roles and entitlements. With `AccessTokenSecret`, it signs using HMAC-SHA256. Its alternative RSA/private-key path does not mean the ordinary access-token parser accepts RSA access tokens: that parser constructs a symmetric key. Use the builder and parser pair intentionally.

The exchange result is an `Authorization` **response header**, not an OAuth-style JSON response. [DeliverToken](../Niobium.Platform.Identity/HttpRequestExtensions.cs) also exposes that header for cross-origin access. The host still needs the appropriate origin/CORS policy. It should explicitly prevent caching of credential responses and redact headers; the helper itself does not establish all of those controls.

### Validation parameters and lifetime

[PrincipalParser](../Niobium.Platform.Identity/PrincipalParser.cs) requires a signed token and expiration, validates lifetime and issuer, and uses a five-minute clock skew. Audience validation is enabled when a nonblank audience is supplied; a blank audience disables it. Both the ID-token entry point and ordinary access-token options therefore need deliberate audience configuration.

[IdentityServiceOptions](../Niobium.Platform.Identity/IdentityServiceOptions.cs) defaults `TokenValidity` to eight hours. That is the token builder's configured lifetime, not a universal policy for tokens issued by a separately deployed identity provider. [Constants](../Niobium.Identity/Constants.cs) provide generic default issuer/audience strings; do not treat defaults as sufficient isolation between applications or environments.

The inspected parser normalizes parsing/validation failures to the framework authentication-required error. It does not query a role store or revocation service for every request. Roles and entitlements in an already-issued access token are a snapshot. Changing a database role is not evidence that an outstanding token or previously issued SAS was revoked.

Recommended configuration review: explicit issuer and audience, environment-specific secret/key ownership, verified signing algorithms, rotation and emergency-revocation procedures, acceptable lifetime/skew, and negative tests for tokens from another application. The current options `Validate()` is not a complete security configuration validator.

## Authentication middleware is not authorization middleware

[BearerTokenMiddleware](../Niobium.Platform.Identity/BearerTokenMiddleware.cs) tries to parse an access token. On success it sets `HttpContext.User`; otherwise it still calls the next handler. [TryParseAsync](../Niobium.Platform.Identity/PrincipalParserExtensions.cs) returns null for missing/incorrect scheme or parsing failures.

Consequently, `UsePlatformIdentity()` does not make every endpoint private. For each protected operation:

1. Require the expected authenticated principal and claims.
2. Bind tenant and user identifiers to the requested operation; reject conflicting body/route identifiers.
3. Check roles, permissions and record ownership required for that operation.
4. Validate the command and only then invoke business work.

This is a development rule, not a new framework enforcement layer delivered by this documentation. UI `[Authorize]` and `AuthorizeRouteView` control presentation; they do not replace these server checks. A Functions `AuthorizationLevel.Anonymous` binding can intentionally rely on application authorization, but only when that check really exists. Function-key authorization is also a different boundary from end-user identity.

[Invoicing NewInvoice](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/NewInvoice.cs) is a concrete example: it checks `Sid` and `GroupSid`, compares them with `BillerID` and `Tenant`, validates input, and then runs the issuance flow. This does not imply all its endpoints have the same policy; its HTML route deliberately uses an invoice-link token.

Store's CAPTCHA-backed public order path and Notification's public subscription/contact paths must be assessed as their own anonymous workflows. CAPTCHA is an abuse signal, not a tenant/user principal or proof of resource ownership.

## Resource grants and independent lifetimes

[ResourceTokenMiddleware](../Niobium.Platform.Identity/ResourceTokenMiddleware.cs) intercepts the configured `/rsas` path. It parses `type`, `resource`, and optional `partition` and `id`, validates the application access token, and calls the signature service. The client sends the access token in the Authorization header and reads a JSON `StorageSignature` response.

[SignatureService](../Niobium.Platform.Identity/SignatureService.cs) selects matching controls, takes the first non-null grant and requires a single matching issuer. No eligible control produces NotAcceptable; eligible controls returning no grant produce Forbidden; a missing issuer produces ServiceUnavailable. A control that throws can stop processing rather than simply allowing the next control to decide.

The service requests expiry at UTC now plus one day. It does not clamp that time to the access token's expiration. A local `start` variable is calculated but is not passed to the issuer. Do not infer a not-before or revocation policy from it. The response contains the signature, Unix-seconds expiry and granted `StorageControl`.

Resource issuance requires valid application authority at issuance time, but use of the resulting SAS is a separate backend operation. Design short, least-privilege grants and a revocation/rotation story appropriate to the backend; do not assume application logout invalidates an issued SAS. See [resource access](resource-access.md) for exact Table, Blob and queue scope behavior.

## Client-side token state

[DefaultAuthenticator](../Niobium.Channel.Identity/DefaultAuthenticator.cs) decodes tokens using `JsonWebToken`, checks token times, and can exchange a still-valid ID token for another access token. This is not server-side cryptographic token validation or a demonstrated refresh-token rotation protocol. Server endpoints must validate independently.

Resource signatures are cached by type, resource, partition and row, with expiry checks. The base implementation also contains static saved ID/access tokens and a shared resource-token dictionary. Those lifetimes are not inherently isolated by user, tenant or host. Do not adopt this base implementation in a shared multi-user server without an isolation design and tests.

[LocalStorageAuthenticator](../Niobium.Channel.Identity.Blazor/LocalStorageAuthenticator.cs) persists ID/access tokens and resource signatures through JavaScript using `idToken`, `accessToken` and `resourceTokens` keys. Recommended practice: treat that storage as credential-bearing client state, prevent untrusted script execution, review shared-origin applications, and test account switching. Browser display state is not authorization evidence.

The base logout/access-token clearing paths depend on comparisons with saved tokens. Test remembered and non-remembered login, logout, reload and account switching explicitly. Do not promise that every cached token/client is cleared merely because the UI reports a logout. Adapter SDK-client caches are separate from authenticator caches; see the known limitations below.

## Service-to-service identity and messages

Invoicing registers `AddProfile(useServicePrincipalAuthentication: true)`. The framework's [ServicePrincipalProfileService](../Niobium.Platform.Profile/ServicePrincipalProfileService.cs) uses `DefaultAzureCredential` to obtain a token for `api://{ProfileAppID}/.default`. That outbound profile credential is separate from the incoming Niobium access token and from the application's Azure storage identity configuration.

The messaging broker can copy an available application access token into a message's Authorization property. Copying or deserializing that property does not validate it. Consumers must either explicitly validate a delegated principal or rely on a documented private-producer trust boundary. The inspected Invoicing command consumer uses the latter. See [messaging](messaging.md) for delayed-token expiry, sender permissions and settlement implications.

## Invoice-link capabilities are application-specific

[Invoicing InvoiceDomain](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Core/Domains/InvoiceDomain.cs) persists a link token before notification enqueue, reuses an existing saved token, and checks the saved token for HTML rendering. The public HTML endpoint does not require a recipient account. The inspected rendering path does not establish a token expiry policy.

Changing the generation secret does not by itself invalidate already saved links. Keep these link tokens out of logs, referrers, analytics and unrelated messages. Define expiry/revocation and sharing expectations in the application. Do not reuse the token as a Niobium access token or Azure SAS.

## Source-level limitations that must remain visible

These are observations and proposed follow-ups, not fixes in this documentation PR:

- The token builder uses `Dictionary.Add(ClaimTypes.Role, role)` for each role. Multiple roles attempt to add the same key, which can fail token issuance. Fix and test role representation before relying on multi-role exchange; do not remove intended roles as a workaround.
- Audience validation can be disabled by blank configuration. Validate required options and test wrong-audience rejection rather than relying on generic defaults.
- Resource grants can outlive the access token; claims and grants have no per-request revocation lookup in the inspected paths.
- Static authenticator and adapter caches need principal/endpoint/permission/expiry isolation and logout/account-switch tests.
- Token endpoints are matched by path; the middleware itself does not establish a complete HTTP-method, duplicate-query, escaping, cache or rate-limit policy. Placeholder bindings and edge policy need tests.

Use [security review and known limitations](security-review-checklist.md) to turn these observations into scoped implementation work. Do not describe this source review as a production security certification.
