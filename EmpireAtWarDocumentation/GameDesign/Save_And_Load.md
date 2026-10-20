---
type: design-reference
system:
  - saves
  - persistence
design_state: implemented
verified: 2026-10-20
verification: source-and-assets
---
# Save and Load

## Goal

- Current player save/load behavior and the state preserved by a battle save.
- Source and serialized UI/input snapshot: 2026-10-20. No save round-trip or interactive acceptance test ran for this reference.

## Rules

### Supported modes and slots

- Skirmish and Survival support saves. Battle Simulation does not.
- Save capture is permitted while Running or Paused; Loading, Ended and concurrent save writes are refused.
- Default F5 → quicksave; F9 → quickload for the current mode. Saved input rebinding may override these keys.
- Quicksave and autosave each overwrite one dedicated slot per mode; named slots are separate.
- Storage: `Application.persistentDataPath/Saves/<Skirmish|Survival>/<slot>.json`.
- Names `autosave` and `quicksave` are reserved. Named slots reject empty/whitespace names, padding, trailing dots and invalid filename characters.
- Save browser: Save As New requests a name; duplicate names are rejected case-insensitively; selecting an existing save overwrites that slot.
- Load browser has Skirmish/Survival mode tabs, newest-first entries, save date/battle duration, and remove actions.
- Unsupported versions remain identifiable but cannot load. Unreadable files are skipped from listing with a diagnostic.
- Save/load failures are shown as status text.

### Autosave

- Interval: 300 seconds of elapsed battle time; the clock advances only while Running.
- Time is scaled: 4× speed advances battle seconds faster; paused time does not advance the battle clock.
- A restored battle starts a fresh 300-second interval.
- Survival also requests autosave on WaveActive → Preparation, after waves 1–4.
- Final victory is not a between-wave autosave; Ended battles cannot be saved.
- If saving is unavailable or another save is in progress, the due autosave remains pending.
- A successful due-save dispatch schedules the next interval from the current elapsed time.

### Saved world and player state

- Battle setup: mode-specific options; Skirmish map seed preserves its generated layout.
- Header: format version, mode/slot, UTC save time, elapsed battle time and game speed.
- Camera position, rotation and target zoom height.
- Living ships/squadrons: stable entity ID, owner/type, position/rotation, health and current order.
- Ship health stores hull/shield/hardpoint fractions; squadron health stores per-member hull/shield values.
- Stations, mining facilities and defense platforms: identity, owner/type, position and health; hangar state where present.
- Ability slot states/timers; hangar state and launched-squadron relationships.
- Per-player money, station level, completed research, production queue, built reserve, paid AI builds, AI ship-order count and superweapon state.
- Capture-site owner, state, capture progress, facility/construction state and operational facility link.
- Relay owner, capturing player, capture progress and contested flag.
- Victory model memory of which players' stations were already observed alive.
- Survival wave index/phase, preparation time, queued unspawned units, spawn count and settle delay.

### Restore behavior

- Loading rebuilds the mode setup, restores model/player state and entities, then reapplies relationships and orders.
- Original entity IDs reconnect order targets, hardpoints and hangar-owned squadrons.
- Camera pose, elapsed time and saved speed are restored.
- Restored ships use `ShipSpawnPose.Restored` instead of replaying hyperspace arrival.
- Active ability → Recovering for `remaining active time + full RecoveryDelay`; the active effect itself is not resumed.
- Orders restore destinations, offsets, hardpoint target and remaining waypoints.
- A missing restored target leaves the unit idle. A pending UseAbility order becomes None.
- An Attack Move order receives a new per-unit engagement; the original shared group engagement is not preserved.
- Survival restores the director queue instead of queuing the whole current wave again.

## Important Values

- Current format: `BattleSaveHeader.CURRENT_VERSION = 2`.
- Compatibility is exact version equality. Older/newer formats are refused; no migration is performed.
- Files are written to a temporary file, then replaced/moved into the slot path.
- Capture happens before background file writing; Unity state is captured on the calling main-thread flow.
- `autosave.json` and `quicksave.json` are independent in Skirmish and Survival.

## Edge Cases

- Save state is not a complete simulation snapshot: live projectiles, VFX, selection/UI targeting, fog-grid/discovery presentation and active ability effects are not serialized.
- Unit save records do not preserve weapon reload, shield-regeneration delay or movement interpolation internals.
- Balance assets are not embedded in the save. Restoration uses current unit definitions; a saved ability-slot mismatch raises an error.
- Saved game speed does not record a paused-state flag; do not assume loading returns to a paused battle.
- Quickload is refused while the battle is loading, ended or writing a save.
- The v2 relay state and separate save/load browsers supersede older plan descriptions of format v1.
- [[GameDesign/Modes_And_Victory]], [[GameDesign/Economy]], [[GameDesign/Production_And_Research]], [[GameDesign/Capture_And_Structures]] define the restored systems.

## Files

- `Assets/Scripts/Services/Saves/BattleSave.cs`; `Assets/Scripts/Services/Saves/BattleSaveHeader.cs` — serialized state and version.
- `Assets/Scripts/Services/Saves/BattleSaveService.cs`; `Assets/Scripts/Services/Saves/JsonBattleSaveRepository.cs` — capture, restrictions, storage.
- `Assets/Scripts/Services/Saves/BattleSaveActions.cs`; `Assets/Scripts/Services/Saves/BattleSaveSlot.cs` — player actions and names.
- `Assets/Scripts/Services/Saves/AutosaveSchedule.cs`; `Assets/Scripts/Services/Saves/BattleClock.cs`; `Assets/Scripts/Services/Saves/SurvivalAutosave.cs`.
- `Assets/Scripts/Services/Saves/UnitSaveService.cs`; `Assets/Scripts/Services/Saves/AbilitySlotSave.cs`; `Assets/Scripts/Services/Saves/UnitOrderSave.cs`.
- `Assets/Scripts/Services/Saves/BattleRestorer.cs`; `Assets/Scripts/Services/Saves/BattleRestoreStep.cs`.
- `Assets/Scripts/Services/Saves/PlayerSave.cs`; `Assets/Scripts/Services/Saves/CaptureSiteSave.cs`; `Assets/Scripts/Services/Saves/ReinforcementZoneSave.cs`; `Assets/Scripts/Services/Saves/SurvivalSave.cs`.
- `Assets/Scripts/Entities/Saves/SaveGameRouteUiController.cs`; `Assets/Scripts/Entities/Saves/LoadGameRouteUiController.cs`.
- `Assets/Prefabs/Ui/MainMenu/SaveBrowserUi.prefab`; `Assets/Settings/Input/EmpireAtWar.inputactions`.
- [[GameDesign/00_GameDesign]].
