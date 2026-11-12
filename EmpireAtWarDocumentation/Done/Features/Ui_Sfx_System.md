---
category: Features
status: done
created: 2026-11-12
tags:
  - audio
  - ui
completed: 2026-11-12
acceptance: pending-user
---
# UI SFX System

## Goal
- Click sound for every accepted UI button press (main menu + battle) with no per-button wiring.
- Selection sound per selected unit kind: station, ship, other unit, enemy.
- Forbidden sound remains deferred. See [[#Deferred follow-up]].

## Decision
- Chosen: **one global click listener** (variant A).
- Why: 31 button prefabs / 37 views use `onClick.AddListener` directly; no shared button wrapper. A listener covers all of them, plus future buttons, with zero prefab edits.
- Avoid: per-button `UiButtonSfx` component (31 prefab edits, DI into prefab components, silent if forgotten); calls inside each view handler (37 views).
- Rule: **one UI cue per frame, highest priority wins** → `selection > click` (later `forbidden > selection > click`). Clicking a ship icon that selects the ship plays only the selection sound.

## Assets
- Source: `Assets/Audio/SFX/Interface/GeneratedUi/`
- `ui_click_01..03.wav` → accepted button press.
- `select_station_01..03.wav` → Base selected.
- `select_ship_01..03.wav` → Ship selected (squadrons included).
- `select_unit_01..03.wav` → MiningFacility / DefendPlatform selected.
- `select_enemy_01..03.wav` → Three original synthesized target-acquisition cues. Each is 248 ms, 48 kHz mono, 16-bit PCM, with a −8 dBFS peak.
- `ui_forbidden_01..03.wav` → present, unused until forbidden phase.

## Selection Rules
| Selection | Scope | Sound |
|---|---|---|
| Base | Local / ally | station |
| Ship / squadron | Local / ally | ship |
| MiningFacility, DefendPlatform | Local / ally | unit |
| Any | hostile (`ILocalPlayer.IsHostile(entity.Owner)`) | enemy |

- Play only when the selection changes to a new non-empty set; skip when `ISelectionSubject.IsPruningSelection` (unit died / removed).
- Ship voice (`AudioDialogShipComponent` → `TryPlayVoice(_voices.Selection)`) stays; selection SFX plays alongside it.

## Implementation
1. **Data** — `UiSfxData : Data` in `Services/Audio`, asset `Assets/Settings/Data/Models/Audio/UiSfxData.asset`. Fields as `RandomAudioClips`: `click`, `selectStation`, `selectShip`, `selectUnit`, `selectEnemy`. Volume per cue.
2. **`UiSfxService`** (`IUiSfxService`, `ILateTickable`, `IInitializable`, `ILateDisposable`), registered in `ProjectScope` (survives menu ↔ battle).
   - One 2D `AudioSource`, mixer group `Sfx`, `ignoreListenerPause = true` (works while paused).
   - `RequestClick()`, `RequestSelect(UiSelectSfx kind)` only store the pending cue; `LateTick` plays the highest priority via `IAudioService.PlayOneShot`, then clears.
3. **`UiClickListener`** — uses the active EventSystem's pointer click and submit actions. Raycast to the top hit's click handler for pointer input. Temporarily listen to the target `Button.onClick` to request audio only after uGUI accepts the click. Disabled buttons and canceled presses stay silent.
4. **`UiSfxMute`** — empty marker MonoBehaviour for buttons with their own audio (candidates: `ShipAbilityButtonUi`, `SuperWeaponButton`). Both candidates keep the click cue. Ship ability audio follows gameplay state; superweapon announcements follow firing and availability.
5. **`SelectionSfx`** (Skirmish scope) — subscribes to `ISelectionService` (`INotifier<ISelectionSubject>`), maps `SelectionType` + hostility → `RequestSelect(kind)` per the table.
6. **Verify** — Unity recompile completed with zero errors. Editor verified the saved `ProjectContext.prefab` binding, exact `Sfx` group, and all 15 imported clip references. Tests not run under the local override. In-game checks → [[TODOs/User_Acceptance_Queue]].

## Files
- New: `Services/Audio/UiSfxData.cs`, `IUiSfxService.cs`, `UiSfxService.cs`, `UiSelectSfx.cs` (enum), `UiClickListener.cs`, `SelectionSfx.cs`, `Components/Ui/UiSfxMute.cs`.
- Edit: `Services/VContainer/Scopes/ProjectScope.cs` (data field + registration), `Services/VContainer/Registrations/SkirmishServiceRegistrations.cs` (`SelectionSfx`).
- Test categories touched: `Audio`, `Ui`, `BattleInput`.

## Edge Cases
- Click on world (no UI hit) → no click; selection handled by `SelectionSfx`.
- Box select of many ships → one ship cue (per-frame arbitration).
- Mixed friendly selection → unit cue when the context reports `SelectionType.None`. Terrain is silent.
- Selecting through hotkeys / control groups → selection sound, no click.
- Main menu has no `ISelectionService` → `SelectionSfx` only in Skirmish scope.

## Reusable guidance
- Shared button audio, opt-out and cue priority → [[Rules/UI_CODE_BUILD_GUIDE#6. Shared button audio]].

## Deferred follow-up
- [ ] Forbidden phase (deferred by user 2026-11-12): play `ui_forbidden` for refused buy (insufficient credits), refused ability press, invalid ability target on wrong ship. Route existing `InterfaceSfxService.PlayRejected()` callers (`FactionUiController`, `CaptureSiteBuildUiController`, `SuperWeaponPresenter`, `AbilityTargetingSfx`) through `UiSfxService` with priority `forbidden > selection > click`; decide whether disabled buttons also play forbidden.
