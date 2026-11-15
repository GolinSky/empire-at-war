---
type: integration-record
verified: 2026-11-15
---
# Secutor-class Star Destroyer

## Source

- Republic `Secutor_Rep` from Clone Wars Holdout Factions (Workshop `2794270450`) → `Neutral_Republic_Secutor.ALO`.
- Remake 4.0 (Workshop `1770851727`) supplies source textures and 32 separate turret models named by the Republic hardpoint XML.
- [[GameDesign/References/Secutor, Procurator and Praetor - Workshop Reference]] compares the Imperial Secutor preview with the Procurator and Praetor.

## Implementation

- Republic reinforcement, station level 5 · 10,000 credits · 85 s · 18 capacity · maximum 3. Cost, build time, capacity and limit are project tuning.
- Heavy capital · source hull 43,100 and shields 30,000 · project speed 13, turn 3°/s, bank 5°, Capital shield regeneration 180 per 1 s tick.
- 40 weapon banks · 24 DBY-827, 5 heavy dual turbolaser, 6 heavy dual laser, 3 heavy dual ion, 2 heavy proton torpedo. Shared weapon profiles and project firing arcs use each bank's primary muzzle.
- 46 health targets · 40 weapons, 2 shields, 2 engines, 2 hangars. ARC-170 8 total / 2 active and Y-wing 4 total / 1 active. No active ability.
- Living, placement and wreck views use `RepublicUnitViews`. The wreck uses living hull geometry and project wreck effects. Static turret models and engine glow retain the source sockets and surfaces.
- Hull length 280 · width 232.04 · height 47.41 · navigation radius 180 · banked Y −24.64 to 24.25.

## Verification

- Source-to-FBX round trip checked 58,628 visible triangles, material slots, UVs, normals and attachment matrices. Maximum attachment matrix difference 0.000245 source units.
- Unity saved and resolved faction, preview, wreck, data, icon and Addressables references. Verified 40 weapons, 46 health targets and two bays. Rendered the icon and all 16 living team colors.
- Source helpers and source animations are omitted from the Unity conversion. Play Mode and automated tests were not run.
