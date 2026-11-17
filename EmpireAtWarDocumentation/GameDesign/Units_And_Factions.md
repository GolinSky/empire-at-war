---
type: design-reference
system:
  - units
  - factions
  - hangars
design_state: implemented
verified: 2026-11-17
verification: Checked faction roster entries, 7-tier warship class ladder, 27 class/tier reassignments, Nebulon-B 600 m resize, and saved CR90, Vulture and Endurance assets.
---
# Units and Factions

## Goal

- Current build rosters, unit roles and ownership of gameplay data.
- Entry point: [[GameDesign/00_GameDesign]]. Costs and unlocks: [[GameDesign/Production_And_Research]]; combat: [[GameDesign/Combat]].

## Rules

- Faction selects a roster; player team determines alliances. `PlayerRoster.IsAllied` compares teams, not faction names; different factions can be allies.
- Buildable roster entries, registered data assets and imported models are different sets. An asset or enum entry alone does not make a unit purchasable.
- `FactionDefinition` owns ships, squadrons, research lines and allowed shared buildables. `FactionCatalog` currently references all four definitions below.
- `FactionData.Role` and descriptions describe intended use; actual performance comes from unit data, installed weapons, damage rules, abilities and runtime modifiers.
- Use current saved assets for numeric configuration. [[GameDesign/Proposals/Rebel units]] and [[GameDesign/Proposals/Empire Fleet Import]] retain proposals; import records retain production history.

## Important Values

### Faction rosters

- Read each faction’s `ships` and `squadrons` tables in the linked faction assets for current roster membership, prices and limits.

### Current examples

- CR90: `Corvette`, anti-fighter/anti-bomber. Base hull/shields `600/600`, speed `35` units/s. Eight baked `Laser` and two `PointDefense` mounts.
- CR90 purchase: `1,250` credits, `20 s`, level `1`, population `3`, maximum `10`. No hangar bays. Power to Engines uses `BoostEnginePower` (ID `4`).
- Vulture: `Fighter` with `13` members, each `44` hull and `0` shields. Base combined hull `572`, cruise/combat speed `43.2/48` units/s, and `52` baked weapon mounts.
- Endurance: three hangar bays with `8` reserve and `2` maximum active squadrons each.
- Combat classes (`ShipClass`): Fighter (`0`), Bomber (`1`), Interceptor (`7`), HeroStrikecraft (`9`), Corvette (`2`), Frigate (`3`), Cruiser (`8`), HeavyCruiser (`10`), Capital (`4`), BattleCruiser (`5`), Dreadnought (`11`), Structure (`6`). They select matrix/bias behavior; a display-name phrase does not define a new class.
- Warship class ladder (2026-11-17): `Corvette → Frigate → Cruiser → HeavyCruiser → Capital → BattleCruiser → Dreadnought`. `NebulonB` resized from 300 m (`31.5` units) to 600 m (`63.0` units, `Cruiser`).
- 2026-11-17 warship class and matching `ShieldRegenTier` reassignments (`Cruiser`/`Frigate`/`HeavyCruiser`/`Capital`/`BattleCruiser`/`Dreadnought`; unlisted ships retain prior class and `ShieldRegenTier`):
  - `Frigate`: `Carrack` (from `Corvette`), `AcclamatorAssault` (from `Cruiser`).
  - `Cruiser`: `Acclamator`, `Centax`, `NebulonB`, `Munificent`, `Captor` (all from `Frigate`).
  - `HeavyCruiser`: `Gladiator`, `MC80Freedom`, `DHOmni` (from `Capital`); `Victory`, `VictoryI`, `VictoryII`, `HeavyDreadnought`, `MC75Profundity`, `BothanAssaultCruiser`, `Liberator`, `MC30c`, `CorellianBattlecruiser`, `Dispatcher`, `Recusant`, `Peleset` (from `Cruiser`).
  - `Capital`: `Dauntless` (from `Cruiser`).
  - `BattleCruiser` (serialized ID `5`, renamed from `HeavyCapital`): `MediatorBattlecruiser` (from `Capital`); `Allegiance`, `Titan`, `Procurator`, `Secutor`, `HomeOne`, `MC80Independence`, `RecusantDreadnought`, `Invincible`, `Lucrehulk`, `Malevolence` hold ID `5`.
  - `Dreadnought` (serialized tier ID `5` replacing `Executor`): `Executor`, `Praetor`, `Mandator`.
