---
type: plan
category: Bugs
status: todo
created: 2026-11-13
system:
  - ships
  - combat
  - balance
verification: Findings carried from November 13 telemetry and source/asset
  audit; plan created and links verified; no fixes or tests.
tags:
  - flag/issue
---
# Ship Hardpoint and Balance Plan

## Goal

- Resolve firing-angle inconsistencies and investigate low ship output before compensating with damage buffs.
- Review ship cost/durability and telemetry attribution using the November 13 battle and verified serialized settings.
- Own ship follow-ups separately from [[TODOs/Bugs/Enemy_AI_And_Faction_Balance]]; squadron tuning remains with [[TODOs/Bugs/Carrier_and_Squadron_Rebalance]].

## Decision

- Priority 1: consistent wrapped-yaw handling for Peleset/Recalcitrant; priority 2: Recusant and narrow-arc layouts; priority 3: trustworthy attribution and ship-value review.
- Confirmed: numeric yaw-contract mismatch, restrictive serialized sectors, shared-prefab attribution and stale roster description stats (Venator II plus seven others).
- Unresolved: intended mount sectors, arc-caused battle damage loss, correct balance changes. Narrow broadsides alone are not defects.
- This is a new implementation plan; diagnosis exists, fixes are unimplemented. No gameplay edits or tests were performed for this plan.
- Flag issue: unresolved firing/attribution defects and balance concerns; remove after fixes or documented dismissal with evidence.

## Important Values

- Battle: `Battle_20261113_110922.csv`, 1,851 unscaled seconds; Player0 Separatists, Player1 Republic AI. No final winner established.
- Weapon-recorded ship losses: Republic 112 / 500,900 credits; Separatists 124 / 653,200 credits. Republic destroys 1.304 credits per credit lost.
- Direct ship weapon damage: Republic 3,524,218; Separatists 2,795,724. Carrier-launched squadron damage is separate.
- Raw DPS = `damage × shotsPerSalvo / max(reload, (shotsPerSalvo − 1) × shotInterval)`; reload starts at salvo start.
- Ship dual/quad damage factors are 1.6/2.4, not barrel count. Raw estimates exclude accuracy, target matrix, upgrades, abilities, travel, aiming and destroyed mounts.
- `activeSeconds` spans first-to-last shots for an aggregated type; it is not summed per-ship firing exposure. Kills exclude deaths outside instrumented weapon impacts.

## Implementation

### Recusant — restrictive hardpoint layout confirmed

- Serialized audit: 10 weapon mounts match `RecusantShipData` loadout. Raw sum 264.872 DPS includes 51.2 strikecraft-only point defense; non-PD sum = 213.672 DPS.
- Heavy dual: 54.912 DPS, yaw −42°…43°, range 585. Medium quad: 38.88 DPS, yaw −90°…90°, range 517.5.
- Medium single: 16.2 DPS, yaw −159°…−121°; medium dual: 25.92 DPS, yaw −158°…−124°. Both rear-left, range 517.5; no distant-bearing overlap with forward heavy.
- Four medium dual lasers: 19.44 DPS each, two yaw −160°…−20°, two 20°…160°, range 345. Two PD mounts: 25.6 DPS each, same side arcs, range 200.
- Common distant-bearing maximum non-PD = 132.672 DPS (62.1% of non-PD sum); straight ahead = 93.792 DPS. Near-hull parallax varies; research/ability bonuses excluded.
- Confirmed constraint, not confirmed accidental authoring: intended broadside geometry and actual battle contribution remain unresolved. Do not treat summed loadout DPS as simultaneously available single-target DPS.
- Naming mismatch: single medium mount is named `DualMediumTurbolaserM1`; inspect before using hierarchy labels to infer weapon type.
- Source: `Assets/Prefabs/Models/Ships/Separatist/View/RecusantShipView.prefab:1845,1956,2077,2193,2309,2430,2551,2781,2892,3111` (all 10 hardpoint prefab instances); `Assets/Settings/Data/Ship/Separatist/RecusantShipData.asset:42`.
- Next: compare intended mount facing/coverage with serialized arcs and observed combat bearings; audit similarly weak ships before applying stat compensation.

### Confirmed contract mismatch — yaw bounds outside ±180°

