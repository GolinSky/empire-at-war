---
type: plan
category: Bugs
status: in-progress
created: 2026-11-13
system:
  - ships
  - combat
  - balance
verification: "2026-11-13: roster audit and 22 description corrections
  source-verified. Home One follow-up refreshed, targeted reserialized, saved
  and read back in Edit Mode; no import/serialization errors; a Pipeline request
  timeout was recorded. Tests not run. Other plan scope remains open."
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
- Description corrections are implemented; firing-angle, attribution and balance work remains open. No gameplay data changes, tests or Play Mode runs were performed for this follow-up.
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

- Prior ship audit matching: CR70, Pelta, Carrack, Wavecrest, Recalcitrant. Squadron follow-up results below.
- Sources: `Assets/Settings/Data/Factions/Republic/RepublicFaction.asset` (Venator II :37, Resolute :121); `Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset`; matching `Assets/Settings/Data/Ship/<Faction>/*ShipData.asset`.
- Future prevention option: generate stat text from authoritative data; no presentation code changes in this audit.

- Follow-up audit 2026-11-13: all 16 Empire ships, 21 Rebellion ships, 20 squadron entries across four factions, Centax regeneration and all 8 shared defense entries inspected. Tables below preserve pre-fix claims.
- Mapping: faction ship key → `ShipType` → `ShipsData.GetShipDataPath` → GUID in `Assets/Settings/Data/Ship/Shared/ShipsData.asset` → asset `.meta`. Ship asset stems below resolve under `Assets/Settings/Data/Ship/<Faction>/`, with `.asset` suffix.
- Squadron mapping: `FactionDefinition.Squadrons` → `SquadronType` → `SquadronFactory.Create` (`squadronType + "SquadronData"`) → Addressables GUID → asset `.meta`. Squadron stems resolve under `Assets/Settings/Data/Squadron/`, with `SquadronData.asset` suffix.
- H/S/V = hull/shields/base speed; R = shield regeneration per second. Only numeric baseline claims changed; ability text, weapons and YAML wrapping preserved.

**Empire drift — corrected**

| Key / unit | Data asset stem | Description H / S / V | Data H / S / V |
|---|---|---|---|
| 202 Victory I — Advanced Loadout | `VictoryIShipData` | 12,000 / 8,000 / 175 | 6,000 / 4,000 / 16 |
| 201 ISD I | `ISDIShipData` | 11,000 / 9,000 / 18 | 12,000 / 10,000 / 18 |
| 203 Victory II — Advanced Loadout | `VictoryIIShipData` | 12,000 / 10,000 / 20 | 8,000 / 5,500 / 25 |
| 204 ArquitensImperialCruiser | `ArquitensImperialCruiserShipData` | 2,600 / 1,400 / 48 | 4,000 / 4,000 / 30 |
| 205 ISD II | `ISDIIShipData` | 12,500 / 10,000 / 18 | 16,000 / 12,000 / 18 |
| 208 Raider Corvette | `RaiderCorvetteShipData` | 600 / 800 / 35 | 600 / 600 / 35 |
| 209 AcclamatorAssault | `AcclamatorAssaultShipData` | 5,000 / 2,500 / 40 | 4,700 / 2,000 / 25 |
| 210 Tector Star Destroyer | `TectorShipData` | 12,000 / 9,000 / 16 | 15,000 / 10,000 / 16 |
| 213 Allegiance-class Battlecruiser | `AllegianceShipData` | 15,000 / 15,000 / — | 20,000 / 15,000 / — |
| 215 Tartan-class Patrol Cruiser | `TartanShipData` | 650 / 850 / 70 | 600 / 600 / 35 |
| 216 Gladiator-class Star Destroyer | `GladiatorShipData` | 6,000 / 5,000 / — | 9,000 / 8,000 / — |
| 217 Interdictor Star Destroyer | `InterdictorStarDestroyerShipData` | 20,000 / 16,000 / — | 10,000 / 10,000 / — |

- Gladiator's “capital shield regeneration” already matches `ShieldRegenTier.Capital = 3` → shared `capital: 180`; retained.

**Rebellion drift**

| Key / unit | Data asset stem | Description H / S | Data H / S | Action |
|---|---|---|---|---|
| 301 Corellian Corvette / CR90 | `CorellianCorvetteShipData` | 500 / 800 | 600 / 600 | Corrected |
| 321 Liberator Cruiser | `LiberatorShipData` | 4,800 / 4,200 | 7,500 / 8,400 | Corrected |
| 303 Home One / Admiral Ackbar | `HomeOneShipData` | 30,000 / 40,000 | 25,000 / 20,000 | Corrected by explicit user decision |

- Home One corrected 2026-11-13 by explicit user decision: description now uses base H25,000 / S20,000; table preserves the old claims. `FleetCommandService.Tick` skips `ReferenceEquals(entity, commander)` and preserves command ships' configured base stats.
- Home One verification: only two description numbers changed; YAML wrapping preserved. Refreshed, reserialized only `RebellionFaction.asset`, saved, and read the imported description back; asset clean and Editor in Edit Mode. No import/serialization errors; console also recorded a Pipeline request timeout. Tests not run; Play Mode not entered.

