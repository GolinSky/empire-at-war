---
type: reference
updated: 2026-10-07
tags:
  - blender
  - unity
  - alo
  - executor
---
# Executor Model Import

## Files

- Guide: [[Architecture/ALO_MODEL_IMPORT_GUIDE]]. Rebuild scripts and evidence: `Tools/Blender/Executor/README.md`, `ImportEvidence.json`.
- Source: AOTR Workshop `1397421866`, `E_Executor_Star_Destroyer`, `Empire_Executor_Super_Star_Destroyer.ALO` + ten `HTL_01..10` turrets.
- `Temp/ALO_MODEL_MAP.txt` absent → verified `output/aotr-empire-units/ship-catalog.csv` and source XML.
- XML: `Data/XML/SpaceUnitsSupers.xml`, `Hardpoints_Supers.xml`; source models/textures/XML unchanged (`27` SHA-256 checks).
- Main assets: `ExecutorShipView.prefab`, `ExecutorShipData.asset`, `ExecutorReinforcementView.prefab`, `ExecutorWreckView.prefab`, `ExecutorIcon.png`.

## Implementation

- Commit `1b8e96a1`: Executor assets/registrations and shared multi-hangar/multi-engine damage support.
- Empire `ShipType.Executor = 207`; hull `200000`, shields `200000`, speed `15`; Laser Beam + Tractor Beam.
- Target weapons: `10` heavy 2-burst LR turbolasers; `23` heavy 2-burst dual turbolasers; `8` heavy 2-burst LR dual turbo-ions; `10` heavy 8-burst artillery rockets.
- Integrated weapons: `69` medium 2-burst LR dual turbolasers, `40` heavy lasers, `14` repeating PD mounts. Explicit user request → non-targetable.
- `174` weapons; `59` targets (`51` weapons + `4` generators + `2` engines + `2` hangars); `182` unique hardpoint IDs.
- Weapon profiles `49..54` isolate required bursts from existing ships. Heavy lasers reuse `28`.
- Split source dual laser/PD A/B bones to requested mount counts; repair mapping `WTFP_8/9 → WTFP_08/09` for `HP_EX_RPD_04`.
- Generator positions `SG_01..04`; hangars `Spawn_00/01`; two engine targets placed across engine bounds.
- Shared hangar code disables individual bays; legacy ships stop reserve launches after all hangars are lost. Shared ship code tracks every engine.

## Important Values

- Gameplay size `646.230 × 140.862 × 1900` units; bank `±3°`; banked hull Y `-71.311 .. 70.539`; navigation radius `1009`.
- Source economy retained: price `125000`, build `2500 s`, population `40`, maximum `1`. Availability level `5` is a project choice.
- Weapon health `2500`; generator/engine/hangar health `4000`. Weapon normalization and new system health are project choices.
- Added TIE Fighter/TIE Bomber bays: reserves `6/7`, active slots `1` each, delays `7.5 s`; exits `8` units below banked hull bottom. Fighter bay and reserve interpretation are project choices.
- Source alpha mask would recolor almost all hull → mask/livery strength `0`; team rim strength `0.3`. Eight owned palette renders verified.

## Decision

- Preserve source geometry, UVs, bones and authored visibility in FBX; remove unused helper meshes only from game prefabs.
- Conversion: `11` models, `377` meshes, `450857` triangles, `579` bones. Identity Root restored after verifying source.
- Prefabs: removed `39` unused helpers / `207113` triangles; retained gameplay ShieldSurface.
- Unity maximum attachment error `0.0000228646` units before gameplay scaling; parent checks pass; preview size matches; no broken serialized references.

## Edge Cases

- Final related tests: `17/17` passed (Executor assets `8`, engine damage `2`, hangar model `7`).
- Full Editor run `1086/1090`; after import fixes, project Editor run `565/567`. Remaining failures: Acclamator team renderer list and unused helper meshes; outside Executor scope.
- PlayMode suite `263/264`; remaining Zenject scene fixture lacks `TestSceneContextEvents` in build settings. No Executor battle simulation recorded.
- Static import only; original EaW animations, proxy particles and custom shader effects not recreated. Turning, scale, regeneration delay and donor presentation remain provisional balance.
