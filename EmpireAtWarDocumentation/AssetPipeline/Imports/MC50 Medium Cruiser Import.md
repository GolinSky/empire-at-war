---
type: integration-record
updated: 2026-10-12
tags:
  - alo
  - blender
  - unity
  - rebellion
system:
  - asset-pipeline
  - units
record_state: historical
reviewed: 2026-10-20
---
# MC50 Medium Cruiser Import

> [!info] Import record
> Integration and verification evidence recorded at the dates below. Gameplay values are snapshots and may differ from current assets. Current rules/data ownership: [[GameDesign/Units_And_Factions]], [[GameDesign/Combat]] and [[GameDesign/Economy]]. Open acceptance work remains in its linked TODO plan.
> Plan: none — historical provenance only; no tracked open work (2026-10-29 review).

## Decision

- Source: Remake Workshop `1770851727`, `Rebel_Mon_Calamari_MC_50.alo`; textures: `Data/Art/Textures`.
- Gameplay balance: [[GameDesign/Proposals/Rebel units]], MC50 section. Remake XML supplies geometry provenance and sockets, not the requested balance.
- Rebellion `ShipType.MC50 = 311`; `ShipAbilityId.MC50HitAndRun = 40`; class `Cruiser = 8`.
- No hangar or squadron complement; source hangar art remains visible.

## Important Values

- Hull `4,200`; shields `3,000`; speed `27 units/s`; price `3,400`; build `42 s`.
- Weapons: `2` heavy turbolasers + `4` medium dual turbolasers + `2` medium ion cannons + `4` lasers + `2` point-defense lasers.
- `14` weapons + engine + shield = `16` targetable hardpoints; IDs `0..15`; health/fog collections include every weapon.
- Hit and Run: movement ×`1.25` for `8 s`; recovery `35 s` after expiry. Existing engine-boost settings; other multipliers `1`.
- Provisional: level `2`, population `4`, limit `3`, range `600`, navigation radius `65`, length `110`, banking ±`5°`, arcs ±`85°`.
- Provisional hardpoint health: weapon `300`, engine `700`, shield `600`; shield regeneration `20` every `3 s`.
- Living bounds: `41.470 × 19.965 × 110.000`; opaque preview/wreck length `109.348`; bank envelope `−10.016 .. 9.983`.

## Implementation

- Separate Blender `3.6.23` MCP process, port `9891`, avoids concurrent shared-scene imports; safe mode retained, telemetry disabled.
- Source binary materials audited and slots repaired before FBX transport; FBX/blend retain all helpers. Game prefabs remove authored shadow/collision/shield geometry.
- Requested mounts use actual source firing/damage/effect sockets. Mapping is a project choice; source XML has five gun groups.
- `EmpireAtWar/Ship Lit` opaque surfaces; separate additive engine/window/hangar effects. Normal maps use linear import and flipped green channel.
- Broad inverse-alpha hull mask uses strength `0.25`; team rim `0.6`; matching wreck settings preserve neutral panel detail.
- Own gameplay/shield/wreck/data/placement/icon/matchups; Rebellion roster, ship/HUD/tooltip icon mappings, data/view Addressables and ability/audio registrations saved.

## Verification

- Blender: geometry/UV/hierarchy round trip passed; geometry error ≤`0.00004645`, bone displacement ≤`0.00012642` source units.
- Unity: `10` meshes, `35,592` triangles, `51` bones; attachment error ≤`0.000000563` units.
- `2,296` saved-asset checks passed; source material slots/triangles/UVs, all four prefabs, socket positions, stats, targetability and registrations verified.
- `16` input hashes unchanged; `11` texture references lossless; `50` asset/metadata pairs verified.
- Transparent `512 × 512` icon inspected; alpha crop `(69,128)..(438,354)`. Eight live/wreck palettes verified distinct; placement/wreck renders inspected.
- No automated Unity tests or combat Play Mode run by this import.

## Edge Cases

- EaW animated/refraction shader effects are approximated by static additive geometry; ALA animations are not ported.
- Runtime firing arcs, hardpoint destruction, ability timing, fog/selection, reinforcement and wreck lifecycle remain unverified.

## Files

- `Tools/Blender/MC50/README.md` — workflow, choices and limitations.
- `Tools/Blender/MC50/ImportEvidence.json` — saved verification evidence.
- `output/remake-rebel-ships/MC50-Converted/` — editable blend, FBX, textures, source audit, hardpoint mapping and previews.
- `Assets/Prefabs/Models/Ships/MC50ShipView.prefab` — gameplay prefab.
- `Assets/Settings/Data/Ship/MC50ShipData.asset` — gameplay data.
