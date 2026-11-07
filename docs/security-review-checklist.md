# Security review checklist and known limitations

This is a source-review checklist, not a penetration-test report or a claim about live infrastructure. It applies to the [documented commits](README.md#evidence-baseline). No runtime fix, credential rotation, RBAC change or deployment is delivered by this documentation.

Use the source observations below to scope separate implementation work. First reproduce behavior with the consumer's actual resolved packages, then add regression tests and fix the shared abstraction/adapter where appropriate. Do not silently turn current bugs into prescribed conventions.

## Review gates for a new feature

| Gate | Evidence required before release |
| --- | --- |
| Authority | Identify anonymous, user-delegated, trusted-service or possession-based entry points. Name the enforcing component and negative tests. |
| Tenant/record binding | Show how validated identity constrains body/route IDs, partition keys, file paths and queue commands. Do not trust a tenant field solely because it deserialized. |
| Token policy | Explicit issuer/audience/key configuration; intended lifetime/skew; secret storage, rotation, logout and revocation expectations. |
| Direct backend access | Exact allowed operations and SAS scope; prove clients cannot bypass invariants that must remain server-owned. |
| Persistence | Key/schema compatibility, ETag/precondition behavior, conflict handling and reconciliation after an uncertain write. |
| Delivery | Stable operation identity, producer permissions, settlement, retry/dead-letter policy and database/queue/provider partial-failure recovery. |
| Operations | Redacted logs, useful correlation IDs, metrics/alerts, least-privilege deployment and tested rollback/rollout dependencies. |

A test using a fake repository, broker or authenticator proves only the behavior of the code exercised by that fake. It does not establish actual SDK credential isolation, Azure scope enforcement or Functions settlement.

## Source observations and follow-up work

The ordering below is a suggested review order for affected features, not an externally verified severity score or a claim of active exploitation.

### Authentication and grant boundaries

| Observation | Required follow-up / regression cases |
| --- | --- |
| [BearerTokenMiddleware](../Niobium.Platform.Identity/BearerTokenMiddleware.cs) continues when no principal is parsed. | Require explicit authorization for protected operations. Test missing, malformed, expired, wrong-issuer and wrong-audience tokens and assert no business side effects. Keep deliberate anonymous/capability paths distinct. |
| [PrincipalParser](../Niobium.Platform.Identity/PrincipalParser.cs) disables audience validation when its audience input is blank; generic issuer/audience defaults are shared. | Validate production-critical options and application/environment separation. Test blank configuration and tokens from another consumer. Do not assume the options validator enforces all of this. |
| [BearerTokenBuilder](../Niobium.Platform.Identity/BearerTokenBuilder.cs) adds each role with the same dictionary key. | Reproduce exchange with the default role plus a stored role, then fix multi-role representation and test round-trip role authorization. Do not drop legitimate roles merely to avoid the exception. |
| [SignatureService](../Niobium.Platform.Identity/SignatureService.cs) requests one-day resource grants without clamping to access-token expiry; first successful control wins. | Decide lifetime/revocation policy and test expired application token versus already issued SAS. Test overlapping controls so a permissive fallback cannot bypass a stricter policy. |
| [DefaultTableControl](../Niobium.Platform.StorageTable/DefaultTableControl.cs) uses prefix eligibility, ignores the requested row for bounds and does not compare the claim host to the signing account. | Test near-prefix partitions, another account with the same table name, null partition, explicit row and tenant/user key formats. Require a custom narrow policy where the default is too broad. |
| [DefaultBlobControl](../Niobium.Platform.Blob/DefaultBlobControl.cs) and [Blob issuer](../Niobium.Platform.Blob/AzureBlobSignatureIssuer.cs) grant a container, not one filename/virtual directory. | Test access to another blob in the same container. Do not expose a shared private container while describing the grant as per-file. |
| [OwnershipControl](../Niobium.Platform.StorageTable/OwnershipControl.cs) checks an ownership row derived from Sid and the requested resource partition, without a separate tenant check. | Verify the complete key model, especially a user in multiple tenants. Test unrelated/missing ownership records and user/tenant switching. |

See [security and tokens](security-and-tokens.md) and [resource access](resource-access.md) for the complete decision paths.

### State, caches and composition

| Observation | Required follow-up / regression cases |
| --- | --- |
| [DefaultAuthenticator](../Niobium.Channel.Identity/DefaultAuthenticator.cs) has static saved tokens and a shared signature dictionary; clearing paths depend on saved-token comparisons. | Test two principals/tenants, remembered and non-remembered login, logout, reload and account switch in one process. Verify ID token, access token and resource signatures are independently cleared/isolated. Do not use this state model unchanged in a multi-user server. |
| [Blazor authenticator](../Niobium.Channel.Identity.Blazor/LocalStorageAuthenticator.cs) persists credentials in JavaScript-accessible storage. | Review script/content trust and shared-origin applications. Test cleanup across account switch and reload. UI authentication state does not replace server validation. |
| [Table](../Niobium.Database.StorageTable/AzureTableClientFactory.cs) and [Blob](../Niobium.File.Blob/AzureBlobClientFactory.cs) delegated SDK-client caches omit identity/expiry dimensions; [Service Bus](../Niobium.Messaging.ServiceBus/AuthenticationBasedQueueFactory.cs) sender/receiver caches use only queue name. | Test fresh SAS with an existing SDK client, identical resource names in different accounts/namespaces, two users, changed permissions and parallel calls. Define cache keys, expiry/update and disposal ownership centrally. |
| Several `DependencyModule` classes use process-static registration guards. | Construct two separate service collections/hosts in one process and resolve the expected services/middleware from both. Use process isolation until the shared lifetime issue is addressed; do not make business domains singleton as a workaround. |
| Resource entitlement convenience helpers can replace non-null supplied endpoints with fallback configuration and reject absent values. | Exercise options-only, fallback-only, both-present and mismatched endpoints. Inspect each backend's overloads rather than assuming one workaround applies to all. Sources: [Table](../Niobium.Platform.StorageTable/DependencyModule.cs), [Blob](../Niobium.Platform.Blob/DependencyModule.cs), [Service Bus](../Niobium.Platform.ServiceBus/DependencyModule.cs). |

### Data, files and messaging

| Observation | Required follow-up / regression cases |
| --- | --- |
| [GenericDomain](../Niobium.Abstractions/GenericDomain.cs) force-save upserts and dispatches events after persistence; its cached hash is not reset by Save/Reload. | Test stale writes, force-save policy and event failure after commit. Do not present callbacks as a transactional outbox or cached hashes as verified fresh versions. |
| Existing [data-access guidance](domain-data-access.md) records write-ETag mapping and domain-cache limitations. | Use real-adapter tests for read/write ETag round trips, stale updates, batches and ambiguous outcomes. Read back and prove the complete intended mutation before borrowing a version for a claim. |
| [CloudBlobRepository](../Niobium.File.Blob/CloudBlobRepository.cs) defaults several reads to container creation, buffers downloads and separates upload from tagging. | Test read-only credentials and combined permission flags, oversized content, leading-slash names, overwrite conflicts and upload-success/tag-failure. Keep tenant-path authorization outside the adapter explicit. |
| [ServiceBusMessageEntry.DisposeAsync](../Niobium.Messaging.ServiceBus/ServiceBusMessageEntry.cs) completes a manually dequeued message. | Reproduce handler failure inside an unconditional disposal scope. Make completion a deliberate success action; define failure settlement/lock handling. Test Functions bindings separately. |
| [Broker sends](../Niobium.Messaging.ServiceBus/ServiceBusQueueBroker.cs) are sequential; forwarded Authorization is optional and not validated by body parsing. | Test partial sends and lost acknowledgements. Enforce either restricted producer trust or explicit delegated-principal validation at the receiver. Do not confuse stable IDs with an implemented inbox. |
| [DevelopmentMessagingBroker](../Niobium.Messaging/DevelopmentMessagingBroker.cs) logs bodies and does not deliver. | Test environment selection and ensure secrets/recipient data are not used in verbose development logs. Separate real-broker integration tests from logging-only tests. |

## Cross-boundary test matrix

Build the following cases into an affected implementation PR. These are proposed tests, not a statement that this documentation ran them.

| Scenario | Assertions |
| --- | --- |
| Protected HTTP command with mismatched tenant/biller | Reject before any repository write, enqueue or provider call; assert the endpoint's chosen status contract. |
| Resource grant from wrong account or adjacent partition prefix | No unintended grant; the effective SAS bounds and operations match the documented policy. |
| Valid principal but unauthorized direct Table write | Either the backend rejects it or the architecture explicitly permits that operation without domain mediation. |
| Logout/account switch with cached SDK clients | No reuse of another principal's authority; expired tokens are not silently kept by cached clients. |
| Same queue name in two namespaces | Each configured producer/consumer reaches the intended namespace, including concurrent resolution. |
| Same operation ID with changed payload | Explicit conflict or documented version policy, not an unnoticed duplicate side effect. |
| Database commit followed by enqueue failure | Recover without losing the durable business intent or blindly repeating an irreversible action. |
| Provider success with lost response | Reconcile through stable identity and bounded idempotency policy, not an unconditional new attempt. |
| Scheduled recovery with broker duplicate detection | The recovery wake actually arrives under deployed settings; retention/window assumptions are documented. |
| Read-only file consumer | No accidental container creation or broader SAS needed simply to read an authorized blob. |
| Capability-link access | Wrong/missing token denied; stored-token reuse, secret rotation and any expiry/revocation rules tested explicitly. |

The Notification durable path, Invoicing HTTP/private-command/link paths and Store public order path illustrate different cases in this matrix. See [consumer patterns](consumer-patterns.md); do not apply one trust model to all of them.

## Operational checks not established by this source review

Verify backend data-plane RBAC, queue rule rights, network ingress/egress and actual deployed application settings. Review rate limits, abuse controls, TLS, CORS and non-cacheable token responses at the host/edge. The [token response helper](../Niobium.Platform.Identity/HttpRequestExtensions.cs) exposes response headers but does not itself establish all those policies.

[Platform forwarded-header registration](../Niobium.Platform/DependencyModule.cs) clears known proxy/network collections. Establish which ingress can set forwarded headers before using a remote address as an audit/risk signal. Do not assume this exact pipeline runs in every Functions host.

Log non-sensitive correlation identifiers, outcome categories and timing rather than access/ID tokens, SAS URLs, private message bodies or invoice-link tokens. Review [error payloads/logging](../Niobium.Platform/ErrorHandlingMiddleware.cs), development broker logs, provider errors and query-string telemetry together. Do not put production credentials or user data into test fixtures or PR evidence.

For a follow-up PR, record the baseline package/source, minimal synthetic reproduction, failing test, fix location, compatibility and migration implications, tests actually run, remaining limitations and required consumer release. Keep package publication and deployment separately approved; [package upgrades](package-upgrades.md) explains why a source merge does not update already installed consumers.
