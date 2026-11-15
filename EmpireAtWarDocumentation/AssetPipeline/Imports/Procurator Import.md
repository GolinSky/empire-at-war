---
type: integration-record
verified: 2026-11-15
---
# Procurator-class Battlecruiser

## Source

- Fall of the Republic Workshop `1976399102` → `Procurator-class_battlecruiser.alo` and `Procurator-class_battlecruiser_D.alo`.
- `Data/XML/Units/Republic_Space_Units.xml` and `Data/Text/FotR_text.txt` agree on hull **13,020**, shields **9,000**, and source shield refresh **22.5**.
- Live hardpoint definitions give **8 medium dual turbolasers, 14 light turbolasers, 6 proton launchers, 2 engines, 1 hangar**. Both `Turbo17` and `Turbo18` have separate definitions and sockets.
- [[GameDesign/References/Secutor, Procurator and Praetor - Workshop Reference]] compares the three source hulls.

## Implementation

- Republic `ShipType.Procurator = 18` · station level **5** · **10,000** credits · **90 s** · **12** capacity · maximum **2**. Economy values are project tuning.
- Heavy Capital · project speed **12 units/s** · turn **3°/s** · bank **5°** · Capital shield regeneration **180/s**. Lowest height tier, length **260**, navigation radius **133**.
- Shared weapon profiles and single projectile origins at the midpoint of the authored muzzle pair. Hardpoint IDs **0–30**, source yaw cones, project hull-based hardpoint health.
- One destroyable hangar. ARC-170 and V-wing each have **1 active / 2 replacements**. Launch delays **4 s** initially and **15 s** between launches.
- Ship, placement, and static authored wreck views in `RepublicUnitViews`. Intact hull alpha supplies the team-color mask. Wreck uses the project's procedural breakup effects.

## Verification

- Source-to-FBX comparison preserved UVs and measured maximum position error **0.000218** source units. Two wreck material slots had normal deviation up to **0.58°**.
- Unity readback checked **31** injected hardpoints, **28** weapon mounts, hangar clearance, fitted shield and placement, **5** opaque wreck meshes, icon/roster routes, and Addressables ownership.
- Inspected the icon, team-color renders, wreck, and placement hologram. Automated tests and Play Mode were not run.