- `WeaponTargetSelector.cs:21–23` normalizes target yaw to −180°…180° and applies literal min/max bounds. `WeaponFacingSolver.AddCoverage` wraps arc width modulo 360°; facing can value arc portions the target gate cannot admit.
- Peleset `HeavyDualTurbo3`, direct component at `Assets/Prefabs/Models/Ships/Separatist/View/PelesetShipView.prefab:2091`: bounds 115°…245° store 130° coverage; normalized target gate can admit only 115°…180° = 65°.
- Recalcitrant missile type 93, HP05/HP06 direct components at `Assets/Prefabs/Models/Ships/Separatist/View/RecalcitrantShipView.prefab:2217,2275`: bounds −236.10281°…−106.10281° / 106.102844°…236.10284° store 130° each; normalized gate admits approximately 73.897° each.
- Batched path matches: `WeaponTargetSelectionJob` also derives yaw in −180°…180° and compares literal bounds.
- Editor gizmos (`WeaponHardPoint.cs:246`, `HardPointSceneDrawer.cs:89`) clamp bounds to ±180°, so the scene view shows the clipped arc, not the stored 130°.
- Full prefab scan 2026-11-13, including inherited `Vfx/Combat/WeaponHardPoint.prefab` instances: only these three mounts exceed ±180°. Station/platform −360°…360° mounts are full circles and unaffected.
- Clipping follows directly from numeric bounds regardless of parent orientation. This is a source/serialized-data contract mismatch; actual lost battle damage has not been runtime-reproduced.
- Proposed: choose one consistent representation for wrapped intervals across asset authoring, target eligibility and facing coverage. Do not merely widen arcs without confirming intended sectors.
- Source files: `Assets/Scripts/Components/Weapon/WeaponTargetSelector.cs`; `Assets/Scripts/Components/Weapon/WeaponFacingSolver.cs`.

### Narrow-arc candidates — intent not established

- Resolute LaserBeam type 12: yaw −54°…−33° = 21°; `Assets/Prefabs/Models/Ships/Republic/View/ResoluteShipView.prefab:2934`.
- Arquitens heavy dual missile type 128: yaw 120°…148° = 28°, rear-right; `Assets/Prefabs/Models/Ships/Republic/View/ArquitensShipView.prefab:11438`.
- Dispatcher medium dual lasers type 94: yaw 163°…180° / −180°…−162° = 17° / 18°, stern; `Assets/Prefabs/Models/Ships/Separatist/View/DispatcherShipView.prefab:6787,7143`.
- Parent orientation checked in serialized audit. Narrow arcs may be intentional; compare intended firing sectors and combat exposure before calling them defects.
- Battle effective damage by weapon family: Dispatcher lasers 423 versus turbolasers 43,334 and ion 16,418; Arquitens missiles 15,057; Recalcitrant missiles 13,840; shared Resolute/Venator II beam row 56,257.
- Peleset turbolaser total 194,021 cannot be attributed to the restricted mount alone. Family totals establish output, not arc-caused loss; preserve shared-prefab and exposure caveats.


### Mandator — cost/durability investigation

- Base hull 75,000 + shields 100,000; price 12,000 credits. Invincible: 22,500 + 24,000 / 20,000; Malevolence: 50,130 + 30,000 / 20,000.
- Base durability/credit: Mandator 6.27× Invincible and 3.64× Malevolence. This ignores hardpoint hull multipliers, regeneration, abilities and matchup differences.
- Restrictions: max one active Mandator, 25 population, 110 s build. Two deployed/lost in the recording; 674,843 direct damage = 19.1% of Republic ship damage.
- Next: compare cost, population, build time, effective durability and useful damage together; do not select a nerf percentage from this single battle.
- Sources: `Assets/Settings/Data/Ship/Republic/MandatorShipData.asset:31`; `Assets/Settings/Data/Factions/Republic/RepublicFaction.asset:149–153` (Mandator limit/price/build/population); `Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset:182` (Invincible price); corresponding Invincible/Malevolence ShipData.

### Battle output and attribution

- Recusant: 31,337 direct damage / seven recorded losses; Munificent: 138,076 / 16. This identifies an investigation target, not controlled per-unit DPS.
- Providence: 887,412 direct damage; Bulwark: 535,474. Separatist loss costs: Invincible 120,000, Providence 96,000, Bulwark 90,000; together 46.8% of recorded ship value lost.
- ResoluteShipView row pools 791,104 damage from Resolute and Venator II. Mapping keys share prefab GUID `d9d6eb346b0497649911724f20048daf`; telemetry keys shooters by view Transform name.
- Sources: `Assets/Settings/AssetMappingData.asset:514–523`; `Assets/Scripts/Components/Weapon/WeaponComponent.cs:165,247`.
- Proposed: retain stable attacking unit type/entity identity in damage telemetry; preserve prefab name only as additional context. Recheck all shared-prefab types.
- Track carrier-launched damage separately and link it to its carrier before judging carrier value; free hangar losses are not paid replacement losses.

### Roster descriptions — stale displayed stats

- Fixed 2026-11-13: descriptions now use the ShipData values. Table kept as the pre-fix record.
- Faction `description` hull/shield text vs `<Hull>`/`<Shields>` in ShipData (audit 2026-11-13):

| Unit | Description | ShipData |
|---|---|---|
| Venator II | 7,000 / 5,000 | 9,000 / 10,000 |
| Resolute (hero) | 7,000 / 5,000 | 10,500 / 10,000 |
| IPV-2C Stealth Corvette | 400 / 500 | 850 / 900 |
| Centax | 2,000 / 2,000 | 2,000 / 1,300 |
| Invincible | 10,000 / 6,000 | 22,500 / 24,000 |
| Bulwark Mk1 | 8,500 / 2,500 | 12,750 / 8,000 |
| Peleset | 7,000 / 3,000 | 5,000 / 4,500 |
| C-9979 | 500 / 600 | 600 / 600 |

