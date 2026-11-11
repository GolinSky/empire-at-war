---
type: external-game-reference
system:
  - ships
  - combat
source_version: 2.11.9
verified: 2026-11-11
verification: Installed AotR XML and English tooltips inspected. Hull lengths
  measured from Blender meshes using XML scale factors. Lore references and
  estimates are labelled separately.
---
# Awakening of the Rebellion Capital Ship Reference

## Files

- [Open AotR Capital Ships.blend](file:///C:/Users/golin/.codex/visualizations/2026/11/10/01a1c6a2-3f12-75a0-ad9f-f7c5907060f3/AotR%20Capital%20Ships.blend) · Seven ships at AotR scale with packed textures.
- Blender file path `C:/Users/golin/.codex/visualizations/2026/11/10/01a1c6a2-3f12-75a0-ad9f-f7c5907060f3/AotR Capital Ships.blend`
- Installed mod: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data` · Steam Workshop `1397421866` · AotR `2.11.9`.
- Unit data: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/XML/SPACEUNITSCAPITAL.XML` and `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/XML/SPACEUNITSSUPERS.XML`.
- Hardpoint attachments: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/XML/Hardpoints_Empire_Space.xml`.
- English tooltips: `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/Text/TranslationManifest.xml`.

| Ship | AotR unit ID | Source model |
|---|---|---|
| MC80 Justice | `R_MC80_Justice` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/MC80_Justice.ALO` |
| MC80 Freedom | `R_MC80_Freedom` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/RV_MC80_Freedom.ALO` |
| MC80 Liberty | `R_MC80_Liberty` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/RV_MC80_Liberty.ALO` |
| MC80a Alliance | `R_MC80a_Alliance` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/RV_MC80_Alliance.ALO` |
| MC80b Remonda | `R_MC80b` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/MC80c.ALO` |
| Mediator Battlecruiser | `R_Mediator_Battlecruiser` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/Mediator.ALO` |
| Interdictor Star Destroyer | `E_Dominator_Star_Destroyer` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/Empire_Imperial_SD.ALO` |
| Interdictor gravity-well assembly | `HP_Dominator_Parts` | `D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/Empire_Dominator_SD.ALO` |

## Lengths

- Bow-to-stern hull mesh extent in Blender, with AotR's `Scale_Factor` applied. The preview includes the Interdictor's Dominator assembly and uses simplified materials. Turret attachments, effects, and collision geometry are outside the hull measurement.
- User's reference: Liberty `1,200 m` ≙ `954.815 mod units` → `0.795679 mod units/m`.
- Liberty-equivalent values are estimates calculated from model proportions. Lore references and user-supplied assumptions appear in their own column.

| Ship                       | AotR length (mod units) | Liberty-equivalent length (m) | Lore reference or assumption (m)        |
| -------------------------- | ----------------------: | ----------------------------: | --------------------------------------- |
| MC80 Justice               |                   740.4 |                          ≈931 | Unverified                              |
| MC80 Freedom               |                   963.9 |                        ≈1,211 | Unverified                              |
| MC80 Liberty               |                   954.8 |                         1,200 | 1,200, reference                        |
| MC80a Alliance             |                   857.7 |                        ≈1,078 | 1,200, assumed for this variant         |
| MC80b Remonda              |                 1,004.0 |                        ≈1,262 | 1,200, user-provided reference          |
| Mediator Battlecruiser     |                 1,369.7 |                        ≈1,721 | Roughly >1,600, exact length unverified |
| Interdictor Star Destroyer |                   942.0 |                        ≈1,184 | 1,600, Imperial I-derived Dominator     |

- User's lore-proportion targets: Alliance `954.8 units` (`1.1133×`), Interdictor `1,273.1 units` (`1.3514×`).
- Mediator raw ALO length `913.161 units` × XML scale `1.5` → `1,369.741 units`. Other listed ships use `1.0×`.
- Interdictor uses the Imperial I hull. Length references → [Mon Calamari Star Cruiser](https://www.starwars.com/databank/mon-calamari-star-cruiser), [Imperial Star Destroyer](https://www.starwars.com/databank/Imperial-Star-Destroyer), [Legends Dominator](https://starwars.fandom.com/wiki/Dominator_%28Interdictor-class%29).

## AotR 2.11.9 Combat Stats

| Ship | Tactical hull HP | Shields | Shield regen/s | Max speed | Max turn rate | Sensor range | Weapon range max / ideal |
|---|---:|---:|---:|---:|---:|---:|---:|
| Justice | 12,000 | 14,000 | 35 | 1.75 | 0.155 | 4,000 | 2500 / 2500 |
| Freedom | 16,000 | 18,000 | 45 | 2.25 | 0.2 | 4,000 | 2500 / 1250 |
| Liberty | 15,000 | 15,000 | 37.5 | 2 | 0.177 | 4,000 | 4000 / 3250 |
| Alliance | 13,000 | 14,000 | 35 | 2 | 0.177 | 4,000 | 5250 / 5000 |
| Remonda | 16,000 | 16,000 | 40 | 2.75 | 0.244 | 4,000 | 4000 / 3250 |
| Mediator | 35,000 | 40,000 | 80 | 1.75 | 0.116 | 5,500 | 5250 / 4500 |
| Interdictor | 20,000 | 16,000 | 20 | 2.25 | 0.1 | 5,000 | 4000 / 2500 |

- Speed, turn, and sensor values are raw XML values. Weapon ranges come from the English tooltips.
- Hull armor is medium for the Rebel ships and heavy for the Interdictor. Liberty and Remonda have anti-turbolaser shields. The other five use balanced capital shields.
- Energy capacity and refresh match shield capacity and refresh for each listed ship.
- Interdictor reinforcement prevention radius: `4,000` game units.

## Build Stats

| Ship | XML tech | Required starbase / structure | Galactic credits / build time | Skirmish credits / build time | Galactic / tactical pop |
|---|---:|---|---:|---:|---:|
| Justice | 0 | 3 | 15,000 / 300 s | 9,100 / 61 s | 8 / 35 |
| Freedom | 2 | 1 + `Rebel_Capital_Shipyard` | 16,000 / 320 s | 10,200 / 68 s | 8 / 34 |
| Liberty | 2 | 1 + `Rebel_Capital_Shipyard` | 18,000 / 360 s | 11,150 / 74 s | 8 / 35 |
| Alliance | 2 | 1 + `Rebel_Capital_Shipyard` | 19,000 / 380 s | 10,900 / 72 s | 8 / 36 |
| Remonda | 3 | 3 | 19,000 / 380 s | 13,400 / 89 s | 8 / 35 |
| Mediator | 3 | 1 + `Rebel_Capital_Shipyard` | 36,500 / 730 s | 19,600 / 130 s | 12 / 62 |
| Interdictor | 3 | 1 + `E_Shipyard_CapitalModule` | 21,000 / 420 s | 14,500 / 106 s | 8 / 34 |

- Values are base XML fields. Campaign unlocks and availability can depend on scenario or scripting.
- Mediator build limit: `3` active per player.

## Loadouts

- Weapons and abilities follow the English encyclopedia tooltips. `TL` = turbolaser, `IC` = ion cannon, `LC` = laser cannon, `LR` = long-range, `CR` = close-range, `RKT` = rocket.
- Justice inherits base stats from `T_MC80_Justice`. Its starting and reserve complements come from `R_MC80_Justice`.
- Remonda uses `MC80c.ALO` and a shared weapons tooltip. Its other stats use the `R_MC80b` unit entry.

### MC80 Justice

- Battle Carrier · Project Shield · Bombardment, Bombing Run.
- Weapons: 4xMed Dual TL, 4xLt Dual Turbo IC, [6xLt TL, 8xLC].
- Start/reserve: 1/2 Z-95 Squadron, 1/2 R-41 Squadron, 1/2 Y-Wing Squadron.

### MC80 Freedom

- Brawler / Disabler · Defensive Shield · Bombardment, Bombing Run.
- Weapons: 4xHvy Stun Dual IC, 6xMed CR Dual TL, [6xLt TL, 4xHvy LC].
- Start/reserve: 1/0 Y-Wing Squadron.

### MC80 Liberty

- Battleship · Power to Shields · Bombardment.
- Weapons: 4xHvy Dual Turbo IC, 6xMed Dual TL, [8xMed TL, 6xHvy LC].
- Start/reserve: 1/0 A-Wing Squadron.

### MC80a Alliance

- Artillery · Power to Shields, Full Salvo · Bombardment.
- Weapons: 8xMed LR Dual TL, 2x8-Burst Diamond Boron RKT, [6xMed LR TL, 4xLC].
- Start/reserve: 1/2 X-Wing Squadron, 1/2 Y-Wing Squadron.

### MC80b Remonda

- Battleship · Defensive Shield, Power to Weapons · Bombardment, Bombing Run.
- Weapons: 4xHvy Dual Turbo IC, 7xMed Dual TL, [4xMed Dual Turbo IC, 7xMed TL, 2xHvy LC].
- Start/reserve: 1/2 X-Wing Squadron, 1/2 Y-Wing Squadron.

### Mediator Battlecruiser

- Command ship · Defensive Shield · Bombardment, Bombing Run.
- Weapons: 8xMed 2-Burst LR Dual TL, 6xMed 2-Burst LR Dual Turbo IC, [8xLt LR Dual TL, 6xLt LR Dual Turbo IC, 6xHvy LC].
- Start/reserve: 1/2 A-Wing Squadron, 1/2 E-Wing Squadron, 1/2 B-Wing Squadron, 1/1 K-Wing Squadron.

### Interdictor Star Destroyer

- Interdictor · Interdict, Electro-Magnetic Defense Field · Bombardment.
- Weapons: 2xHvy 2-Burst TL, 2xMed Turbo IC, [3xMed 3-Burst TL, 4xLt TL, 6xLC].
- Start/reserve: 1/2 TIE-Fighter Squadron, 1/2 TIE-Scout Flight.
