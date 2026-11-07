# Working in Niobium

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
- Inspect workflow triggers before pushing or opening PRs: this repository has a
  publication workflow. Respect the current task's deployment/publication boundary.
- Read [HTTP client configuration](docs/http-clients.md) before transport changes.
  Configure redirects through shared framework support; preserve existing handlers
  and keep redirect policy separate from retry ownership. A consumer must pin an
  actual released package before relying on a new framework API.