- Matching: CR70, Pelta, Carrack, Wavecrest, Recalcitrant. Squadron descriptions not audited.
- Sources: `Assets/Settings/Data/Factions/Republic/RepublicFaction.asset` (Venator II :37, Resolute :121); `Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset`; matching `Assets/Settings/Data/Ship/<Faction>/*ShipData.asset`.
- Next: align descriptions with authoritative data; prefer generating stat text from ShipData over hand-written numbers.

### Dismissed or limited suspicions

- Wavecrest: additional directly serialized positive-side lasers complete the mirrored set; earlier same-side-only suspicion was disproved.
- Providence: heavy quad arcs −100°…−75° / 75°…100° are narrow broadsides, but high observed output does not support calling the ship defective from arcs alone.
- Recusant hull-facing solver already chooses DPS-weighted arcs from currently in-range non-PD weapons. Do not assume it always points its nose at targets.
- Raw maximum overlap is a distant/common-bearing estimate; mount offsets change exact near-target overlap. Ship radar range is not each weapon's firing range.

## TODO

1. [ ] Revalidate affected prefab bindings, inherited transforms, unlocked mounts, range multipliers and loadouts against current assets before implementation.
2. [ ] Choose one wrapped-yaw representation; make target eligibility, facing solver, turret aiming and authoring agree at ±180° and across the seam.
3. [ ] Correct Peleset/Recalcitrant handling without widening intended physical sectors; verify same target is admitted consistently by serial/batched targeting and facing.
4. [ ] Confirm intended Recusant rear-left versus forward sectors; resolve single/dual naming discrepancy and verify any corrected loadout matches the prefab.
5. [ ] Review Dispatcher, Resolute/Venator II and Arquitens narrow arcs against model placement and intended weapon roles; record fix or justified retention for each.
6. [ ] Separate shared-prefab attacker identity; establish per-unit/mount firing exposure and blocking reasons needed to measure arc effects.
7. [ ] Review Mandator economics and Recusant output after arc/attribution corrections; account for carrier output, support, upgrades and population restrictions.
8. [x] Correct roster description drift (8 units listed above). Done 2026-11-13: hull/shield text in `RepublicFaction.asset` / `SeparatistFaction.asset` now matches ShipData; assets refreshed, reserialized and saved, no console errors. Remaining: squadron, Empire and Rebellion descriptions not audited; Centax shield-regen text unchecked.
9. [ ] Record source/non-test validation, applicable user acceptance gates and reusable guidance before closure.

## Rules

- Relevant verification categories for future changes: Combat, Ships, Balance; EnemyAi if combat valuation changes; Ui if presentation code changes.
- Local test execution remains disabled unless explicitly requested. Do not run tests or test preparation as part of this plan's creation.
- Future source checks: matching prefab/loadout counts; valid intended arc coverage; boundary/wrap consistency; raw DPS arithmetic; stable telemetry attribution; scoped diff and compilation diagnostics.
- Future Unity asset edits require targeted refresh/reserialization/save and console checks under repository policy. No manual Editor save handoff.
- Before future UI changes read `UI_UX_GUIDELINES`; before asset imports read applicable pipeline guidance. This plan creates no project assets.
- Runtime acceptance: observe seam-crossing targets, front/side/rear engagement and comparable ship encounters. Do not claim these checks passed from static inspection.
- Closure: resolve or explicitly dismiss every scoped item; update canonical combat/telemetry guidance, backlog and user acceptance queue under repository lifecycle rules.

## Files

- Prefab root: `Assets/Prefabs/Models/Ships/`; ship data: `Assets/Settings/Data/Ship/`.
- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset` — family, caliber and mount factors.
- `Assets/Scripts/Components/AttackComponent/WeaponProfile.cs:138`; `WeaponProfileFactory.cs:12` — profile DPS.
- `Assets/Scripts/Components/Weapon/WeaponTargetSelector.cs:21`; `WeaponTargetSelectionJob.cs:120`; `WeaponFacingSolver.cs`; `WeaponComponent.cs:361` — eligibility/facing.
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs:154–162` — runtime salvo timing; reload timer starts with the salvo.
- Telemetry: `C:/Users/golin/AppData/LocalLow/GolinSkyCompany/EmpireAtWar/BattleTelemetry/`; damage file above plus `Loading_20261113_110752_ai.csv` and `Loading_20261113_110752_ai_orders.csv`.
- [[TODOs/Bugs/Enemy_AI_And_Faction_Balance - Battle Review 2026-11-13]] — dated AI battle context.
- [[Tooling/Battle_Telemetry]] — schemas and limitations.
