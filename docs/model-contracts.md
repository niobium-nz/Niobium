# Business names and model-owned data contracts

The owner established this rule on 14 November 2026 for every Niobium project.
Use business names on Table models, and keep the representation of a model's
identity and stored data inside that model. Rich domains continue to own business
rules. This guide is the shared policy; project guides record their concrete
contracts and any compatibility work still required.

## Name the business value, then map the storage role

Use names such as `Tenant`, `ID`, `Account` or `Period` according to what the value
actually represents. Do not name a business-model property `PartitionKey` or
`RowKey` merely because Azure Table Storage uses that column name. A composite
key needs a name for the whole value; do not call a tenant/user pair just `Tenant`.

The attribute type is [EntityKeyAttribute](../Niobium.Abstractions/EntityKeyAttribute.cs).
Its normal C# spelling is `[EntityKey(EntityKeyKind.PartitionKey)]` or
`[EntityKey(EntityKeyKind.RowKey)]`. The mapping works in both directions:

| Model property | Annotation | Physical Table column |
| --- | --- | --- |
| `NotificationSubmission.Tenant` | `EntityKeyKind.PartitionKey` | `PartitionKey` |
| `NotificationSubmission.ID` | `EntityKeyKind.RowKey` | `RowKey` |
| `ETag` | `EntityKeyKind.ETag` | ETag concurrency metadata |
| `Timestamp` | `EntityKeyKind.Timestamp` | Timestamp metadata |

An illustrative shape is:

```csharp
public sealed record NotificationSubmission
{
    [EntityKey(EntityKeyKind.PartitionKey)]
    public string Tenant { get; private init; } = String.Empty;

    [EntityKey(EntityKeyKind.RowKey)]
    public string ID { get; private init; } = String.Empty;

    [EntityKey(EntityKeyKind.ETag)]
    public string? ETag { get; init; }

    [EntityKey(EntityKeyKind.Timestamp)]
    public DateTimeOffset? Timestamp { get; init; }

    // A meaningful factory supplies the identity; callers do not compute a hash.
    // Other persisted fields and the factory are omitted from this shape example.
}
```

[EntityMappingHelper.GetMapping](../Niobium.Abstractions/EntityMappingHelper.cs)
substitutes the annotated storage role for the property name. The Table adapter's
[DBEntityHelper](../Niobium.Database.StorageTable/DBEntityHelper.cs) reads and writes
those mapped names. `IRepository<NotificationSubmission>` therefore stores the
values in `PartitionKey` and `RowKey`; it does not add `Tenant` and `ID` columns.
Application code needs neither an Azure entity DTO nor a second mapper.

The mapper enumerates public instance properties, including computed properties.
`JsonIgnore` and `JsonPropertyName` govern JSON, not Table mapping. Do not retain
old public key aliases alongside new annotated properties: aliases can collide
with mapped names or become unwanted columns. A derived value that is not a
column should normally be a method or a separate nonpersisted contract.

Keep the parameterless construction and setters needed by the actual adapter.
The inspected Table implementation constructs the model and calls
`PropertyInfo.SetValue`; a public getter with a private init setter can retain
model-controlled construction while permitting materialization. This is not a
guarantee for every serializer/backend. Test reads, writes and ETags against the
consumer's installed package before restricting accessors. ETags are strings;
Timestamp properties are `DateTimeOffset` or nullable `DateTimeOffset`.

`StorageKey`, `GenericDomain`'s key-based initialization, and adapter types such as
`EntityDictionary : ITableEntity` describe storage infrastructure. Their technical
key names are appropriate at that boundary. They are not examples for naming a
business Table model. Do not rename framework interfaces or SDK-required members
as a mechanical response to this rule.

## A model owns its representation

The owning data contract defines identity derivation, canonical fingerprints,
composite-key encoding, container/path conventions and its serialized shape.
Keep implementation helpers private. Expose the smallest meaningful factory,
comparison or value-producing operation that callers need.

