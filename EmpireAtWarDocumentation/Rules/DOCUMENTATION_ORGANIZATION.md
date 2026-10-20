---
type: policy
system:
  - documentation
updated: 2026-10-20
---
# Documentation Organization

## Rules

- Repository `AGENTS.md` is the policy authority. Live source and serialized assets establish implemented behavior; notes are advisory and dated.
- One canonical note per purpose; link across areas instead of maintaining the same facts twice.
- Gameplay rules belong in GameDesign; engineering rules belong in Rules. Import records do not establish current balance.
- Current references use `type: design-reference`, list-valued `system`, `design_state: implemented`, `verified` date and `verification: source-and-assets`.
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
6. On completion, move the plan and companions to Done, including its entire backlog entry; do not close unrelated active work.

## Edge Cases

- An import can be technically integrated while playtesting/balance remains pending; keep its active plan open.
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