**Ships already matching**

| Faction/key / unit | Data asset stem | Description = data |
|---|---|---|
| Empire/207 Executor | `ExecutorShipData` | H150,000 / S200,000 / V10 |
| Empire/211 Imperial Venator | `ImperialVenatorShipData` | H9,000 / S10,000 |
| Rebellion/302 MC80 | `MonCalCruiserShipData` | H12,750 / S12,000 / R180 |
| Rebellion/323 MC80 Justice | `MC80JusticeShipData` | H12,000 / S14,000 |
| Rebellion/326 Mediator | `MediatorBattlecruiserShipData` | H35,000 / S40,000 |
| Rebellion/325 MC80b Remonda | `MC80bRemondaShipData` | H16,000 / S16,000 |

- MC80 R180 verified through tier 3 and shared `ShipShieldRegenTiersData.asset`, GUID `3425d6ddc79a6b649990974111258ac9`.

**Other ships — no numeric baseline claim to correct**

| Faction/key / unit | Data asset stem | Actual H / S / V | Relevant wording |
|---|---|---|---|
| Empire/212 Procursator | `ProcursatorShipData` | 8,000 / 7,000 / 20 | Ability reload speed |
| Empire/214 Titan | `TitanShipData` | 42,000 / 36,000 / 14 | None |
| Rebellion/300 Nebulon-B | `NebulonBShipData` | 3,000 / 3,000 / 30 | Shield recovery/speed ability |
| Rebellion/304 MC75 Profundity | `MC75ProfundityShipData` | 5,000 / 6,000 / 30 | None |
| Rebellion/305 MC80 Independence | `MC80IndependenceShipData` | 22,500 / 16,000 / 20 | Hull destruction; regen/movement ability |
| Rebellion/306 Corellian Battlecruiser | `CorellianBattlecruiserShipData` | 5,000 / 4,500 / 25 | Shield regen ability |
| Rebellion/307 MC30c | `MC30cShipData` | 4,000 / 3,500 / 30 | “Fast” |
| Rebellion/308 CD-273 | `CD273ShipData` | 1,600 / 1,000 / 26 | “fast” |
| Rebellion/320 Quasar Fire | `QuasarFireShipData` | 2,000 / 2,000 / 30 | None |
| Rebellion/311 MC50 | `MC50ShipData` | 4,200 / 4,000 / 25 | Speed ability |
| Rebellion/309 Assault Frigate Mk II | `AssaultFrigateMkIIShipData` | 3,500 / 4,000 / 25 | Regen/speed ability |
| Rebellion/310 Dauntless | `DauntlessShipData` | 5,000 / 6,000 / 20 | None |
| Rebellion/330 Nebula | `NebulaShipData` | 12,000 / 12,000 / 20 | Regen-disable ability |
| Rebellion/312 Bothan Assault Cruiser | `BothanAssaultCruiserShipData` | 5,000 / 5,000 / 25 | None |
| Rebellion/322 Endurance | `EnduranceShipData` | 15,000 / 14,000 / 18 | Regen/speed ability |
| Rebellion/324 MC80 Freedom | `MC80FreedomShipData` | 16,000 / 18,000 / 20 | Regen ability |

**Squadrons — all 17 numeric baseline descriptions already match**

- H/S are per member, explicitly “each fighter/bomber”; Boba Fett is one craft. No whole-squadron-total mismatch. C/T = cruise/combat speed; actual R/C/T included even where the description does not quote them.

| Faction/key / unit | Data stem | Description claims | Actual H / S; R; C / T |
|---|---|---|---|
| Republic/2 ARC-170 | `ARC170` | H80 / S30 | 80 / 30; 3; 69 / 78 |
| Republic/3 BTL-B Y-Wing | `YWing` | H62 / S24 | 62 / 24; 2; 34 / 39 |
| Republic/4 NTB-630 | `NTB630` | H60 / S22 | 60 / 22; 2; 34 / 39 |
| Republic/5 V-19 Torrent | `V19Torrent` | H72 / S25 | 72 / 25; 2; 71 / 81 |
| Republic/6 V-Wing | `VWing` | H62 / S22; “fast” | 62 / 22; 2; 76 / 84 |
| Separatist/100 Belbullab-22 | `Belbullab22` | H38 / S5 | 38 / 5; 1; 62 / 70 |
| Separatist/102 Vulture | `Vulture` | H28 / no shields | 28 / 0; 0; 54 / 60 |
| Empire/200 TIE Fighter | `TIEFighter` | H42 / no shields | 42 / 0; 0; 56 / 63 |
| Empire/201 TIE Bomber | `TIEBomber` | H45 / no shields | 45 / 0; 0; 31 / 36 |
| Empire/202 TIE Avenger | `TIEAvenger` | H44 / S10 | 44 / 10; 1; 67 / 75 |
| Empire/203 TIE Interceptor | `TIEInterceptor` | H32; “fast” | 32 / 0; 0; 70 / 78 |
| Empire/204 TIE Brute | `TIEBrute` | H52 | 52 / 0; 0; 55 / 62 |
| Empire/205 TIE Punisher | `TIEPunisher` | H48 / S12 | 48 / 12; 1; 31 / 36 |
| Empire/206 Boba Fett / Slave I | `BobaFettSlaveI` | H2460 / S1440 | 2460 / 1440; 4.8; 70 / 75 |
| Rebellion/1 A-Wing | `AWing` | H85 / S40; “fast” | 85 / 40; 6; 83 / 94 |
| Rebellion/300 T-65 X-Wing | `XWing` | H95 / S50 / R4 | 95 / 50; 4; 75 / 86 |
| Rebellion/301 BTL-A4 Y-Wing | `YWingBomber` | H70 / S40 / tactical speed 44 | 70 / 40; 3; 38 / 44 |

