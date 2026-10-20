---
category: Documentation
status: done
created: 2026-10-20
completed: 2026-10-20
---
# Vault Organization and GameDesign References

## Goal

- Separate current game rules, implementation, asset workflows, proposals, external references and historical evidence.
- Add source-verified GameDesign references and repair navigation without changing Obsidian settings or credentials.

## Rules

- Scope authorized 2026-10-20: Markdown documentation only; preserve existing source/asset changes.
- Preserve original note filenames when moving; retain useful contents, dated evidence and plan lifecycle.
- Repository `AGENTS.md` governs policy; live source/serialized assets govern implemented behavior.
- Protected: `.obsidian/`, MCP configuration, credentials and existing credential-bearing setup blocks.

## Implementation

1. [x] Inventory 189 notes and capture note contents, paths and protected-file hashes.
2. [x] Move import, external-reference, proposal and historical notes using vault MCP; retain a migration map.
3. [x] Add current design references for economy, production/research, capture/structures, units/factions, combat, abilities/superweapons, modes/victory, commands/controls, visibility and save/load.
4. [x] Add navigation and documentation ownership rules; distinguish historical claims from current verified behavior.
5. [x] Repair inbound links, obsolete machine-specific source links and implicated duplicate entrypoints.
6. [x] Verify persisted notes, destinations, internal links, cited source paths and protected content.
7. [x] Move this plan and its migration map to Done/Documentation and update the backlog.

## Important Values

- Baseline: 189 Markdown notes; 51 under GameDesign, including 47 Import filenames.
- Static documentation validation only; no runtime code/asset edits, Unity tests or playtesting.
- Current design references use `verified: 2026-10-20`, `verification: source-and-assets`; balance/playability remains unverified unless separately recorded.

## Files

- [[Rules/DOCUMENTATION_ORGANIZATION]]
- [[GameDesign/00_GameDesign]]
- [[Done/Documentation/Vault_Organization_And_GameDesign_References - Migration Map]]

## Outcome

- Reorganized 65 existing notes; preserved all 189 original note destinations and filenames through explicit moves.
- Added 10 GameDesign references and purpose-specific indexes. Current vault: 211 notes.
- Split Empire fleet asset mapping from its design proposal; marked proposals, external sources, superseded guidance and dated import/audit records.
- Compressed backlog navigation while preserving detailed entries in a historical snapshot; unrelated plan statuses remain unchanged.
- Updated implicated repository documentation entrypoints and archived this plan with its migration map.

## Verification

- All vault wikilink targets and heading anchors resolve; all 12 distinct local Markdown link targets and 25 links in the seven updated repository entrypoints exist.
- All 163 distinct repository source/asset paths cited by the 10 new design references exist; final review also verified relay capture, reinforcement deployment and current mouse controls.
- MCP readback confirmed destination notes; all 65 former content paths and both former active documentation-plan paths are absent.
- Credential strings in all three setup notes and the complete legacy Home setup section match the captured originals.
- 19 protected configuration files match baseline hashes. The already-dirty `.obsidian/workspace.json` changed during the session; agents did not write or restore it.
- Git whitespace validation passed with Windows CRLF recognized. No runtime tests, playtesting, source edits or asset edits were performed.