The companion Notification implementation at
[revision `12fd243`](https://github.com/niobium-nz/Niobium.Notification/tree/12fd2438f46bf701111e81c374ba8f55a33542d9/src/Niobium.Notification.Core/Delivery)
illustrates the intended boundary. This revision is pending review when this
guide is written; it is not the older inspected consumer baseline or evidence of
a production deployment. The example and representation APIs described below
refer to that exact companion revision:

```csharp
NotificationSubmission request = NotificationSubmission.Create(command);
NotificationSubmissionDomain domain = await submissions.GetAsync(
    request.Tenant, request.ID, forceLoad: true, cancellationToken: token);

await domain.RegisterAsync(request, frozenPayload, token);
SubmissionAttempt attempt = await domain.PrepareAttemptAsync(
    request, clock.GetUtcNow(), retryPolicy, token);
```

The caller knows that a submission has a tenant and identity. It does not know
that the current ID is uppercase SHA-256 over UTF-8 command identity, and it does
not call a public hash utility to reproduce that ID. `Create` captures the request
fingerprint and a private payload reference. Registration still decides whether
the request may create or reuse a durable operation.

The model also owns `GetProviderIdempotencyKey()` and payload-name construction.
The domain uses `NotificationSubmission.PayloadContainer` and the stored
`PayloadName` with `IFileService`; it must not interpolate a path from domain keys.
`FrozenNotification` owns its JSON representation and credential-fingerprint
comparison. A provider supplies its credential to that comparison without knowing
the digest algorithm, and the contract retains the fingerprint, not the credential.

Keep SDK calls, repository access, service resolution, clocks controlling business
policy and external effects out of these representation helpers. Serialization of
an already supplied stream is a data-format operation; opening cloud storage or
deciding whether a send is allowed is not.

## Keep business rules on rich domains

| Responsibility | Owner |
| --- | --- |
| Key names/attributes, ID encoding, canonical request representation, blob references and wire format | Model/data contract |
| Registration validity, ownership, allowed transitions, claim eligibility, retry window, suppression and outcome decisions | Domain |
| Conditional persistence and ordering a blob write before a row references it | Domain using repository/file abstractions |
| Coordinating rendering, scheduling a recovery wake and invoking a provider | Flow |
| Azure column conversion, SDK clients, ETags and backend operations | Framework adapter |

Structural validation needed to construct a usable identity may live in a model
factory. Business permission, retry eligibility or lifecycle state must remain in
the domain. A model factory does not persist, claim ownership or authorize a send.
Conversely, a rich domain is not a reason to make it responsible for hash
algorithms, path layouts or JSON field names. See [domain behavior and persistence](domain-data-access.md).

## Compatibility is part of the contract

Renaming a C# key property with the same annotation and the same encoded value can
preserve the physical schema. It does not automatically preserve JSON, public APIs,
compiled consumers or storage identity. For Notification, the tenant remains the
existing GUID `N` text. Changing the property to `Guid` without compensating for
the mapper's default conversion would produce a different key representation.

Private ownership localizes a future change; it does not make that change safe to
deploy without a compatibility plan. Keep existing key bytes, case, canonical
fingerprints, provider idempotency keys, blob names, JSON names and schema versions
unless a separate rollout explicitly changes them. A different ID algorithm can
make an existing operation look new and permit a duplicate external effect.

For a representation refactor, require fixed vectors from the old implementation,
raw legacy Table rows and Blob content, read/write round trips through the pinned
adapter, and restart/concurrency checks. Verify that renamed properties materialize
correctly and do not create extra columns. Preserve the adapter's actual ETag and
failure behavior rather than assuming the refactor fixes it.

## Legacy contracts awaiting a separate rollout

The owner explicitly selected Notification implementation plus documentation now;
the following existing contracts were inventoried, not renamed in this delivery.
Their generic names are legacy compatibility debt, not approved examples for new
models. Review their consumers and serialization before choosing the final names.

| Repository / model | Existing key meaning and follow-up |
| --- | --- |
| Niobium.Finance `Accounting` | Principal and reverse-time accounting-period end; preserve key formatting and consumers |
| Niobium.Finance `Transaction` | Owner and reverse-time creation identity; preserve ordering and stored IDs |
| Niobium.Finance `PaymentMethod` | User and payment-method identity; inspect published API and payment consumers |
| Niobium.Platform.Identity `Profile` | Composite tenant/user partition and profile row; inspect profile serializers and consumers |
| Niobium.Platform.Notification `OpenID` | User and kind/application/identifier encoding; retain existing lookup ranges |
| Niobium.Platform `Cache` | Cache grouping and item identity; inventory each caller's semantics before naming |
| Niobium.Identity `Login` | Authentication-kind/application scope and trimmed login identity; preserve lookup and authentication behavior |
| Niobium.Store `ShippingOption` | Existing singleton catalogue partition (`dummy`) and `ID`; do not reinterpret the partition as a tenant |
| Niobium.Invoicing `Biller` | External management-group and biller/user profile fields; preserve the upstream wire contract |

Niobium.DevOps currently supplies a reusable build workflow and no Table model.
Its documentation and future examples/generators must use the same naming and
ownership rules. Apply this policy to new models and touched contracts in every
project; migrate listed legacy APIs through an explicitly scoped consumer rollout.

The additional Niobium.AI (`386e506`), Niobium.IoT (`fdd9e6c`) and
Niobium.AI.Ecommerce (`9e9503f`) repositories contained no Table model in the
inspected source. Their local guides apply this policy to future models and
examples while preserving existing device/vendor protocols.

Inventory baselines: framework `887c891`, Notification `a8bc0e4`, Invoicing
`41deb0b`, Store `a196fa2`, Identity `0a3f8aa`, and DevOps `deeaa70`.
