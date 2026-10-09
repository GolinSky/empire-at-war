---
category: Features
status: done
created: 2026-10-06
reopened: 2026-10-09
outcome: Workshop ISD II replacement committed; seven engine surfaces/emission
  restored and 38 obsolete visuals removed.
completed: 2026-10-09
---
# ISD II

## Goal

- Replace Empire ISD II `205` visuals from Workshop `1770851727 / Star_Destroyer_II`.
- Remove superseded ISD II visuals after checking retained dependencies; keep ISD I assets and gameplay identity distinct.

## Implementation

- [x] Read full ALO import, organization and required UI guides; inspect live integration and balance.
- [x] Resolve XML inheritance and source-specific structure/all 13 turret attachments.
- [x] Convert 16 living/death visual parts through Blender; preserve geometry, UVs, material slots, bones and hierarchy.
- [x] Preserve and label 38 obsolete visuals/snapshots; update the existing model/gameplay/wreck/placement/icons in place.
- [x] Rebuild 24 targetable weapons and three systems; add unique weapon profiles/audio 70/71 and record source mappings.
- [x] Fit shield/collider/hull limits/fog/selection, ownership and wreck bindings; correct inherited banking scale and stripe surface depth.
- [x] Persist/import Unity assets; verify saved geometry, references, registrations, obsolete labels and screenshots.
- [x] Restore all seven engine surfaces and visible glow; rear/top/bottom closeups and material reimports verified.
- [x] Delete 38 obsolete ISD II visuals/snapshots and four empty folders; zero retained Unity asset dependencies.
- [x] Commit only ISD II replacement/follow-up changes: `f0b035f9`; unrelated working changes preserved.
- [x] Archive plan/research, reconcile backlog and verify moved links.

## Decision

- Preserve live `5,500/4,200/25` hull/shields/speed, economy, fighter bays and abilities. Historical `21,000/18,000` notes are not current balance.
- Own replacement hull/textures/materials and XML-specific attachments; active ISD II prefabs contain no ISD I or obsolete art dependencies.
- Automated tests require an explicit request under the import guide. Saved-asset inspection and isolated renders completed; runtime combat is unverified.
- Source values and mapping reasons: [[Done/Features/ISDII_Import - Research|Source mapping and evidence]].

## Important Values

- Source `40,000/20,000` hull/shields; speed `3` source units; cost/build/population `25,000/80 s/24`.
- `24` weapons + shield + hangar + tractor → `27` unique targets. No source engine hardpoint; obsolete engine targets removed.
- Saved geometry bounds `101 × 49.5 × 180`; roots/banking scale `1`.
- Verify: `55` source hashes unchanged, `16` packed editable blends, `16` Blender/Unity geometry-UV-bone checks, four prefab/registration checks, eight team palettes.
- Maximum Blender geometry error `0.000157953` source units; maximum Unity error `0.000003026` project units; exact saved mount positions.
- Engine groups retain `1,428 / 1,840` triangles; `_Cull = 0`, emission intensity `2`, source blue/white textures, `RealtimeEmissive` flags.
- Engine settings survive material reimport. Final Unity compilation/import ready; no new Console errors. No automated tests or combat Play Mode run.

## Files

- `Tools/Blender/ISDIIReplacement/README.md` — current instructions and mappings.
- `Tools/Blender/ISDIIReplacement/ImportEvidence.json` — durable verification and obsolete records.
- `Assets/Art/Models/EmpireShips/ISDIIReplacement/`; old ISD II visual folders/snapshots removed.
- `output/ISDIIReplacement/` — screenshots, resolved source audit and editable blends.
- `Tools/Blender/ISDII/` — historical AOTR tooling, marked obsolete.

## Implementation History

- 2026-10-06 initial AOTR import: commit `f17fcd22`; 11 related tests passed; full EditMode `1,066/1,067`, unrelated Victory II binding failure.
- Original integration reused ISD I hull/triples, had 23 weapons/17 targets and then `21,000/18,000/25` balance.
- 2026-10-09 replacement: own Workshop 1770851727 visuals/source loadout, current balance preserved; saved-asset verification only.

- 2026-10-09 follow-up: restore two-sided/emissive engine materials, remove all 38 obsolete visuals/snapshots; commit `f0b035f9`. No retained Unity dependency or staged GUID gaps.
