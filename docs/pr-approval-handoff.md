# PR approval and framework release handoff

Apply the owner's standing delivery process to authorized implementation work.
Keep the implementation agent responsible through review, protected merge,
package publication and any scoped consumer upgrade. Existing authorization
continues across resumed sessions; pending review is work in progress.

## Verification and independent decision

1. Read `AGENTS.md`, the affected module guidance and current workflow triggers.
   Work on a feature branch from current main. Reproduce defects with the actual
   consumed package, then verify the source fix and compatibility tests.
2. This repository's Publish workflow runs on pull requests and main pushes,
   with publication disabled for PR events. After local verification, the author
   creates the PR to obtain its complete CI evidence. The mandatory platform test
   step executes the whole platform test project, including identity regressions.
3. Verify the author connection. For `niobium-nz` authorship, request `5he11` and
   explicitly invoke the separate `pr_approver` agent. Requesting that GitHub
   reviewer alone does not invoke an independent review. Supply the exact PR/head,
   source baseline, tests, CI and native review evidence without directing a verdict.
4. Monitor native automatic code/security review and all human comments on the
   current head. Fix actionable findings and repeat affected tests and current-head
   CI. Native findings review and a formal GitHub decision are separate steps.
5. The independent approver verifies its authorized identity, reviews the actual
   head and records its own decision only after required verification is complete.
   The implementation agent must not self-approve. A valid existing current-head
   approval is sufficient; do not require another approval merely to resume work.

## Merge, publication and adoption

Verify current main ancestry, resolved threads, required repository protections,
the current-head approval and equality of tested/proposed merge trees. Merge
through GitHub with the verified expected head and all configured protections
satisfied, under the standing authorization. Never push main, weaken protections
or manufacture a status/approval.

Monitor the main Publish run to a terminal conclusion. Check the changed-project
selection and all affected transitive dependent packages, then verify feed
availability and package repository metadata against the actual merge. Follow
[package upgrades](package-upgrades.md) when updating consumers: their existing
higher-level references must resolve the real published release and pass their
own delivery process. A source merge or local candidate package is insufficient.

Record final run/package evidence and limitations on the PR. Report an external
blocker only after a concrete failed handoff or dependency, with the attempted
recovery and next action. Pending review/publication remains active delivery work.
Keep the current task's scope and explicit production-data restrictions intact.