- Saved `ShipData` distribution (76 total; every asset matches its `ShieldRegenTier`): `Corvette` 9, `Frigate` 7, `Cruiser` 10, `HeavyCruiser` 15, `Capital` 21, `BattleCruiser` 11, `Dreadnought` 3.
- Shared superweapon permissions currently match across all four factions: Ion Cannon, Hypervelocity Gun, Plasma Cannon. See [[GameDesign/Abilities_And_Superweapons]].

## Implementation

### Data ownership

| Data | Owns |
|---|---|
| `FactionDefinition` / `FactionData` | Roster membership, display role/name, hero flag, prices, build times, unlock levels, maximum counts, population and research |
| `ShipData` | Hull/shields, class, movement, radar, hardpoint health rules, assigned ability IDs and hangar bays |
| `SquadronData` | Per-member health, flight/attack-run settings, formation, repair/countermeasures, vision and ability IDs |
| Unit view prefabs | Installed mounts, sockets, firing arcs, subsystem configuration and explicit references |
| `WeaponsData` / `DamageMatrixData` | Shared weapon rules, variants, class damage/accuracy and shield interactions |
| `ShipAbilityCatalog` | Shared ability definitions, timing, target requirements and concrete settings |

- `ShipData.weaponLoadout` and `SquadronData.MemberCount/weaponLoadout` are baked summaries of prefab configuration; they are not independent weapon authoring locations.
- `ShipData.Range` is radar range. Weapon reach is derived from each weapon profile; see [[GameDesign/Combat]] and [[GameDesign/Visibility]].

### Carrier reserves

- Each `HangarBay` identifies squadron type, initial reserve and maximum active squadrons.
- Initial and replacement launches consume the same reserve: launch → reserve `−1`, active `+1`; do not add reserve and maximum active as separate supplies.
- Launch requires an operational, enabled bay with reserve `>0` and active `< MaxActive`; launch interval throttles automatic launches.
- Losing a squadron frees an active slot. A disabled bay stops further launches; all bays disabled → hangar non-operational.
- Carrier-provided squadrons need not match the faction's purchase roster. Read the carrier's current bays instead of inferring its complement from lore or model geometry.

## Edge Cases

- The CR90 and Endurance import records and fleet proposals preserve their dated figures. Use the saved unit assets for current values.
- Runtime health, movement and damage can differ from base data through research, abilities, station levels and environmental modifiers.
- The 2026-11-16 check covers source and serialized data.

## Files

- `Assets/Settings/Data/Factions/Shared/FactionCatalog.asset` — registered factions.
- `Assets/Settings/Data/Factions/Republic/RepublicFaction.asset`, `Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset` — Republic/CIS rosters.
- `Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset`, `Assets/Settings/Data/Factions/Empire/EmpireFaction.asset` — Rebellion/Empire rosters.
- `Assets/Settings/Data/Ship/Rebellion/CorellianCorvetteShipData.asset`, `Assets/Settings/Data/Ship/Rebellion/EnduranceShipData.asset`, `Assets/Settings/Data/Squadron/VultureSquadronData.asset` — checked examples.
- `Assets/Scripts/Entities/Faction/Model/FactionDefinition.cs`, `Assets/Scripts/Entities/Faction/Model/FactionCatalog.cs`, `Assets/Scripts/Entities/Faction/Model/FactionType.cs` — roster definitions and identifiers.
- `Assets/Scripts/Entities/Faction/Model/FactionData.cs` — `Role`, purchase values and hero metadata.
- `Assets/Scripts/Entities/Players/PlayerRoster.cs` — `IsAllied`, `IsHostile`.
- `Assets/Scripts/Entities/Ship/Data/ShipData.cs`, `Assets/Scripts/Entities/Squadron/Data/SquadronData.cs` — unit schemas.
- `Assets/Scripts/Components/Hangar/HangarModel.cs` — `TryLaunchImmediately`, `FindImmediateLaunchBay`, `DisableBay`.
