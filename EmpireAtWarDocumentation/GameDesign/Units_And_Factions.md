---
type: design-reference
system:
  - units
  - factions
  - hangars
design_state: implemented
verified: 2026-10-20
verification: source-and-assets
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

### Build rosters — saved 2026-10-20

| Faction | Ship entries | Squadron entries | Squadron roster |
|---|---:|---:|---|
| Republic | 12 | 5 | ARC-170, BTL-B Y-Wing, NTB-630, V-19 Torrent, V-Wing |
| Separatist | 9 | 3 | Belbullab-22, Advanced Droid Bomber, Vulture Droid |
| Rebellion | 17 | 3 | A-Wing, T-65 X-Wing, BTL-A4 Y-Wing Bomber |
| Empire | 14 | 6 | TIE Fighter, Bomber, Avenger, Interceptor, Brute, Punisher |

- Total: `52` ship entries and `17` squadron entries. Counts are a dated roster snapshot, not population limits or all available art.
- Republic ships: Venator I/II, Arquitens, Acclamator, Heavy Dreadnought, Thranta, Rothana, Resolute, Pride of the Core, Imperator, IPV-2C Stealth Corvette, Victory.
- Separatist ships: Providence, Recusant, Munificent, Lucrehulk, Malevolence, Captor, Patrol Frigate, C-9979, Dispatcher.
- Rebellion ships: Nebulon-B, CR90, MC80, Home One, MC75 Profundity, MC80 Independence, Corellian Battlecruiser, MC30c, CD-273, Quasar Fire, MC50, Assault Frigate Mk II, Dauntless, Liberator, Nebula, Bothan Assault Cruiser, Endurance.
- Empire ships: Victory I/II Advanced Loadout, ISD I/II, ArquitensImperialCruiser, Executor, Raider, AcclamatorAssault, Tector, Imperial Venator, Procursator, Titan, Allegiance, Tartan.

### Current examples

- CR90: `Corvette`; anti-fighter/anti-bomber role; `500` hull, `800` shields, speed `40` units/s; eight baked Light Laser Cannon mounts.
- CR90 purchase: `1,250` credits, `25 s`, level `1`, population `1`, maximum `10`. Its data has no hangar bays and ability ID `14` (Power to Engines).
- Vulture: `Fighter`; `7` members, each `35` hull / `0` shields; base combined hull `245`. Cruise/combat speed `34.5/39` units/s; baked loadout `28` fighter-laser mounts.
- Combat classes: Fighter, Bomber, Interceptor, Corvette, Frigate, Cruiser, Capital, HeavyCapital, Structure. They select matrix/bias behavior; a display-name phrase does not define a new class.
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

- CR90 source conflict: its old import record says `750/600/24.5`; the fleet proposal says `500/800/80`; current saved hull/shields/speed are `500/800/40`. These are separate snapshots, not interchangeable specifications.
- Endurance's saved bays currently contain `reserve/maxActive = 1/1, 1/1, 0/0, 0/0`; the older `6 active + 6 replacements` proposal/import is not the current asset configuration.
- Runtime health, movement and damage can differ from base data through research, abilities, station levels and environmental modifiers.
- Verified from C# and saved assets on `2026-10-20`; no battle simulation, Play Mode or automated tests were run for this reference.

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
