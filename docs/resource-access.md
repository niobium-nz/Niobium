# Resource authorization and backend credentials

Read [security and tokens](security-and-tokens.md) first. This guide documents the [review baseline](README.md#evidence-baseline), including behavior that needs hardening. It does not change grants or infrastructure permissions.

## Three separate decisions

A resource operation involves three different decisions:

1. **Application authorization:** may this principal perform this business action on this tenant/record?
2. **Grant issuance:** may this principal receive a direct-backend credential for this resource and scope?
3. **Backend authorization:** does the server identity or issued credential have the backend rights needed for this operation?

`IRepository<TEntity>` and `IFileService` do not automatically implement the first decision when operating with a server identity. Azure RBAC for a service does not establish end-user ownership. Keep business authorization at the entry/domain boundary and credential translation in adapters.

## Server mode versus resource-token client mode

| Adapter | Configured server endpoint/namespace | No configured server endpoint/namespace |
| --- | --- | --- |
| [Table factory](../Niobium.Database.StorageTable/AzureTableClientFactory.cs) | Uses `DefaultAzureCredential`; reads the configured endpoint or conventional configuration fallback. | Selects an access-token resource entitlement and asks `IAuthenticator` for a Table SAS. |
| [Blob factory](../Niobium.File.Blob/AzureBlobClientFactory.cs) | Uses `DefaultAzureCredential` for the Blob service. | Selects a matching container entitlement and requests a Blob SAS. |
| [Service Bus factory](../Niobium.Messaging.ServiceBus/AuthenticationBasedQueueFactory.cs) | Uses `DefaultAzureCredential` for the namespace. | Selects a queue entitlement and constructs an `AzureSasCredential` from a resource token. |

Server-mode Table and Blob factories do **not** use the `Key` property to authenticate ordinary repository/file calls in the inspected source. Those keys are used by the platform SAS issuers. A deployment can therefore need managed-identity data-plane rights for server work and a protected account key for delegated SAS issuance. These are separate prerequisites, not interchangeable settings.

Do not set a server endpoint in an untrusted client and assume resource-token checks still run: endpoint configuration selects the server credential path. Never ship account keys, signing secrets or private keys to a browser. Development options allow interactive credentials. Verify the selected identity and least-privilege data-plane permissions in the actual deployment; registration code alone does not prove RBAC is correct.

## Entitlement representation

[AccessTokenMiddleware](../Niobium.Platform.Identity/AccessTokenMiddleware.cs) creates custom resource claims with this logical shape:

```text
Claim type: NB-<integer ResourceType>://<host-or-namespace>/<table-container-or-queue>/<optional-scope>
Claim value: <comma-separated permissions>
```

[ToResourcePermissions](../Niobium.Abstractions/PermissionExtensions.cs) parses the first path component into `Resource`, the second into `Partition`, and the third into `Scope`. These names describe the entitlement representation, not the backend's entire key schema. For Table claims, `Partition` is the table name and `Scope` is the permitted application partition prefix.

Use the existing descriptor APIs rather than constructing strings in each consumer. Keep host, resource name, GUID/key formatting, permission casing and separators consistent. The parser does not provide a general URI-policy or key-normalization layer. Do not assume an arbitrary nested path or differently formatted user ID is equivalent.

## Resource-control extension points

Register the appropriate resource-token support alongside identity and the backend adapter:

| Backend | Platform registration | Issuer / default control |
| --- | --- | --- |
| Table | [`AddDatabaseResourceTokenSupport`](../Niobium.Platform.StorageTable/DependencyModule.cs) | `AzureTableSignatureIssuer` / `DefaultTableControl` |
| Blob | [`AddBlobResourceTokenSupport`](../Niobium.Platform.Blob/DependencyModule.cs) | `AzureBlobSignatureIssuer` / `DefaultBlobControl` |
| Service Bus | [`AddServiceBusResourceTokenSupport`](../Niobium.Platform.ServiceBus/DependencyModule.cs) | `ServiceBusSignatureIssuer` / `DefaultServiceBusQueueControl` |

A custom `IResourceControl` answers whether it can handle the type/resource, then returns an authorized `StorageControl` or no grant. `ISignatureIssuer` converts that grant into a backend signature. [SignatureService](../Niobium.Platform.Identity/SignatureService.cs) uses the first successful control and a single matching issuer; it does not intersect every control's permissions.

Adding a stricter control does not automatically narrow an existing permissive one. Design registrations so an unintended fallback cannot grant the request, and test overlapping controls. Grant only the operations and bounds you intend to expose. A row ID sent by a caller has no effect unless the control and issuer actually use it.

## Table grants

[RoleBasedEntitlementDescriptor](../Niobium.Platform.StorageTable/RoleBasedEntitlementDescriptor.cs) produces account/table grants. [PersonalizedEntitlementDescriptor](../Niobium.Platform.StorageTable/PersonalizedEntitlementDescriptor.cs) appends the user GUID as the scope. The personalized descriptor does not append the tenant GUID; applications needing tenant-and-user isolation must design that scope explicitly.

[DefaultTableControl](../Niobium.Platform.StorageTable/DefaultTableControl.cs) matches Table entitlements by table name and uses `partition.StartsWith(scope)` for scoped grants. It returns equal start/end partition keys for the requested partition. It does not use the requested row argument to set row bounds. An unscoped grant with a null requested partition can issue a table-wide grant.

Important distinctions:

- Prefix eligibility is not exact user-partition equality. A derived partition beginning with the scope can be eligible; the resulting SAS is bounded to that requested partition.
- The control does not compare the claim's account/host `Resource` with the configured signing account. Validate account containment when supporting multiple accounts or custom descriptors.
- Default grants are not row-only grants, even when the request contains `id`.
- A read/write grant lets the client call the backend directly, bypassing application domain methods. Do not grant writes to records whose invariants require server mediation unless equivalent constraints are deliberately enforced elsewhere.

[AzureTableSignatureIssuer](../Niobium.Platform.StorageTable/AzureTableSignatureIssuer.cs) maps Query/Add/Update/Delete to Table SAS permissions and copies partition and row bounds from `StorageControl`. It signs with the configured account key. It does not independently discover ownership or narrow an overbroad control result.

### Ownership-based child data

[OwnershipControl<TResource, TOwnership>](../Niobium.Platform.StorageTable/OwnershipControl.cs) requires a requested partition, derives the owner from `Sid`, checks that an ownership record exists at `(owner, requestedPartition)`, and grants Query over that resource partition. It does not add a separate tenant claim check.

Invoicing registers `OwnershipControl<InvoiceItem, Invoice>` so the existence of the caller's invoice record authorizes querying its item partition. That depends on the invoice and item key design. Do not reuse it for unrelated key layouts without testing the ownership relationship and tenant model.

### Actual Invoicing grants

The inspected [Invoicing composition](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/DependencyModule.cs) grants the default role:

| Table | Personalized permissions |
| --- | --- |
| `Invoice` | Query |
| `Billable` | Query, Add, Delete |
| `Billee` | Query, Add, Delete, Update |
| `InvoiceItem` | Query via the ownership control, not the same personalized descriptor |

These are application choices, not a recommended universal grant set. Review whether each direct write is compatible with domain invariants. Server-side repository operations still rely on the server's credential and explicit business checks.

## Blob grants

[DefaultBlobControl](../Niobium.Platform.Blob/DefaultBlobControl.cs) matches the configured host and entitlement container prefix. It returns equal start/end partition values representing one container. It does not use the row argument as a blob name.

[AzureBlobSignatureIssuer](../Niobium.Platform.Blob/AzureBlobSignatureIssuer.cs) signs a **container** SAS using that value. Write also maps to Tag, and Delete also maps to DeleteBlobVersion. Read, Add, List and Create are mapped independently. Do not describe this API as issuing per-file or per-virtual-folder access.

A path such as `tenant/template.html` within a container is an application naming convention, not an access-control boundary enforced by this container SAS. For narrower exposure, design a separate authorization/serving path or a deliberately scoped issuer with tests; do not imply that capability already exists. See [files and Blob storage](files-and-blob-storage.md).

## Service Bus grants

[DefaultServiceBusQueueControl](../Niobium.Platform.ServiceBus/DefaultServiceBusQueueControl.cs) matches the namespace and exact/prefix queue entitlement, then returns a grant for the requested queue. [ServiceBusSignatureIssuer](../Niobium.Platform.ServiceBus/ServiceBusSignatureIssuer.cs) expects an entry in `ServiceBusOptions.Keys` named `<queue>-<numeric permission mask>` and signs a token for the namespace/queue.

The provisioned authorization rule and its rights must agree with that key name and intended permissions. The code does not create the rule or independently prove the associated key has only those rights. Do not use a broad namespace-management key as a substitute for a deliberately scoped producer/consumer grant.

This delegated SAS path exists in source; the inspected Store, Notification and Invoicing server compositions demonstrate configured-namespace messaging, not a validated browser-to-queue grant deployment.

## Configuration and cache limitations

The Table default entitlement convenience helper has a notable source-level behavior: when `FullyQualifiedDomainName` is non-null, it reads/parses the conventional configuration URI and replaces the value; when it is null, it ultimately throws. Do not assume either an options-only or fallback-only configuration works for that helper. Test both inputs and consider the explicit Table descriptor overload while fixing shared behavior centrally. [Source](../Niobium.Platform.StorageTable/DependencyModule.cs).

Blob and Service Bus descriptor-registration helpers have a similar non-null branch that reads the fallback configuration. Their string overloads also pass through that helper. Do not assume the explicit-overload workaround for Table applies to those backends. Test actual registration and resolution with the intended settings. [Blob source](../Niobium.Platform.Blob/DependencyModule.cs), [Service Bus source](../Niobium.Platform.ServiceBus/DependencyModule.cs).

SDK-client caches are static and incompletely keyed for identity-sensitive use: Table resource clients use table/partition, Blob resource clients use container, and Service Bus senders/receivers use queue name without namespace. Those keys do not encode every principal, host, permission or expiry dimension. An authenticator obtaining a fresh SAS does not prove an already cached SDK client has refreshed it.

Before using delegated access in a shared process, test expiry, logout, account switch, cross-tenant access and identical names in different accounts/namespaces. See the [review checklist](security-review-checklist.md). These findings are documented limitations, not evidence that a live deployment was tested.
