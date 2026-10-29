---
type: policy
system:
  - documentation
updated: 2026-10-29
---
# Documentation Organization

## Rules

- Repository `AGENTS.md` is the policy authority. Live source and serialized assets establish implemented behavior; notes are advisory and dated.
- One canonical note per purpose; link across areas instead of maintaining the same facts twice.
- Gameplay rules belong in GameDesign; engineering rules belong in Rules. Import records do not establish current balance.
- Gameplay references use `type: design-reference`, list-valued `system`, `design_state: implemented`, `verified` date and an exact `verification` description.
- Reusable engineering guides use `type: engineering-reference`, list-valued `system`, `verified: YYYY-MM-DD` and an exact `verification` description of checks actually performed. Link source plans/research and current code/asset/report paths; separate implemented rules, historical evidence, proposed fixes and open work.
- `verified` means the recorded checks only; source inspection does not establish playtest balance, runtime acceptance or passing tests.
- Plans retain `category` and `status: todo | in-progress | blocked | done`. Use `design_state` for design maturity and `record_state` for historical evidence.
- Do not change Obsidian settings, credentials, plugin state or MCP configuration during documentation maintenance.

## Important Values

| Area | Canonical purpose |
|---|---|
| `GameDesign/` | Current player-facing rules, data ownership, formulas, limits and edge cases |
| `GameDesign/Proposals/` | Proposed design; acceptance is per decision, not implied by folder membership |
| `GameDesign/References/` | External-game research or material with unverified provenance |
| `Architecture/` | Current implementation responsibilities, dependencies and lifecycle contracts |
| `AssetPipeline/Guides/` | Repeatable import/authoring procedures |
| `AssetPipeline/Imports/` | Per-asset provenance, integration and dated verification records |
| `AssetPipeline/History/` | Superseded asset integrations |
| `Tooling/` | Tool entrypoints; detailed operational source stays beside its repository tool |
| `Rules/` | Engineering/documentation conventions |
| `TODOs/<Category>/` | Active execution plans and their research/mapping companions |
| `Done/<Category>/` | Completed plans and companions; completion is limited to agreed scope |
| `Artifacts/` | Dated audits, historical snapshots and retained evidence |

## Implementation

1. Read current code/data before asserting implemented rules. Record exact repository paths, relevant symbols, values and units.
2. Give each gameplay domain one entry point linking rules, implementation, asset sources, proposals and related plans.
3. Preserve provenance and historical values; label them rather than silently refreshing an old audit.
4. Keep plan summaries in `TODOs/TODOs.md`; execution evidence stays in plans or explicitly linked historical records.
5. Move notes through vault MCP with an old/new map. Repair links, read back destinations and verify old paths are absent.
6. Before closure, extract reusable rules, issues and verified solutions into canonical guidance; record destination links or an explicit “no reusable guidance” reason in the plan. Verified results may be extracted from an active plan while unfinished work stays with its owner.
7. On completion, move the plan and companions to Done, including its entire backlog entry; do not close unrelated active work. User-only gates go to [[TODOs/User_Acceptance_Queue]] with `acceptance: pending-user`; unimplemented scope stays active. Repository `AGENTS.md` owns the full lifecycle.

## Edge Cases

- Technical integration with only user playtesting/balance gates → close with `acceptance: pending-user` and queue the gates. Missing abilities, assets or decisions → keep active.
- A historical finding marked open does not automatically reopen a completed umbrella task. Preserve `finding_status` separately.
- Unit values can be overridden by faction entries, shared profiles, prefab mounts, research or modes. Cite the actual owner and scope.
- Use full vault paths for wikilinks. External source paths are repository-relative code references, not missing vault notes.
- Retired setup documents containing credentials remain at their paths with historical labels; never copy credential values into new notes.

## Files

- [[00_Home]]
- [[GameDesign/00_GameDesign]]
- [[TODOs/TODOs]]
- `AGENTS.md` — canonical repository policy.
- [Obsidian links](https://obsidian.md/help/links), [properties](https://obsidian.md/help/properties), [Bases](https://obsidian.md/help/bases).
- [Diátaxis documentation purposes](https://diataxis.fr/start-here/) — organization rationale; no plugin/settings change required.

## Note Flags

- Use the built-in list-valued `tags` property; preserve existing tags and unrelated metadata. Discovery → [[Rules/NOTE_FLAGS]]. Do not add duplicate `mark`/`flags` properties.

| Tag | Meaning | Required context / removal |
|---|---|---|
| `flag/revisit` | Review, revalidation or guidance extraction needed. | Reason + next trigger; remove when review resolves. |
| `flag/important` | Durable, high-impact knowledge. | Impact or regression prevented; retain while useful. |
| `flag/issue` | Unresolved concern, limitation or suspected defect. | Scope + evidence + next action; remove when resolved/disproved. |
| `flag/broken` | Confirmed failure under stated conditions. | Reproduction/evidence + workaround; remove after fix verification. |
| `flag/delayed` | Work deliberately deferred. | Reason + resume date/dependency/decision; remove on resumption/cancellation. |

- Record one concise reason/action per active flag in note text. Combinations are allowed; use broken alone for the same finding, issue + broken only for distinct findings.
- Important does not establish correctness or policy. Flags neither change `category`, `status`, `acceptance`, `design_state`, `record_state` nor reopen history, authorize work or prove defects.
- Issue/solution records live under the relevant guide's Edge Cases: symptom + scope → cause/confidence → workaround/fix → evidence → remaining verification → owning plan. Label proposals; only verified fixes become reusable solutions. Split a note only when size warrants it; unresolved work links to its existing plan.
- Resolved findings retain evidence/history with resolved date; remove their attention tag unless another open finding needs it. Optional `review_after: YYYY-MM-DD` records a date trigger; no automatic reminder is implied.
- Agents: use `manage_tags` to add/remove flags; discover with `search_notes` using the literal `flag/important` (or other flag) and `searchFrontmatter: true`, then inspect the actual `tags` list. Text matches may be examples or prose; MCP text search does not interpret Obsidian query syntax.

```yaml
tags:
  - optimization
  - flag/important
  - flag/revisit
```
