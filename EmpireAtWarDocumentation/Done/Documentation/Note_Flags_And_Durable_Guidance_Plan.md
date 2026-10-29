---
category: Documentation
status: done
created: 2026-10-29
completed: 2026-10-29
acceptance: pending-user
---
# Note Flags and Durable Engineering Guidance

## Goal

- Mark notes with `revisit`, `important`, `issue`, `broken`, or `delayed`; find them from one vault entry point.
- Extract reusable rules, known issues and verified solutions from high-impact plans.
- Route every repository AI agent to relevant guidance before changes; keep historical evidence separate from current rules.

## Decision

- Chosen: built-in `tags` list with five namespaced flags: `flag/revisit`, `flag/important`, `flag/issue`, `flag/broken`, `flag/delayed`.
- Why: clickable tags, native search and existing vault MCP tag operations; multiple flags per note without another metadata field.
- Avoid: duplicate `mark`/`flags` fields, custom plugins, settings changes, a second backlog, or copying every completed plan into guidance.
- Implementation authorized 2026-10-29: flag rollout, pilot guidance extraction and shared agent policy routing. Pilot implementation work remains with its existing owners.

## Rules

| Flag | Meaning | Required context / removal |
|---|---|---|
| `flag/revisit` | Needs a later review, revalidation or guidance extraction. | Record reason and next trigger; remove after review is resolved. |
| `flag/important` | Durable, high-impact knowledge worth finding quickly. | Record impact or regression prevented; retain while useful. |
| `flag/issue` | Current unresolved concern, limitation or suspected defect. | State affected scope, evidence and next action; remove when resolved or disproved. |
| `flag/broken` | Confirmed failure: documented procedure or system behavior does not work in the stated conditions. | Record reproduction/evidence and workaround; remove after fix verification. |
| `flag/delayed` | Work deliberately deferred until a date, dependency or decision. | Record reason and resume trigger; remove when resumed or cancelled. |

- Flags describe current attention needs; `category`, plan `status`, `acceptance`, `design_state` and `record_state` retain their existing meanings.
- Allow combinations, e.g. important + revisit. Broken is stronger than issue; use broken alone for the same finding, both only for distinct findings.
- Important does not mean correct, verified or mandatory. An archived plan may be important without being current policy.
- A flag alone does not change plan status, reopen history, authorize execution or prove a defect.
- Resolved findings retain history/evidence in the note; remove their attention flag unless another open finding still needs it.
- Add flags to frontmatter, preserving existing tags and unrelated metadata. In note text, record one concise reason/action per active flag.
- A date-triggered delay may use `review_after: YYYY-MM-DD`; a dependency-triggered delay needs a concrete dependency. No automatic reminder is implied.

## Implementation

### 1. Define the flag contract and discovery

- [x] Extend [[Rules/DOCUMENTATION_ORGANIZATION]] with the vocabulary, lifecycle and frontmatter example below.
- [x] Create `Rules/NOTE_FLAGS.md` as a compact search dashboard; link it from [[00_Home]] without touching protected configuration or legacy credential sections.
- [x] Include five native search blocks plus combined attention searches; no manually duplicated list of flagged notes.
- [x] Document agent access: use vault `manage_tags` for changes; use `search_notes` with the literal flag and `searchFrontmatter: true` for discovery, then inspect actual tags. MCP text search is not Obsidian query syntax.
- [x] Verify preserved metadata and tag add/remove readback on the pilot notes; visually checking the dashboard remains a user gate if no UI access exists.

Example note metadata:

```yaml
tags:
  - optimization
  - flag/important
  - flag/revisit
```

Native Obsidian searches:

| View | Query |
|---|---|
| Important | `tag:flag/important` |
| Revisit | `tag:flag/revisit` |
| Issues | `tag:flag/issue` |
| Broken | `tag:flag/broken` |
| Delayed | `tag:flag/delayed` |
| All marked notes | `tag:flag` |
| Important notes needing review | `tag:flag/important tag:flag/revisit` |

### 2. Extract current guidance from the two pilot plans

- [x] Read each plan and research companion; inventory durable decisions, current limitations, historical fixes and evidence.
- [x] Verify candidate rules against live code, serialized assets and current reports before publishing them as implemented guidance.
- [x] Reuse an existing canonical note where possible; proposed new notes: `AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE.md` and `Rules/BATTLE_PERFORMANCE_GUIDELINES.md`.
- [x] Keep guidance concise: Goal, Rules, Decision, Implementation, Important Values, Edge Cases and Files as needed.
- [x] Use `type: engineering-reference`, list-valued `system`, a `verified` date and an exact `verification` description. These fields describe checks actually performed.
- [x] Add source-plan/research links and exact code/asset/report paths. Link source plans back to canonical guidance.
- [x] Mark pilot plans important + revisit while extraction is pending. Remove revisit once all selected lessons are reconciled; retain important. Add issue/broken/delayed only where current evidence supports them.

Addressables pilot — [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan]]:

- Candidate rules from the 2026-10-24 record: main/wreck/preview entries together in `EmpireUnitViews`, `RepublicUnitViews`, `SeparatistUnitViews`, `RebellionUnitViews`; common views in `View`; global catalogs in `Model`.
- Shared dependencies have explicit owners; preserve addresses, aliases and labels. Avoid blanket `PackSeparately` and separate preview bundles without dependency/lifetime evidence.
- Document the actual asset-assignment procedure and matching-layout checks, including intentional Resources/scene overlaps. Verify group/schema changes through Addressables APIs; preserve the repository restriction on `Assets/AddressableAssetsData` structure.
- Historical acceptance baseline: 18 bundles, 1,534,762,737 B, zero duplicated assets/objects/payload. This is a dated result, not a permanent size budget or current-build claim.
- Separate runtime gates (packed-player audio routing, early-spawn sync loading) from established bundle ownership rules.

FPS pilot — [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]:

- Extract verified culling/instancing, transformed bounds, LOD, quality-specific nebula/post-processing, particle material/shadow and fog-job lifecycle rules.
- Preserve exact verified owners and values, e.g. recorded 1,000-unit cells and 48/64/96/128 nebula samples; explain applicability instead of declaring every number universal.
- Document matched measurement conditions: seed, camera, quality, resolution, population, time scale, source revision and Editor versus Player.
- Separate implemented behavior, historical measurements, pending visual acceptance and unimplemented investigations. Do not claim a new FPS improvement from source inspection.
- The plan is still active and includes absorbed unfinished scope; guidance extraction must not close it or silently execute deferred builds/captures/tests.

### 3. Capture known issues and solutions without another backlog

- [x] Put concise issue/solution records beside the relevant guidance under Edge Cases; split a note only when size warrants it.
- [x] Each record: symptom + affected scope → cause/confidence → workaround/fix → evidence → remaining verification → owning plan link.
- [x] Label a proposed solution as proposed; only a verified fix becomes a reusable solution.
- [x] Link unresolved implementation work to its existing active plan. Create a Bugs plan only for a distinct new defect.
- [x] Keep resolved cases as regression-prevention examples, with resolved date and evidence; they do not retain issue/broken flags unless something remains open.
- [x] Treat historical exceptions and user-deferred measurements as scoped facts, not permanent global instructions.

### 4. Make guidance discoverable to every AI agent

- [x] Add a short task-routing section to repository `AGENTS.md`: asset imports/Addressables changes → bundle guide; rendering/VFX/quality/profiling changes → performance guide.
- [x] Require agents to read relevant guidance before changes and verify it against live source. Preserve authority: active instructions → repository AGENTS.md → live source/assets/output → advisory notes.
- [x] Add plan-lifecycle handoff: before closure, extract or update reusable rules/issues/solutions; record destination links or an explicit “no reusable guidance” reason.
- [x] Permit extraction from an active plan once a specific result is verified; pending work remains with its owner.
- [x] Update navigation in [[AssetPipeline/00_AssetPipeline]] and [[00_Home]]; use [[Architecture/00_Architecture]] only for implementation references that belong there.
- [x] Reconcile relevant documentation-policy wording with current repository lifecycle rules; avoid a separate competing instruction set in vault `Rules/AGENTS.md`.

### 5. Verify and close this implementation plan

- [x] All five flags documented; pilot metadata preserved; searches find pilot flags without matching unrelated topic tags.
- [x] Both pilots have canonical guidance, provenance, exact applicability, and clearly separated open issues and verified solutions.
- [x] An agent starting from repository `AGENTS.md` can find the correct guide before importing a unit or modifying rendering.
- [x] Guidance contains no unsupported current-performance claim or unverified rule copied blindly from history.
- [x] All new links resolve; no duplicate canonical rules or active backlog entries.
- [x] No Obsidian configuration/plugin/authentication changes; no Unity tests, builds, Play Mode or profiling required for this documentation rollout.
- [x] When implementation is complete, update this plan/backlog, move it to `Done/Documentation/`, repair links and read back. User-only UI checks follow the existing acceptance queue.

## Files

- [[TODOs/TODOs]] — execution index.
- [[Rules/DOCUMENTATION_ORGANIZATION]] — metadata and placement policy.
- [[TODOs/Optimization/Addressables_Bundle_Remediation_Plan - Research]] — Addressables evidence companion.
- [[TODOs/Optimization/Battle_FPS_Recovery_Plan - Research]] — performance evidence companion.
- [Obsidian tags](https://obsidian.md/help/Editing%2Band%2Bformatting/Tags) and [properties](https://obsidian.md/help/properties) — native tags list and search behavior.
- Canonical deliverables now exist at the linked guidance destinations above.

## TODO

- Planning completed 2026-10-29: inspected vault metadata/navigation and both pilot plans; native tag syntax checked against official documentation.
- Completed 2026-10-29: flag vocabulary/dashboard, canonical pilot guides, navigation and shared agent routing/handoff implemented. Guidance destinations: [[Rules/NOTE_FLAGS]], [[Rules/DOCUMENTATION_ORGANIZATION]], [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]].
- Verification: live source/serialized assets and existing reports reviewed; guide provenance and all new vault links checked. Pilot tag add/remove readback: Addressables important + issue; FPS important + broken. Original category/status/created preserved; no extraction revisit remains.
- Findings remain with active pilots: newer CIS wreck/preview group ownership needs review; non-Windows build stamping is source-confirmed broken. Historical performance/layout figures are dated; no new runtime improvement claimed.
- No tests, builds, Play Mode, profiling, Unity asset mutations or Obsidian configuration changes performed. Dashboard native query rendering remains user acceptance → [[TODOs/User_Acceptance_Queue]].