- BTL-A4 tactical speed 44 matches `CombatSpeed`, not `CruiseSpeed` 38; retained. X-Wing R4 matches `ShieldRegenerateValue`.
- Ability modifiers retained/out of scope: TIE Avenger speed −25%; X-Wing S-Foils speed ×1.3 and shield regen ×3.

| Faction/key / unit | Data stem | Description wording | Actual H / S; R; C / T |
|---|---|---|---|
| Separatist/101 Droid Bomber | `DroidBomber` | “slow”; no baseline number | 32 / 5; 0.5; 29 / 33 |
| Separatist/103 Jango Fett / Slave I | `JangoFettSlaveI` | “reinforced hull”; no baseline number | 3000 / 1200; 2.5; 70 / 75 |
| Rebellion/302 Millennium Falcon | `HanSoloMillenniumFalcon` | “Falcon's speed”; no baseline number | 2130 / 2220; 2; 60 / 65 |

- `Delta7SquadronData.asset` has no entry in any faction roster (key 0 absent), so no description exists to change.

**Centax — corrected**

| Unit/key | Description | Data |
|---|---|---|
| Republic/16 Centax | H2,000 / S1,300; R62 | H2,000 / S1,300; R25 |

- `CentaxShipData.asset` → `shieldRegenTiers` GUID `3425d6ddc79a6b649990974111258ac9` → `ShipShieldRegenTiersData.asset`; tier 1 = `Frigate`; `frigate: 25`.
- `ShipData.ShieldRegenerateValue` calls `GetRegenerateValue(ShieldRegenTier)`. Changed only 62 → 25; prior hull/shield correction retained.

**Shared defenses — fully audited**

- Catalog key → `DefendPlatformType` → `DefendPlatformAssets.GetDataName` → `DefendPlatformFactory.Create` → `AssetMappingData.asset` GUID → exact data `.meta`.
- Data stems below resolve under `Assets/Settings/Data/Models/DefendPlatform/`, with `.asset` suffix; H/S live in `healthData`.

| Key / unit | Data asset stem | Description H / S | Data H / S | Result |
|---|---|---|---|---|
| 2 Golan III | `AotrGolanIIIDefensePlatformData` | 10,000 / 12,000 | 10,000 / 20,000 | Corrected shields |
| 3 XQ-3 Empress | `AotrEmpressDefensePlatformData` | 6,000 / 24,000 | 6,000 / 24,000 | Match |
| 4 QuadLaser Satellite | `QuadLaserSatelliteData` | 1,000 / 1,000 | 3,000 / 5,000 | Corrected |
| 5 Turbolaser Satellite | `TurbolaserSatelliteData` | 1,000 / 1,000 | 3,000 / 5,000 | Corrected |
| 6 Ion Satellite | `IonSatelliteData` | 1,000 / 1,000 | 3,000 / 5,000 | Corrected |
| 7 MassDriver Satellite | `MassDriverSatelliteData` | 1,000 / 1,000 | 3,000 / 5,000 | Corrected |
| 8 Missile Satellite | `MissileSatelliteData` | 1,000 / 1,000 | 3,000 / 5,000 | Corrected |
| 0 XQ-6 | `DefendPlatformData` | No numeric claim | 3,000 / 1,500 | No edit |

- Verification: 21 descriptions corrected across four assets; only numbers changed and line wrapping retained. Confirmed Editor outside Play Mode; ran `AssetDatabase.Refresh()`, `ForceReserializeAssets` with exactly the four changed paths, then `SaveAssets()`. No new console errors after baseline cursor 578; one older Pipeline request timeout remains in console history. Tests and Play Mode not run.
- Plan remains active. Backlog entry read; `TODOs.md` deliberately left untouched under this request's explicit working-tree exclusion.


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
8. [x] Correct roster description drift (8 units listed above). Done 2026-11-13: hull/shield text in `RepublicFaction.asset` / `SeparatistFaction.asset` now matches ShipData; assets refreshed, reserialized and saved, no console errors. Follow-up 2026-11-13: 12 Empire ships, CR90, Liberator, Centax regeneration and six shared defenses corrected; all 20 squadron entries audited, 17 numeric baselines already match. Home One follow-up 2026-11-13: corrected to base 25,000 hull / 20,000 shields by explicit user decision; targeted persistence and imported-description readback verified. Remaining: none for this roster-description audit. Other plan items remain open.
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
