# Messaging, brokers and delivery semantics

Use this guide with the [review baseline](README.md#evidence-baseline), [rich-domain guidance](domain-data-access.md) and [security model](security-and-tokens.md). Niobium provides typed transport and event adapters. It does not make every application workflow transactional, authorized or exactly-once.

## Responsibilities and registration

A command requests work; an event describes a fact. Keep their stable wire contracts in the consumer's Contracts layer, business transitions in domains, orchestration in flows, and Azure bindings/transport in the host and adapter. Names such as `IssueInvoiceCommand`, `NotifyCommand` and `OrderCreatedEvent` reflect this convention; the broker does not enforce semantic correctness from the suffix.

[Platform AddMessaging](../Niobium.Platform.ServiceBus/DependencyModule.cs) composes the framework messaging services and Service Bus adapter. The [adapter module](../Niobium.Messaging.ServiceBus/DependencyModule.cs) registers `IMessagingBroker<T>` for classes implementing `IDomainEvent`. `AddMessagingBroker<T>` allows a particular contract to use its own `ServiceBusOptions`, as Store does for invoice and notification destinations.

The per-type registration constructs a fresh options object. Do not assume an omitted per-type setting automatically inherits the global options instance. Resolve and test the actual broker with its intended destination and transport settings.

**Development mode can mean no delivery.** The platform host overload passes development detection into `testMode`. [DevelopmentMessagingBroker](../Niobium.Messaging/DevelopmentMessagingBroker.cs) logs enqueued messages and returns empty dequeue/peek results; it is not a durable in-memory queue. Its full serialized-message logging also deserves a sensitive-data review. Verify that a test exercises the real transport before treating successful enqueue as evidence of Service Bus delivery.

## Queue names and message shape

[ServiceBusQueueBroker<T>](../Niobium.Messaging.ServiceBus/ServiceBusQueueBroker.cs) derives a queue name from the lowercase CLR type name. Generic types use the lowercase name without the backtick, followed by lowercase argument names separated by hyphens. A per-type options object changes the namespace, not this naming rule.

Renaming a contract type can change its queue. Treat that as an infrastructure and compatibility migration. The inspected consumers bind to names such as `notifycommand` and `issueinvoicecommand`; provision queues and sender/receiver permissions consistently.

The outbound envelope has:

| Field | Current behavior |
| --- | --- |
| Body | `MessagingEntry.Body`, carrying the serialized command/event rather than a JSON wrapper containing the whole entry |
| `ContentType` | Exactly `application/json` |
| `MessageId` | `MessagingEntry<T>.ID`, initialized to a new GUID string when the entry is constructed; the broker generates another GUID only if that entry ID is blank at send time |
| Application `Content-Type` property | Optional entry type metadata, distinct from the Service Bus content type |
| Application `Authorization` property | An available authenticator access token as Bearer metadata; absence is allowed |
| `ScheduledEnqueueTime` | The entry's optional `Schedule` value |

Enqueue of an enumerable sends messages one by one. It is not an atomic send batch. Earlier messages can be accepted before a later send fails or the caller loses the acknowledgement.

### Business IDs, body IDs and envelope IDs

A **business-operation ID** identifies the logical work and must be persisted or reconstructible across retries. The **body ID**, such as `command.ID`, is a field in the serialized command. The **envelope MessageId** is the transport identity taken from `MessagingEntry<T>.ID`. These values can deliberately agree, but the framework does not automatically synchronize them.

[MessagingEntry<T>](../Niobium.Messaging/MessagingEntry.cs) initializes its ID independently. Setting `Value = command` serializes the command but does not copy `command.ID` to the entry ID. Reconstructing `new MessagingEntry<NotifyCommand> { Value = command }` on each attempt therefore creates different envelope IDs even when the command's body ID is unchanged.

For a producer whose stable command ID is also the intended transport identity, assign both explicitly:

```csharp
using Niobium.Messaging;
using Niobium.Notification;

// command is an already validated NotifyCommand; broker is IMessagingBroker<NotifyCommand>.
// Persist or reconstruct command.ID for the same logical operation across attempts.
ArgumentException.ThrowIfNullOrWhiteSpace(command.ID);
MessagingEntry<NotifyCommand> entry = new()
{
    ID = command.ID,
    Value = command
};
await broker.EnqueueAsync(entry, cancellationToken);
```

This fragment uses the [single-entry enqueue extension](../Niobium.Messaging/IMessagingBrokerExtensions.cs). It does not generate an ID on retry or establish a complete producer/outbox implementation. When one operation emits multiple distinct messages, define a stable identity for each message rather than reusing one operation ID indiscriminately.

[Service Bus duplicate detection](https://learn.microsoft.com/en-us/azure/service-bus-messaging/duplicate-detection) uses the envelope `MessageId` rather than an ID inside the JSON body; partitioning also affects its uniqueness rule. Setting a stable envelope ID does not enable duplicate detection, define its retention window or give the consumer an inbox/idempotency policy. The scheduled-recovery caveat below still applies.

## The two parsing APIs are different

Two similarly named extensions have different contracts:

| Extension | Behavior |
| --- | --- |
| [`Niobium.Platform.ServiceBus`: `TryParse(out T, out rawbody)`](../Niobium.Platform.ServiceBus/ServiceBusReceivedMessageExtensions.cs) | Requires exact `application/json`, reads direct JSON and returns the request. It does not validate a principal or copy the envelope ID into the command. Its deserialization call is not enclosed by a catch in this helper. |
| [`Niobium.Messaging.ServiceBus`: `TryParse(out MessagingEntry<T>)`](../Niobium.Messaging.ServiceBus/ServiceBusReceivedMessageExtensions.cs) | Reads optional CLR type metadata, parses an entry, copies envelope ID/time into the entry and its value, and catches parsing failures to return false. |

Select the overload deliberately and test the actual serialization path. A method named `TryParse` is not a promise that all malformed inputs become false in every overload. Content type matching is exact; `application/json; charset=utf-8` is not accepted by these checks.

Do not treat optional type metadata or deserialized tenant/user fields as proof of authority. Keep producer contracts controlled and validate the application request after parsing. Notification's durable consumer explicitly copies `message.MessageId` into `NotifyCommand.ID` because the direct-body parser does not do so.

## Broker authentication versus command authorization

[AuthenticationBasedQueueFactory](../Niobium.Messaging.ServiceBus/AuthenticationBasedQueueFactory.cs) uses `DefaultAzureCredential` when a namespace is configured. Without one, it selects an access-token entitlement and requests a queue SAS. See [resource access](resource-access.md#service-bus-grants) for signing keys and permissions.

A server credential authorizes a process to send or receive from a queue. It does not authorize every tenant or user named inside a body. Each command boundary must choose and document one of these models:

**Trusted private command.** Only explicitly trusted producers can send. The consumer treats their payload as a privileged request and still validates business invariants. Invoicing's `IssueInvoiceCommandConsumer` is such a path; it does not parse an end-user principal. Restrict send rights accordingly and do not issue untrusted customers a SAS for that queue.

**Delegated user command.** The consumer validates the forwarded token, issuer/audience, tenant/user binding and operation permissions itself. The platform extensions offer principal/claim parsing helpers, but ordinary body parsing does not invoke them. Tokens can expire while delayed or redelivered; design that failure policy rather than silently trusting stale token contents.

The producer broker can attach an available access token, but it tolerates an unavailable authenticator. Never infer that every message carries a user token. For a durable workflow that continues after the original user's token expires, define the original authorization decision, subsequent worker authority and revocation policy explicitly.

## Acknowledgement and failure paths

The framework's manually dequeued [ServiceBusMessageEntry](../Niobium.Messaging.ServiceBus/ServiceBusMessageEntry.cs) calls `CompleteMessageAsync` from `DisposeAsync`. Completion is therefore a side effect, not ordinary harmless cleanup. An unconditional `await using` scope can complete a message while a handler exception is unwinding.

For manual consumption, review how completion is invoked only after successful processing and how failed processing is abandoned or allowed to redeliver. The inspected entry abstraction does not provide a complete explicit abandon/dead-letter/lock-renewal policy. A transport-specific consumer requiring those operations needs an appropriate reviewed boundary, rather than assuming disposal implements them.

Functions-triggered consumers use the binding/runtime's settlement path, not this manually dequeued entry's disposal callback. Their behavior also differs: Invoicing throws on parse/validation failure; Notification's inspected notify consumer logs and returns for those failures. A normal return may permit completion under the binding's configuration. Specify whether invalid input should be dropped, dead-lettered or retried; logging alone is not a delivery policy. Do not log raw tokens, full private payloads or recipient data to make failures diagnosable.

## Domain events are not a transactional outbox

[`EnableExternalEvent`](../Niobium.Messaging/DependencyModule.cs) registers an [EventMessagingBroker](../Niobium.Messaging/EventMessagingBroker.cs). It forwards eligible internally sourced events targeted externally. The domain's persistence and later event dispatch remain separate operations.

The current event adapter constructs `new MessagingEntry<TEvent> { Value = e }` without assigning the envelope ID. Re-dispatching the same event therefore does not preserve its transport identity automatically, even if `e.ID` is stable. `EnableExternalEvent` is not an identity-preserving retry guarantee. Changing that adapter's identity semantics requires separate runtime work and compatibility tests; this guide only documents the behavior.

A successful database write followed by a failed enqueue is a possible partial outcome. Conversely, an ambiguous send can have reached the broker before the producer retries. Design recovery, deduplication and state reconciliation for these boundaries. Neither `SaveAsync(force: true)` nor repeating a send is a substitute for a concurrency/idempotency protocol.

## Notification's durable pattern

[DurableNotificationFlow](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Core/Delivery/DurableNotificationFlow.cs) provides a concrete consumer-specific design:

1. Require stable message identity and tenant; address a submission domain using tenant and a hash of the ID.
2. Fingerprint the request and freeze the rendered provider payload for a new submission.
3. Ask the domain to prepare an attempt; it can suppress, wait or permit submission.
4. Enqueue a scheduled recovery wake **before** calling the irreversible provider operation.
5. Submit with a stable provider idempotency key and attempt deadline, then record the outcome through the domain.

[NotifyCommandConsumer](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Server/NotifyCommandConsumer.cs) selects this path only when `EnableDurableSubmission` is true; otherwise it uses the legacy flow. This is not a generic broker guarantee or proof that a deployment enabled it.

A recovery wake can reuse the same ID as the original request. Broker duplicate-detection settings/windows must be compatible with that recovery design. Provider idempotency retention, ambiguous responses, lease ownership, deadlines and storage ETags also need tests. Do not call the system globally exactly-once merely because one provider supports an idempotency key.

Notification separately disables automatic redirects and removes HTTP resilience retries for its durable provider client. Its flow owns those attempts. See [HTTP client configuration](http-clients.md); disabling redirects alone does not disable retries.

## Tests before adoption

Test direct JSON and entry parsing, missing/malformed type metadata, envelope/body ID differences, same ID with changed content, duplicate and concurrent delivery, send-success/response-loss, consumer failure before/after persistence, token expiry, unauthorized producers, and scheduling under actual duplicate-detection settings.

For producer retry tests, construct separate entries for the same logical command and assert that both envelope IDs equal the intended stable command ID when assigned explicitly. Contrast this with the default entry initialization and current event-adapter path; a stable body ID alone must not be mistaken for stable transport identity. These are recommended regression cases, not tests executed by this documentation.

Test identical queue names in different namespaces and principals in the same process. The current static sender/receiver caches are keyed only by queue name, and the sender/receiver dictionaries are not concurrent dictionaries. Changing options does not prove that a cached sender points to the new namespace. These are shared framework follow-ups, not defects to copy into each application.

For each new queue, record producer/consumer ownership, trust model, schema/version, queue name, namespace, stable ID rule, retry/dead-letter behavior, reconciliation procedure and observable non-sensitive correlation fields. Use the [security checklist](security-review-checklist.md) to plan the corresponding negative tests.
