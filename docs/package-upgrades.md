# Framework package releases and consumer upgrades

Upgrade a consumer's existing direct Niobium package references to their latest
compatible published versions together. Framework releases include affected
dependent libraries, so a higher-level integration package brings its updated
framework dependencies transitively. Do not add lower-level references only to
force an upgrade that existing integration packages already provide.

This convention follows the owner's
[Notification PR #2 review](https://github.com/niobium-nz/Niobium.Notification/pull/2#discussion_r4501144615).
It does not prohibit intentional direct dependencies: a contracts library that
already references `Niobium.Abstractions` should keep and update that reference.
Review each project's role and the resolved solution graph before adding or removing
references. Merely using a namespace does not require another package reference
when an existing package or referenced project already provides the assembly.

## How release propagation works

The [Publish workflow](../.github/workflows/main.yaml) calls
[Detect-ChangedNuGetProjects.ps1](../.github/scripts/Detect-ChangedNuGetProjects.ps1).
The script selects projects with changed files under their project directories,
then follows reverse `ProjectReference` edges to include every transitive dependent.
Packable selected projects are published together with the workflow's version,
currently `4.2.<run number>`. A dependent integration library can therefore receive
a new version even when its own source did not change.

Unchanged dependencies outside that selected set are not republished. The latest
version is specific to each package; do not assign the newest release number to
every package or assume every package has that version. Root documentation changes
alone select no packages. The explicit `buildAllProjects` workflow input selects
all projects. Pull request runs build and test but do not publish packages.

## Verified example: the 4.2.70 redirect release

[Publish run 42358398851](https://github.com/niobium-nz/Niobium/actions/runs/42358398851)
released the changed Platform library and its affected dependents from commit
`f5b1b46a4a8c83e8771d7a3e9ae9dcc7044b62f0`. NuGet.org package metadata and available
versions were checked on 2026-11-07.

| Package group used by Notification | Latest published version at that check |
| --- | --- |
| `Niobium.Platform`, `Niobium.Platform.Notification.Email` and the affected Platform integration packages | 4.2.70 |
| `Niobium.Abstractions`, `Niobium.Helper`, `Niobium.File`, `Niobium.Messaging` | 4.2.66 |
| `Niobium.Database.StorageTable`, `Niobium.File.Blob`, `Niobium.Messaging.ServiceBus` | 4.2.66 |

In Notification, Core's existing `Niobium.Platform.Notification.Email` reference
can be upgraded to 4.2.70. That package explicitly depends on `Niobium.Platform`
4.2.70. Server references Core and therefore receives Platform and its dependencies
transitively. Server's existing Functions, StorageTable, Blob, ServiceBus, Resend
and ReCaptcha integration references are also upgraded to 4.2.70. Existing direct
Abstractions, File and Messaging references in Contracts/Core move to 4.2.66.
The three extra Server references to Platform, Abstractions and Helper are redundant.

### Current package metadata limitation

[New-ProjectVersionProps.ps1](../.github/scripts/New-ProjectVersionProps.ps1) gives
selected projects the release version and unselected projects the base version
`4.2.0`. That base can appear as a minimum dependency in the generated packages,
even when no 4.2.0 version exists on NuGet.org.

For example, the published 4.2.70 StorageTable, Blob and ServiceBus integration
packages request their respective adapter packages at a minimum of 4.2.0. In
Notification's upgraded graph, NuGet resolves the available 4.2.66 adapters and
reports NU1603 for these three dependencies. This is a release-metadata limitation,
not a missing assembly or a reason to restore redundant consumer references.
Keep the warning visible and record the actual resolved versions and compatibility
checks. Correcting the minimum versions belongs in a separate framework packaging
change and release; this documentation does not change the publishing scripts.

## Consumer checklist

1. Inspect existing direct references across the solution and check the latest
   compatible published version of each Niobium package. Upgrade those references
   coherently, including higher-level integrations and referenced projects.
2. Wait for successful publication and feed availability. A merge, source checkout
   or locally built candidate does not establish an available dependency.
3. Restore from the intended feed and inspect `obj/project.assets.json` or
   `dotnet list <project> package --include-transitive`. Confirm the new API's
   assembly/version, expected transitive versions and any resolution warnings.
4. Match package repository metadata to source and run relevant consumer tests
   against those packages. For data access, verify actual adapter behavior as
   described in [rich domains and data access](domain-data-access.md).
5. Document the version graph, release evidence, compatibility results and any
   remaining limitation. Do not suppress restore warnings or substitute a local
   ProjectReference as evidence that a published package works.

Unrelated package upgrades remain a separate scope decision. Package alignment
does not change application policy: [redirect configuration](http-clients.md)
still preserves HTTP resilience handlers, and a durable flow must separately own
or disable transport retries.
