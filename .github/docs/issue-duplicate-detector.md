# Issue duplicate detector

[`issue-duplicate-detector`](../workflows/issue-duplicate-detector.md) checks new
and reopened issues in `dotnet/maui`. Maintainers can manually
check an existing open issue. It posts at most one advisory report per run and
never changes labels, closes issues, or asks reporters to close an issue.

## Existing automation and improvement

MAUI previously received `github-actions[bot]` comments explicitly described as
finding similar issues **based on the issue title**, with decimal similarity
scores. Examples: [issue #22884](https://github.com/dotnet/maui/issues/22884#issuecomment-2152048565)
and [issue #22849](https://github.com/dotnet/maui/issues/22849#issuecomment-2149701059).
The latter suggested different CollectionView symptoms across platforms.
Those similarity scores are not probabilities of a shared defect.

At implementation time, the current workflow definitions and Policy Service
configuration contained no dedicated general-purpose duplicate detector.
This workflow complements the area/platform-only `agentic-labeler` and the
maintainer-authorized `/issue triage` command; neither is broadened.
The triage policy still requires an authorized canonical duplicate disposition,
not an AI probability, before applying a duplicate label.

The new detector compares full reports and comments, reproduction conditions,
platforms, handler generations, version boundaries, and diagnostics. Historical
title-only suggestions can seed discovery but cannot justify a probability.
Closed reports are explicitly identified; a recurrence after a fix may instead
be a new regression.

## Report contract

### Rate-aware discovery repair

The five accepted trials at `81d416ab1c34b64bbbaf2974815d31f25e7eab95`
all emitted `report_incomplete` after installation semantic-search HTTP403 errors;
they posted nothing and produced no validated probabilities. Those five dispatches
remain spent. This repair does not authorize reruns or modify the fixed sink.

The fork now uses the same `IssueDuplicateSearch.cjs` read-only discovery service
as the production PR repair: literal keyword/phrase REST searches restricted to
public `dotnet/maui`, eight queries, 20 results per query, one page, and at most
16 search HTTP attempts including retries. Requests are serial and at least seven
seconds apart, with bounded waits for `Retry-After`/`x-ratelimit-reset` and
secondary-limit exponential backoff. Whole workflows queue under one group with
`queue: max`, not separate per-source groups. This is ordinary lexical discovery;
the native semantic-search tool is no longer available to the agent.

A trusted post-agent step retains run/source/context-hash-bound discovery status
and stops the exact service process. Both publication and completed no-match
outcomes require successful, nonempty, finished discovery; errors cannot be
turned into empty results. The existing authoritative detector gate, compact
report, fixed fork destination, mandatory probabilities and full-evidence/freshness
validation remain unchanged. No new dependencies or repository secrets are needed.
The service uses only the agent job's existing read-only `GITHUB_TOKEN`, not a
rotated credential to evade quotas. The two narrow DIFC exemptions are now
`safeoutputs` and this fixed public-repository `duplicate-search` service; their
acceptance remains `private:dotnet/maui`, not a wildcard.

### Scoped search-sink rendering repair

The previous compiler patch removed sink visibility only for `safeoutputs`.
It still explicitly emitted public visibility for `duplicate-search`, which
overrode its accept patterns after scoped GitHub reads. Gateway exemptions stop
default visibility injection; they do not remove an explicitly emitted value.

The fork compiler's cumulative reviewed patch now derives an independent policy
for each exact gateway server ID in custom JSON/TOML and built-in renderers.
It omits visibility only for the workflow's two named exemptions, preserves
`private:dotnet/maui` acceptance, and leaves unlisted sinks restricted. The
official source remains `c35393777e5604a63721d09512263b1383301d4f`; the new
patch SHA-256 is
`8fbe948b2ad189da0c6d57272ea0d3408296c19336fe0d3bd4067d882df4111c`.
The production compiler received the same isolated rendering correction in
[`86885522c56a777b4734e6b5d81b41ce244a513f`](https://github.com/kubaflo/gh-aw/commit/86885522c56a777b4734e6b5d81b41ce244a513f).
Neither compiler correction changes the fork's action, engine or gateway pins,
compact report, fixed destination, source whitelist, or spent dispatch approvals.
No hosted run is authorized by this repair.

Every suggested match contains an **integer duplicate-probability estimate**,
shown as a percentage, supporting evidence, differences/uncertainty, and linked
excerpts from both reports. These are **uncalibrated AI estimates**, not measured
statistical probabilities or confirmed duplicate decisions.

| Probability | Publication                                                        |
| ----------- | ------------------------------------------------------------------ |
| 85-100%     | Likely duplicate, requiring distinctive compatible evidence.       |
| 60-84%      | Possible duplicate, with explicit missing evidence or uncertainty. |
| 0-59%       | Not posted; similarity alone is insufficient.                      |

The schema and trusted publisher reject missing, fractional, out-of-range, or
below-threshold scores; self-matches, repeated candidates, pull requests,
unverified source excerpts, and stale evidence also fail validation.
The publisher renders the report and GitHub links itself, so free-form agent
prose cannot bypass the probability requirement. Reports use closed
**Duplicate Analysis** and **Follow-up** sections with nested candidate details.
Every probability remains visible above the expandable sections. Two blue badges
identify the scope and source issue; `img.shields.io` is allowed so publication
sanitization preserves them without disabling URL filtering.

The trusted search service enforces eight logical queries, 20 results per query,
one page, at most three attempts per query and 16 search HTTP attempts across
the whole agent job. Each HTTP request has a 15-second timeout; waits are capped
at 120 seconds individually and 180 seconds cumulatively. The GitHub gateway
separately enforces 30 `issue_read` calls per MCP session. These limits do not
cover public repository checks or the trusted collector/publisher's API reads.
Ten investigated candidates remain an **agent instruction**, not an investigation counter.
The trusted collector/publisher
enforces five published matches, 300 comments per issue, and 1 MiB regular JSON
files. The agent job also has a 15-minute timeout. Missing required evidence
is an incomplete run, not proof that there are no duplicates.
An unchanged report is suppressed. Changed reports are posted as new comments;
existing bot and human comments are never edited, deleted, or minimized.
The fingerprint covers the trusted rendered summary, assessments, excerpts,
and static follow-up, not the evidence-freshness hash or per-run workflow link. An unrelated target
comment therefore cannot defeat suppression when the rendered report is unchanged;
the separate evidence hashes still require fresh target and candidate snapshots.
The visible report fingerprint survives gh-aw's content sanitization. Reports
are recognized by the bot author, trusted workflow markers, and exact fingerprint
even when the publisher prepends a caution. Recognized reports are excluded
from evidence and target hashes.
Safe-output publication is pinned to the built-in `GITHUB_TOKEN`, keeping
comment authors consistent with the strict `github-actions[bot]` provenance
check instead of accepting a configurable publisher identity.
Every candidate's content hash, timestamp, state, and lock status are rechecked
after report construction, followed by the target's final eligibility/evidence
check. Validation and GitHub publication are not an atomic transaction; changes
after the final checks remain possible.
Closed, locked, or already-marked-duplicate targets are skipped. Issues with a
known bot author are skipped automatically but can be checked manually.
Deleted-account author identities are preserved as `null`, without dropping
their evidence or treating unknown authors as trusted workflow bots.

## Manual preview and publication

Run from the repository default branch after the workflow is merged:

```bash
gh aw run issue-duplicate-detector --ref main --raw-field issue_number=12345 --raw-field staged=true
```

Replace `12345` with the real issue number. Manual runs default to `staged=true`:
the same analysis and validation run, but the trusted validator writes the
constructed expandable report and visible probabilities to the safe-output job's
**Validated duplicate report preview** summary rather than posting them.
This preview appears only after all validation and final freshness checks,
and precedes publication sanitization, cautions and provenance wrappers. Invalid,
stale, no-match, and identical-report-suppressed outputs produce no report preview.
To publish, explicitly pass
`--raw-field staged=false`. New/reopened issue events publish qualifying reports.
No-match runs intentionally produce no comment.
The run-scoped evidence artifact is overwritten when the collector reruns, so
full reruns do not collide with immutable uploads. Failed-job-only reruns can
still download the completed collector's artifact; final freshness checks remain
required before publication.

## Fork-only five-issue publication trial

The isolated `duplicate-detector-fork-trial-20261008` branch in `kubaflo/maui`
reuses the registered `daily-repo-status` dispatch path. This is not an upstream
deployment or proof of production readiness. The previous single-source trial
published to [fork issue 945](https://github.com/kubaflo/maui/issues/945); that
destination is consumed and is not reused. The new five-run batch reads only
current public `dotnet/maui` evidence for issues 39280, 39270, 39263, 39169 and 39132. Qualifying reports go only to owner-created
[fork issue 946](https://github.com/kubaflo/maui/issues/946), an attributed summary
of those real reports rather than invented issue evidence.

The fork adapter binds that destination's issue ID, title, author and body hash,
requires it to remain open/unlocked, and checks it again after source validation.
Only prior `github-actions[bot]` reports for different authorized sources are
accepted, with native provenance and matching workflow run metadata from the
same fork commit. This public metadata is read without credentials or additional
publisher permissions; HTTP failures reject the run. Unexpected comments or an already-reported source reject the
run. The destination admits fewer than five existing comments before publication.
These checks and publication are not atomic; the owner-directed batch consists
of five distinct dispatches, not an authorization for extra retries.
The native `add-comment` target and repository are fixed independently of agent
output; the publisher uses only the fork's built-in `GITHUB_TOKEN`.
All source, cardinality, score, excerpt, freshness and provenance checks remain.
Before any comment or completed no-match outcome is accepted, the validator
requires the authoritative detector-job conclusion to be `success` or intentional
`warning`, before branching on the output type. Failed, missing or unexpected
states are rejected. Native warning cautions remain intact.
The fork retains its stricter `continue-on-error: false` detection policy.

The fork adapter selects a compact presentation matching the owner's edited
comment: visible estimated probabilities, closed **Duplicate Analysis** and
candidate sections, linked candidate headers, and distinct likely/possible
icons. It omits Follow-up, the intro quote, visible disclaimer, workflow-result
link and fingerprint. Scores remain uncalibrated estimates; candidate evidence
and uncertainty are unchanged. Trusted native workflow provenance is retained.
Because native publication strips content-supplied HTML comments, the compact
report's fingerprint is retained in the trusted validation artifact and logs,
not a purported hidden report marker. Per-source suppression on the fixed batch
destination uses native provenance and the trusted source badge. The standard
renderer and its visible-fingerprint suppression contract remain available;
the upstream production branch has not been changed by these trials.

Manual dispatch still defaults to a staged preview. The owner requested five
actual tests, each with a different authorized `issue_number` and `staged=false`.
No upstream writes, extra accepted retries or default-branch changes are authorized.
Compile it with `bash .github/scripts/CompileIssueDuplicateForkTrial.sh`; this
preserves the existing immutable v0.89.21 compiler, action and patched gateway
pins and does not force staging over the trusted dispatch flag.

The workflow uses the existing `copilot-pat-pool` environment and GPT-6.1 Sol
configuration. No additional external service, model provider or repository secret is needed.
The workflow-local PAT selector follows `issue-triage`'s existing pattern to
keep the activation guard explicit with the pinned gh-aw v0.86.2 compiler.
Prepared evidence and validator code come from trusted default-branch
infrastructure; issue/reproduction content is never executed.
The GitHub MCP's repository guard restricts issue reads to `dotnet/maui`;
the trusted discovery service separately enforces that same public repository.
Repository scope is enforced rather than left to the prompt.
The gateway's public-repository scope override is disabled so it cannot broaden
that explicit scope to all public repositories.
Exact repository scope conservatively carries the `private:dotnet/maui` secrecy
label even for public reports. The workflow declares
`private-to-public-flows: [safeoutputs, duplicate-search]` for publication and
the fixed public-repository read-only search service only; their write-sinks
still accept only `private:dotnet/maui`. The compiler omits sink visibility only
for these two named servers, not for unrelated sinks,
and does not emit a blanket `allow` or wildcard exemption.
The trusted collector and validator check live repository metadata and reject
anything other than public `dotnet/maui`, including a final check before a report
is approved for publication. The compiled Copilot command grants neither shell
nor filesystem-write tools.

## Editing and validation

Stock gh-aw v0.86.2 rewrites structured GitHub `allowed` entries to tool names
during default-tool normalization, dropping their `max-calls` metadata, and
grants filesystem-write permission even when `edit: false` is declared.
[`CompileIssueDuplicateDetector.sh`](../scripts/CompileIssueDuplicateDetector.sh)
builds an isolated compiler from the immutable
[`8b600a3beee3591b7add4c79e595d9481a470cc4` correction](https://github.com/kubaflo/gh-aw/commit/8b600a3beee3591b7add4c79e595d9481a470cc4)
on top of v0.86.2. The correction preserves both gateway call limits and
Copilot's explicit tool permissions, honors disabled editing, and wires the
scoped safe-output exemption consistently in strict validation and JSON/TOML
configuration. It does not install or replace the user's `gh aw` extension.
Compiler metadata identifies the patched build.
Only the official MCP gateway is upgraded, to
[`v0.4.30`](https://github.com/github/gh-aw-mcpg/releases/tag/v0.4.30), whose
[`sink-visibility correction`](https://github.com/github/gh-aw-mcpg/commit/ece856095ca6445fc4f41884512478b1d4e2851c)
honors the safe-output exemption. Its immutable image digest is
`sha256:ab5a436a1490438db473e4e3d4c973cb1d75e3cb233fb08b73d31b42d7d18fba`.
All other runtime action/container pins and engine versions remain unchanged.
The custom compiler version is not recognized by the runtime's official-release
checker, so an explicit pre-agent step runs the same compatibility and revocation
check against the v0.86.2 base before inference.

Go 1.26.5 or later is required for this build-only workaround. The helper caches
the pinned source and compiler under `${XDG_CACHE_HOME:-$HOME/.cache}/maui/gh-aw`,
checks the source revision and cleanliness before every build, and removes
publication/inference tokens from both the build and compiler execution using
the same command-scoped environment wrapper. The build
sets `GOWORK=off`, so parent- or source-local `go.work` files cannot override the
pinned module's dependency selection. This isolates Go workspace resolution,
not every aspect of the build environment. Replace this helper with a fixed
official compiler after verifying that it preserves the counter policy, explicit
Copilot allowlist, disabled editing and scoped safe-output policy; do not
regenerate with stock v0.86.2 or hand-edit the generated lock.

Commit the source, compilation helper, trusted publisher and compiled lock file together:

```bash
node --check .github/scripts/IssueDuplicates.cjs
node --check .github/scripts/IssueDuplicateSearch.cjs
bash .github/scripts/CompileIssueDuplicateDetector.sh
shellcheck .github/scripts/CompileIssueDuplicateDetector.sh
actionlint -oneline -ignore 'unexpected key "queue" for "concurrency" section' .github/workflows/issue-duplicate-detector.lock.yml
git diff --check
```

The native actionlint v1.7.12 does not yet recognize GitHub's supported
[`concurrency.queue`](https://docs.github.com/en/actions/how-tos/write-workflows/choose-when-workflows-run/control-workflow-concurrency)
field, emitted by gh-aw's conclusion job. The narrow ignore above covers only
that compatibility warning; the compiler still validates the workflow schema.
The native linter also runs the existing ShellCheck without requiring Docker.
