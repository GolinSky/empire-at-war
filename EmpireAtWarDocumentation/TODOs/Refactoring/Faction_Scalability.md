---
category: Refactoring
status: in-progress
created: 2026-10-05
tags:
  - factions
  - data
  - scalability
---
# Faction Scalability

## Goal
- Adding a faction = 1 `FactionDefinition` asset + `FactionCatalog` entry. No enum edits.
- Adding a unit = 1 `ShipDefinition`/`SquadronDefinition` asset + faction list entry.
- Prepares 2 new factions.

## Problems (verified 2026-10-05)
- `FactionsData.asset` (1120 lines) mixes per-faction ships/squadrons/research with global mining, defend platforms, super weapons, station levels.
- `ShipType` (21) / `SquadronType` (9) list all units of all factions. One ship → ~7 edits: enum, `FactionsData`, `ShipsData`, `ShipUiData`, `ReinforcementData`, `<Name>ShipView` address, `ShipIconGenerator`.
- Drift: `StealthCorvette` in enum only; `StarDestroyer1/2` not buyable.
- `FactionType` keys `MusicAudioData`, `ShipSfxData.voices`, `SpaceStationData.wrecks/hangarBays`, `MapGenerationSettings.stationRadii`, `<Faction>SpaceStationView` address.
- Hardcoded `ShipType.Resolute` (one per player, fleet-command aura) and `FactionType.Republic` (aura recipients).

## Decision
- Unit identity = `UnitDefinition` SO (`ShipDefinition`, `SquadronDefinition`). Why: one asset per unit, no enum. Avoid: string ids + registry (runtime typos).
- Faction identity = `FactionDefinition` SO listed in `FactionCatalog`.
- Mining / defend platform / super weapon = shared catalogs; faction lists buildable entries.
- Station levels = shared `StationLevelData`.
- Resolute → definition flags `uniquePerPlayer`, `fleetCommander`.

## Steps
- [x] Phase 1 — decompose `FactionsData` into `FactionDefinition` + shared catalogs + `StationLevelData` (enums kept). Code + data done 2026-10-05; skirmish playtest pending.
- [ ] Phase 2 — unit definitions replace `ShipType`/`SquadronType`; delete `ShipsData`, `ShipUiData` icon maps, `ReinforcementData` unit maps.
- [ ] Phase 3 — `FactionDefinition` replaces `FactionType` (slots, dropdown, station, map radius, music, voices, names).
- [ ] Phase 4 — editor tooling (`ShipIconGenerator`, `SquadronViewPrefabBuilder`) + `Tools/Validate Faction Content`.
- [ ] Phase 5 — tests updated to compile (run only on request).

## Files
- `Assets/Settings/Data/Factions/<Faction>/` — faction + unit definitions.
- `Assets/Settings/Data/Factions/Shared/` — catalogs, `StationLevelData`.

## Progress
### Phase 1 (2026-10-05)
- New: `FactionDefinition` (temp `factionType` field until Phase 3), `FactionCatalog`, `StationLevelData`, `MiningFacilityCatalog`, `DefendPlatformCatalog`, `SuperWeaponCatalog`, `FactionRoster` (per-player, pure C#).
- Assets: `Assets/Settings/Data/Factions/{Republic,Separatist}/<Faction>Faction.asset`, `Assets/Settings/Data/Factions/Shared/*.asset`. Addressable (Model group), address = type name.
- `FactionRoster` bound per player in `PlayerCoreInstaller` / `AiPlayerInstaller` via `FactionCatalog.Get(slot.Faction)`.
- Cross-faction lookups (`HeroUiController`, `ShipUiController`, tooltips, cheats) → `FactionCatalog` (removed in Phase 2).
- Every faction currently lists all mining/platform/super weapon types → behaviour unchanged.
- Deleted `FactionsData.cs`/`.asset`, its Addressable entry, `AssetMappingData` key.
- Migration copied values by reflection (lossless); spot check: Venator 5800, level-2 cost 2000, Republic 10 ships/6 squadrons/5 research, Separatist 9/3/5.
- Unity recompile: 0 errors. Tests updated, not run.
- Not changed: `SuperWeaponPresenter`/`CheatView` still loop all `SuperWeaponType` (render-only; prefab has one button per weapon).
- Pending: skirmish playtest (build menu, levels, research, AI super weapons).

### New factions (2026-10-05)
- `FactionType`: `Rebellion = 2`, `Empire = 3`. Skirmish dropdown picks them up from the enum.
- `Factions/Rebellion/RebellionFaction.asset`: ships none, squadrons `AWing` (Republic A-Wing purchase data copied).
- `Factions/Empire/EmpireFaction.asset`: no ships, no squadrons.
- Both: research = copy of Republic; all mining/platform/super weapon types; listed in `FactionCatalog`.
- Placeholders borrowed from Republic (replace when content exists):
  - `MapGenerationSettings.stationRadii`, `MusicAudioData.battleMusicData`, `ShipSfxData.voices`, `SpaceStationData.wrecks`.
  - `SpaceStationData.hangarBays`: Rebellion → `AWing`, Empire → `Delta7`.
  - `AssetMappingData`: `RebellionSpaceStationView`, `EmpireSpaceStationView` → Republic station prefab.
- Fleet-command aura still Republic-only (`FleetCommandService.cs:54`).

## Adding a unit to a faction (until Phase 2)
1. Unit must already exist: `ShipType`/`SquadronType` member, `<Name>ShipView` / `<Name>SquadronView` + data addresses, `ShipsData` (ships), `ShipUiData` icon, `ReinforcementData` spawn preview.
2. Add `ShipType → FactionData` to `ships` (or `SquadronType → FactionData` to `squadrons`) in `Assets/Settings/Data/Factions/<Faction>/<Faction>Faction.asset`.
3. A ship may belong to one faction only (`CheatPresenter` throws on duplicates); squadrons may be shared (e.g. A-Wing in Republic + Rebellion).
4. Station hangar squadron: `SpaceStationData.hangarBays[<faction>].squadronType`.

## Out of scope (noted)
- `WeaponType` enum growth; `SiteFacilityBuilder` switch; `PlanetType` switch; IonCannon AI rule; Cloak special case; ability-specific `HardPointType` values.

## Verification
- 0 compile errors per phase; migrated values spot-checked; skirmish playtest (build menu, spawn, hangar, AI, Resolute limit/aura, levels).
