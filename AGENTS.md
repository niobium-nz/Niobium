# Working in Niobium

Read [model names and contract ownership](docs/model-contracts.md) before adding or
changing a Table model, key, fingerprint, serialized shape or file reference.
Use business property names with EntityKey attributes. Models privately own their
representation; domains own business rules and transitions, flows coordinate,
and framework adapters map/persist. Keep existing key/wire values compatible and
follow the documented separate rollout for legacy public contract renames.

Start with the [developer documentation](docs/README.md) for the reviewed framework
and consumer baselines, architecture and implementation conventions. Read
[security and tokens](docs/security-and-tokens.md), [resource access](docs/resource-access.md)
and the [security checklist](docs/security-review-checklist.md) before changing
identity, grants, tenant boundaries or credential/client caches. Current behavior,
consumer-specific policy and recommended hardening must remain distinct.

Read [rich domains and data access](docs/domain-data-access.md) before changing
business abstractions, repositories, mapping or examples. It records the source
and package versions inspected and distinguishes current behavior from follow-ups.

- Consumer flows coordinate work; `GenericDomain<TEntity>` subclasses own business
  invariants and meaningful mutations through `IRepository<TEntity>`.
- Keep backend SDK clients/factories in adapters. Extend shared support when needed
  rather than duplicating mappers and persistence operations in each application.
- Use enums for finite business states/outcomes and explicit compatible storage
  representations. Preserve existing wire/storage values unless migration is scoped.
- Execute I/O and state-changing actions before an `if`, then test a named result.
- Match published package versions to source. Do not assume a source fix has reached
  consumers; document the release/rollout dependency and test actual adapter behavior.
- Read [package upgrades](docs/package-upgrades.md) before changing consumer package
  references. Releases include transitive dependent libraries. Upgrade existing
  higher-level references together and inspect the resolved graph instead of adding
  redundant lower-level pins. The latest published version can differ by package.
- Inspect workflow triggers before pushing or opening PRs: this repository has a
  publication workflow. Respect the current task's deployment/publication boundary.
- Read [HTTP client configuration](docs/http-clients.md) before transport changes.
  Configure redirects through shared framework support; preserve existing handlers
  and keep redirect policy separate from retry ownership. Verify that a consumer's
  existing package references resolve an actual release containing the new API.
- Read [messaging](docs/messaging.md) before queue changes. Specify producer trust,
  stable operation identity, partial-failure recovery and settlement. Do not equate
  token propagation with authorization, disposal with harmless cleanup, or domain
  callbacks with a transactional outbox.
- Read [file access](docs/files-and-blob-storage.md) before changing Blob paths,
  stream ownership or file grants. A tenant-looking prefix is not an access boundary.
- Use the [consumer examples](docs/consumer-patterns.md) as evidence, not universal
  policy. Update the relevant documentation and negative tests when behavior changes;
  do not implement a documented limitation as a required pattern.
