# Niobium developer documentation

Start here when extending Niobium or building a service on it. These guides describe the framework as implemented, using Store, Notification and Invoicing as consumer evidence. They are not a claim that every consumer uses every framework feature or that deployed infrastructure has been security-audited.

## Reading paths

| Task | Read |
| --- | --- |
| Understand package boundaries, host composition and coding conventions | [Architecture and conventions](architecture-and-conventions.md) |
| Name model keys or change ID, JSON or Blob-path representations | [Model contracts](model-contracts.md) |
| Add a domain, entity, repository or conditional write | [Rich domains and data access](domain-data-access.md) |
| Add authenticated endpoints or work with identity/access tokens | [Security, authentication and tokens](security-and-tokens.md) |
| Grant direct access to Tables, Blob containers or queues | [Resource authorization and credentials](resource-access.md) |
| Read, write or expose files | [Files and Blob storage](files-and-blob-storage.md) |
| Publish commands, consume events or design retries | [Messaging and delivery](messaging.md) |
| Find an implementation example and understand its limits | [Consumer patterns](consumer-patterns.md) |
| Review a security-sensitive change or pick regression tests | [Security review and known limitations](security-review-checklist.md) |
| Change an outbound HTTP client | [HTTP client configuration](http-clients.md) |
| Upgrade a consumer or release a framework change | [Package releases and consumer upgrades](package-upgrades.md) |

For a new consumer, read architecture, security, resource access and messaging before copying a startup module. For framework work, also read the root [AGENTS.md](../AGENTS.md) and the existing package-level README for the affected module.

## Evidence baseline

This documentation review was performed on 2026-11-08 against the default `main` branches at these commits:

| Repository | Inspected commit |
| --- | --- |
| [Niobium](https://github.com/niobium-nz/Niobium/tree/0f385e8274c4bcbf2a186b745c928361af0215d9) | `0f385e8274c4bcbf2a186b745c928361af0215d9` |
| [Niobium.Store](https://github.com/niobium-nz/Niobium.Store/tree/db600f117002509c932d8d60321f9892f8cb7263) | `db600f117002509c932d8d60321f9892f8cb7263` |
| [Niobium.Notification](https://github.com/niobium-nz/Niobium.Notification/tree/adb9e59dd4428e1c8bf496fc40a42699bd003ba0) | `adb9e59dd4428e1c8bf496fc40a42699bd003ba0` |
| [Niobium.Invoicing](https://github.com/niobium-nz/Niobium.Invoicing/tree/1d7bd30b848fa0592656e7ca4a9671603252b918) | `1d7bd30b848fa0592656e7ca4a9671603252b918` |

Framework source links are relative to this checkout. Consumer implementation links are pinned to the inspected commits. When behavior changes, update both the explanation and its source references.

The [model-contract guide](model-contracts.md) adds a separately dated investigation
on 14 November 2026. It lists its own inventory baselines and pins its proposed
Notification API example to the companion implementation revision. That example
does not change the older baseline of the other guides or establish deployment.

### Source is not the resolved consumer package graph

The inspected Server project files declare the following direct integration versions:

- [Store Server](https://github.com/niobium-nz/Niobium.Store/blob/db600f117002509c932d8d60321f9892f8cb7263/src/Niobium.Store.Server/Niobium.Store.Server.csproj): Functions, StorageTable, ServiceBus, ReCaptcha and Stripe at `4.0.60`.
- [Notification Server](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Server/Niobium.Notification.Server.csproj): Functions, StorageTable, Blob, ServiceBus, Resend and ReCaptcha at `4.2.70`.
- [Invoicing Server](https://github.com/niobium-nz/Niobium.Invoicing/blob/1d7bd30b848fa0592656e7ca4a9671603252b918/src/Niobium.Invoicing.Server/Niobium.Invoicing.Server.csproj): Identity at `4.0.61`; Functions, StorageTable, ServiceBus and Profile at `4.0.60`.

These are declarations, not a fresh restore, a complete inventory of every project, a claim about the newest published versions, or proof of deployment. The older consumer references do not establish that every detail of current framework source is present in their binaries. Verify the resolved dependency graph and package-to-source correspondence before implementing or diagnosing behavior. The existing [package guide](package-upgrades.md) describes that process and its separately dated release evidence.

## How to interpret the guidance

**Implemented** describes a path visible in the inspected source. **Consumer pattern** describes an application decision, not a universal framework policy. **Recommendation** describes a development safeguard or proposed test; it does not mean the safeguard is already enforced.

The review traced host registration, HTTP entry checks, token parsing and exchange, resource grants, storage factories, file operations, messaging transport and representative domain/flow code. It did not run a penetration test, contact production services, validate live RBAC or secrets, restore all consumer packages, or execute their runtime test suites. The known-limitations guide records source-level findings and verification work rather than claiming deployed exploits or delivered fixes.

## Keeping the documentation useful

A behavior-changing PR should name the affected abstraction, the consumer flow that demonstrates it, compatibility and migration constraints, failure semantics, and the tests that support the change. Keep security decisions at their actual trust boundary. Do not promote a workaround into a framework guarantee merely because one consumer currently uses it.

Documentation-only work should remain outside package project directories when it does not need to alter package contents. The existing workflow builds pull requests but gates NuGet publication away from pull-request events; release selection and merge effects are described in [package upgrades](package-upgrades.md). Do not merge, publish or deploy as an incidental part of a documentation task.
