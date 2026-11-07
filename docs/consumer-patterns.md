# Consumer patterns: Store, Notification and Invoicing

The examples below are pinned to the [reviewed commits](README.md#evidence-baseline). They show how applications compose Niobium, not a certification that every path is production-hardened. Consumer package references differ from current framework source; verify the resolved package graph before copying an API or relying on an implementation detail.

## What is shared and what is application policy

All three applications demonstrate separation between contracts, Core domains/flows and a Functions host. They use framework repositories and typed messaging rather than putting every backend operation in an HTTP trigger. Their authority models and delivery guarantees are not identical.

| Concern | Store | Notification | Invoicing |
| --- | --- | --- | --- |
| Representative HTTP entry | Public order request with validation and CAPTCHA | Public contact/subscription paths with their own input/abuse policies | Issuance explicitly checks user/tenant claims; HTML viewing uses a saved link token |
| Business work | Quote, order transitions, payment coordination, downstream command construction | Template/subscription work and delivery; optional durable submission state machine | Invoice preparation, frozen content, rendering and notification coordination |
| Persistence | Framework domains/repositories over Tables | Tables for metadata/state; Blob file service for template content | Tables with ownership-dependent data access and conditional domain transitions |
| Messaging | Typed invoice/notification destination overrides | `NotifyCommand` consumption; scheduled recovery when durable mode is enabled | Private `IssueInvoiceCommand` consumption and notification production |
| Identity/resource example | Inspected Server module does not install the Niobium identity pipeline | Inspected Server module does not install the Niobium identity pipeline | Identity middleware, personalized Table entitlements and child-record ownership control |

Composition sources: [Store](https://github.com/niobium-nz/Niobium.Store/blob/db600f117002509c932d8d60321f9892f8cb7263/src/Niobium.Store.Server/DependencyModule.cs), [Notification](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Server/DependencyModule.cs), [Invoicing](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/DependencyModule.cs). Absence of identity registration here is not proof that an environment has no external gateway or infrastructure policy; those were not audited.

## Store: public ingress, domain transitions, private downstream commands

[MakeOrder](https://github.com/niobium-nz/Niobium.Store/blob/db600f117002509c932d8d60321f9892f8cb7263/src/Niobium.Store.Server/MakeOrder.cs) is an anonymous HTTP trigger. It validates an `OrderRequest`, runs the visitor-risk assessor, and invokes [OrderFlow](https://github.com/niobium-nz/Niobium.Store/blob/db600f117002509c932d8d60321f9892f8cb7263/src/Niobium.Store.Core/Flows/OrderFlow.cs). That flow obtains a quote, asks `OrderDomain` to create the order and construct the charge, then calls the payment service.

The useful pattern is responsibility placement: entry validation and abuse assessment, flow coordination, and domain-owned persistence. CAPTCHA is not a user principal. A body `Tenant` or customer ID is not established as authenticated ownership merely because the risk assessment passed. A new protected operation needs its own authority checks rather than a copied CAPTCHA-only endpoint.

[OrderDomain](https://github.com/niobium-nz/Niobium.Store/blob/db600f117002509c932d8d60321f9892f8cb7263/src/Niobium.Store.Core/Domains/OrderDomain.cs) builds notification commands with stable IDs based on the order and notification stage. Its invoice command distinguishes the order's tenant/biller from the configured invoicing tenant and embeds the billee snapshot. This is why a receiving service must understand the producer's authority to name those identities; the fields are not interchangeable.

The existing force-save settlement/tracking paths and create-conflict handling are observations, not templates for a new concurrency or payment-idempotency protocol. A create conflict must not be answered using an unverified cached candidate. A retried payment event needs an explicit duplicate policy. See [rich domains](domain-data-access.md) and [messaging](messaging.md).

Store's host registers separate broker options for invoice and notification contracts. Preserve that separation when namespaces differ, while testing the framework's same-queue-name cache limitation. Do not assume creating a typed broker provisions a queue or grants its sender rights.

## Notification: file-backed rendering and explicit attempt ownership

[TemplateDomain](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Core/TemplateDomain.cs) owns template lookup and rendering. It obtains content through `IFileService`, uses a tenant-derived filename and HTML-encodes substituted values. This is a server-read pattern, not a demonstration of per-file client SAS. See [file access](files-and-blob-storage.md) for container scope, path conventions and stream ownership.

[NotifyCommandConsumer](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Server/NotifyCommandConsumer.cs) parses the direct JSON body, normalizes parameter values and validates the command. Parse/validation failures log and return; this differs from Invoicing's throwing consumer and must be assessed against the desired settlement policy.

With `EnableDurableSubmission` enabled, the consumer requires a nonblank envelope MessageId and copies it into the command before calling [DurableNotificationFlow](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Core/Delivery/DurableNotificationFlow.cs). The flow uses a submission domain, request fingerprint, frozen provider payload, stable idempotency key and scheduled recovery before provider submission. Otherwise the consumer calls the legacy flow.

This is a useful model for durable work: the domain owns state transitions and claims; the flow orders transport/provider calls; a provider boundary owns its wire protocol. It is still conditional on infrastructure, provider retention, correct ETags and configuration. Do not generalize it into a guarantee that every `IMessagingBroker` operation is exactly-once.

Notification also gives its durable provider client explicit redirect and retry policy. The two policies are separate; see [HTTP client configuration](http-clients.md). Upgrading a framework package does not by itself enable durable submission or validate deployed queue settings.

## Invoicing: three different access boundaries

### Authenticated HTTP issuance

[NewInvoice](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/NewInvoice.cs) reads `Sid` and `GroupSid` from the request principal. Missing claims produce 401; a biller or tenant mismatch produces 403. It validates the request and invoice identifier, disables notification for this HTTP path, then invokes the flow.

The reusable convention is explicit principal-to-command binding before business work. `AuthorizationLevel.Anonymous` at the Functions binding does not explain the whole policy. Conversely, installing bearer middleware does not replace these entry checks. The invoice-age check is one-sided; do not copy it as a symmetric past/future timestamp policy.

### Trusted private command

[IssueInvoiceCommandConsumer](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/IssueInvoiceCommandConsumer.cs) consumes `issueinvoicecommand`, parses and validates it, and supplies the embedded billee to the issuance flow. It does not validate an end-user principal. The documented trust boundary is a restricted private producer set, not arbitrary clients holding a token-shaped string.

Keep that queue private, review every producer's ability to name a tenant/biller, and test malformed, duplicate and conflicting requests. An HTTP authorization check in another trigger does not protect this queue path automatically.

### Possession-based HTML access

[GetHtmlInvoice](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/GetHtmlInvoice.cs) deliberately permits a recipient without an account to use an invoice link. [InvoiceDomain](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Core/Domains/InvoiceDomain.cs) requires the saved token, persists a new token before notification enqueue, and reuses an existing token for resends.

This capability is not a Niobium access JWT or Azure resource token. The inspected path does not enforce an expiry policy, and secret rotation does not invalidate already saved links. Treat the link as sensitive and define its sharing/revocation policy. A delivery timestamp records notification enqueue completion, not confirmation of recipient delivery.

### Direct Table access and service identity

Invoicing grants personalized access to Invoice, Billable and Billee and uses `OwnershipControl<InvoiceItem, Invoice>` for child-item queries. These are separate from the issuance endpoint checks. A client with a direct Table write grant can bypass domain methods; audit the chosen operations accordingly. See the exact [grant matrix](resource-access.md#actual-invoicing-grants).

Its profile client uses service-principal authentication. That outbound Entra credential is different from the incoming Niobium principal and from a Table SAS. Do not configure all three as though they were one token system.

## Applying the patterns to a new feature

Start with the authority model and storage/wire contract, not a copied startup file. Define whether the caller is anonymous, a delegated user, a trusted producer or a capability holder. Identify which component makes the authorization decision and what identity scope reaches the domain.

Then place invariants and conditional mutations in a domain, let a flow coordinate side effects, and use adapters for data/files/messages. Record partial-failure and recovery behavior at every database-to-queue or provider boundary. Choose stable operation IDs, explicit overwrite/concurrency policy and testable negative cases.

Finally, register the required modules and middleware, provision backend resources/least-privilege identities, and verify the actual published packages. The [security checklist](security-review-checklist.md) turns the current framework caveats into concrete tests; it is not permission to silently change application contracts or package versions in a documentation PR.
