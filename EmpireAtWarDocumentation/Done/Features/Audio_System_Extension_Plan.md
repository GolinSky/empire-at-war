---
category: Features
status: done
created: 2026-10-22
completed: 2026-10-29
acceptance: pending-user
---
# Audio System Extension Plan

> Closed 2026-10-29 by backlog triage: implementation complete; open user gates → [[TODOs/User_Acceptance_Queue]].

## Goal
- Add Republic at War SFX: wrong click, orbital-weapon announcer, ship ambience, ability toggles, shield up/down/impact, per-unit engines and hardpoint-loss explosions, alarms.
- Extend the existing `ShipSfxService` / `ShipSfxData` pipeline; no audio MVP, the service decides play/refuse.
- Clip inventory, unit mappings and verified hooks: [[Done/Features/Audio_System_Extension_Plan - Research|Research]].

## Rules
- Source root `F:/Temp_audio/Republic_at_War_1129810972/Audio`; keep originals untouched.
- Import to `Assets/Audio/SFX/<Category>/` (`Interface`, `Ambient`, `Abilities`, `Shields`, `Engines/<FOLDER>`, `Explosions/<Folder>`, `Alarm`); keep source filenames.
- Reuse `Assets/Audio/SFX/PlanetaryAttack/I000_rhd_900{0,1,2}_eng.wav` (byte-identical, currently unreferenced).
- Import only mapped clips, not whole folders.
- Positional unit sounds go through `IShipSfxService` (fog, cloak, camera gain, caps) — includes ability toggles and shield up/down for every visible ship. Interface sounds (wrong click, orbital announcer, targeting cancel) are 2D, local player only.
- Entity rules: components raise events → `Ship`/`Squadron` forwards to its audio component. No component commands a sibling.
- One type per file; `const` = `UPPER_SNAKE_CASE`; no constructor null guards.
- Verify by compile + EditMode tests only; user checks in-game (no agent Play Mode).

## Decision
- **Interface audio:** new `InterfaceSfxService : IInterfaceSfxService` + `InterfaceSfxData` with own 2D `AudioSource`s (UI + announcer). Methods: `PlayRejected()`, `PlayAbilityTargetingCanceled()`, `PlayAnnouncement(AudioClip)`.
  - Why: `ShipSfxService.TryPlayVoice` refuses while unit chatter plays → orbital announcements would be lost.
  - Announcer ducks SFX like unit voice (reuse `SfxDuckVolume`); announcement interrupts/blocks unit chatter.
- **Per-unit data:** `ShipSfxData` gets `DictionaryWrapper<ShipType, ShipUnitAudioProfile>` and `DictionaryWrapper<SquadronType, ShipUnitAudioProfile>` (engine, explosion, optional alarm override) + `fallbackEngine`, `fallbackExplosion`. Lookup `GetUnit(ShipType)` / `GetUnit(SquadronType)` returns fallback for missing keys.
- **Engine cap:** replace `slot.Sound == _data.Engine` with a `VoiceSlot.IsEngine` flag set by a new `TryHoldEngine(...)` entry point; cap stays `engineVoices` (raise 4 → 6, tune).
- **Ambience:** new `ShipAmbienceService` (own `ITickable`), not attached to ships; Burst jobs over registered unit transforms; one `AudioSource`, one loop at a time.
- Avoid: per-ship `AudioSource`s, ambience on ship prefabs, budget/helper classes, new MVP layers.

## Implementation
### 1. Assets and data
1. Import clips listed in Research to the folders above; Vorbis, Decompress On Load for <2 s one-shots, Compressed In Memory for loops/alarms; Load In Background on.
2. Replace common alarm: overwrite `Assets/Audio/SFX/Alarm/SHIP_ALARM.WAV` content keeping its GUID; set `ShipSfxData.alarm` to that single clip; drop `ShipAlarm2.mp3` from the profile (delete asset only if unreferenced).
3. Add `InterfaceSfxData.asset` (`Assets/Settings/Data/Models/Audio/`): `rejected`, `orbitalAvailable`, `orbitalBought`, `orbitalFiring`, `abilityActivated`, `abilityCanceled`.
4. Extend `ShipSfxData.asset`: `shieldsDown`, `shieldsUp`, `shieldImpact` (4 random clips), per-unit map, fallbacks, Lucrehulk alarm override.
5. Ambience data (`ShipAmbienceData` or fields on `ShipSfxData`): 2 loops, radius, fade time, volume, update interval.

### 2. Wrong click
1. `SuperWeaponPresenter.HandleTargetSubmitted`: invalid target (`!CanTarget`) → `PlayRejected()`.
2. `ShipAbilityService` local-player target rejection (`CanStart` false / no usable slot) → `PlayRejected()`.
3. Unaffordable purchase click (faction unit/super weapon buy, capture-site build `!CanAfford`) → `PlayRejected()`.
4. No other triggers (invalid move points are out of scope).
5. Cooldown ~0.15 s (clip length) so spam clicks do not stack.

### 3. Orbital weapon announcer (local player, all `SuperWeaponType`s)
- `Unavailable → Charging` (purchase) → `I000_RHD9001` "bought".
- `→ Ready` with `CanFire` (charge complete or cooldown elapsed re-announce) → `I000_RHD9000` "available".
- `HandleTargetSubmitted` accepted → `I000_RHD9002` "firing".
- Suppress announcements from `SuperWeaponModel.Restore` (save load) and battle start.
- Owner: `SuperWeaponPresenter` (already local-only, already handles `OnStateChanged`).

### 4. Ship ambience loops
1. `ShipAmbienceService` registers ship/squadron transforms from `IEntityLocator.EntityAdded/EntityRemoved` into a `TransformAccessArray` (swap-back removal).
2. Every 0.25 s: `IJobParallelForTransform` writes X/Z (pattern: `RadarPositionJob`) → Burst `IJob` reduces min squared XZ distance to the camera ground focus; complete next tick (no main-thread stall).
3. Audibility mask (`NativeArray<byte>`, refreshed main-thread ~1 Hz): friendly, or enemy visible and not cloaked — same rule as `ShipSfxService.GetGain`.
4. Volume = `1 - smoothstep(innerRadius, outerRadius, distance)` × zoom gain; fade ~1 s. Volume 0 → stop source.
5. One loop at a time: pick loop 1/2 at random on start; never play both. Pause with game pause.
6. EditMode tests: reduction job min-distance, mask exclusion, empty set → silence.

### 5. Ability activated / canceled (any visible ship)
- `Ready → Active` → `GUI_TOGGLE_17`.
- `Active → Recovering` → `GUI_TOGGLE_18`, for every end: player toggle-off and natural expiry/phase completion.
- Player cancelling ability *targeting* (no ability started) → `GUI_TOGGLE_18` as a 2D interface cue for the local player only.
- Positional via `IShipSfxService.TryPlayOneShot` → fog/cloak/camera gating; enemy ships audible only when visible.
- Hook: `AudioShipComponent.HandleAbilityChanged` already sees both transitions; no `ShipAbilityService` change needed except the targeting cancel.
- Keep per-ability `Start/Execution/End/Restore` sounds; toggle cues play alongside them (watch per-ship cap 2 — toggle cue must not evict `Start`; tune priority).

### 6. Shields up / down (any visible ship)
0. All owners; positional via `IShipSfxService` so fog/cloak/camera gain decide audibility.
1. `AudioShipComponent` (via `Ship` forwarding `HealthModelObserver.OnValueChanged`) tracks shield state with hysteresis:
   - Down: `Shields > 0 → Shields <= 0` (incl. generator lost).
   - Up: down-state and `ShieldPercentage >= 0.1` (threshold tunable).
2. Per-ship cooldown 4 s per sound + `SfxProfile.cooldown` ~0.5 s global.
3. Skip ships with `!HasShields`; ignore initial spawn / save restore state.
4. EditMode tests for the hysteresis/cooldown state class (pattern: `ShipEngineAudioState` + `ShipEngineAudioModelTests`).

### 7. Shield impact SFX
1. `HealthComponent.ShowShieldImpact` raises `ShieldImpacted(Vector3)` when the shield shows the impact.
2. `Ship` forwards → `AudioShipComponent.PlayShieldImpact(position)` → `TryPlayOneShot` with 4 random clips.
3. Throttle: `maxInstances` 3, profile cooldown ~0.1 s, low priority (below weapons/alarms); start limiter already global.

### 8. Per-unit engine loops (ships + squadrons)
1. `AudioShipComponent` resolves `GetUnit(ShipType)` once; `UpdateAudio` holds that engine via `TryHoldEngine`.
2. Moving only: hold loop only when `MovementPhase.Moving` and speed > 0; otherwise let lease expire (fade).
3. Squadron: one engine loop per squadron (not per fighter) from squadron move state; register `SquadronType` in `SquadronInstaller` if missing.
4. Flyby-only folders (`DELTA`, `X`, `V19`): use clips as random loop pool; listen-check seams.
5. Caps: `engineVoices` total, per-ship cap unchanged; quieter priority than weapons.

### 9. Hardpoint-loss explosions
1. `Ship` subscribes to all `HardPointModels`; plays when a hardpoint goes alive → `IsDestroyed` via `ApplyDamage`. Ignore `Uninstall`, `SetHealth`, `Restore`, spawn.
2. `AudioShipComponent.PlayHardPointLost()` → unit explosion clip at ship position (Venator family → `Exp_Hp_Venator`, ISD/Victory family → `Exp_Star_Destroyer`, all others → `Exp_Cruiser`); per-ship cooldown ~1 s to merge multi-hardpoint bursts.
3. EditMode test: restore/uninstall do not trigger.

### 10. Alarms
- Common alarm replaced (step 1.2).
- `Lucrehulk` → `LUCREHULK_ALARM.wav` via per-unit alarm override; others use common alarm.

### 11. Verification
1. Compile; `unity` console clean; asset reserialize + `SaveAssets` per AGENTS rules.
2. EditMode: new state tests, ambience job tests, `ShipSfxData` coverage test (every `ShipType`/`SquadronType` resolves engine + explosion, no null clips), existing audio tests.
3. User in-game check: loudness balance, engine cap audibility, shield cooldown, ambience fade, announcer timing.

## Edge Cases
- Save load → `SuperWeaponModel.Restore`, `HardPointModel.Restore`, shield state → no announcements/explosions/shield cues.
- Enemy ship under fog or cloaked → no engine/explosion/shield/ambience contribution.
- Ship destroyed → `ReleaseShip` fades loops; ambience unregisters on `EntityRemoved`.
- Pause (`timeScale 0`) → ambience pauses; interface rejected click still plays (`ignoreListenerPause`).
- Many fighters moving → one loop per squadron, capped by `engineVoices`.
- Shields regen near 0 under fire → hysteresis + cooldown prevents flapping.

## Resolved Decisions (2026-10-22, user)
1. **Wrong-click triggers** → invalid super-weapon target, rejected ability target, unaffordable purchase click. Nothing else.
2. **Explosion fallback** → unmapped ship → `Exp_Cruiser` (2 clips).
3. **"Ability canceled"** → player toggle-off, targeting cancel, and natural ending; unit must be visible.
4. **Shield up/down audience** → every visible ship.
5. **Inferred mappings** → confirmed: `Dispatcher → PROVIDENCE`, TIE variants → `TIE`, ISD/Victory family → `Exp_Star_Destroyer`.
6. **"Orbital weapon available"** → Ready and fireable (after build or cooldown).

## TODO
- [x] Resolve open decisions
- [x] 1 Assets and data
- [x] 2 Wrong click
- [x] 3 Orbital announcer
- [x] 4 Ambience loops (Burst)
- [x] 5 Ability toggle SFX
- [x] 6 Shields up/down
- [x] 7 Shield impact SFX
- [x] 8 Per-unit engine loops
- [x] 9 Hardpoint-loss explosions
- [x] 10 Alarms
- [ ] 11 Verification — automated checks complete; user listening acceptance remains
  - [x] Compile, import settings, saved data/prefab references, focused EditMode tests
  - [ ] User in-game listening: loudness, engine/flyby seams, shield cadence, ambience fade, announcer timing

## Implementation — 2026-10-22
- Imported 33 mapped clips; SHA-256 matches source for all 33. Common alarm GUID retained; unreferenced `ShipAlarm2.mp3` removed. Existing orbital clips reused.
- Added interface audio/announcer priority, shared engine cap 6, unit maps/fallbacks, ability and shield cues, damage-only hardpoint explosions, squadron engines, and Burst ambience.
- `SceneContext.prefab` references `InterfaceSfxData` and `ShipAmbienceData`; assets imported, explicitly reserialized and saved.
- Common alarm uses one shared profile; only Lucrehulk enables the unit alarm override. Toggle priority 60; existing Start/End submitted first.
- Interface audio initializes before player scopes. Audio-created objects dispose correctly in EditMode.
- Verification: successful Unity compilation; 73 distinct recorded EditMode cases passed across Audio, ShipSfx, ShipAmbience, ShipAbilityServiceTests, FactionUnitAvailabilityTests, RealEntityCompositionTests and VContainerCompositionTests. Audio runner summary reports 20 while its result list contains 19; count uses recorded names.
- Composition retry passed 15/15 after an initial CLI main-thread timeout polluted a test log. Concurrent unrelated test runs were excluded from evidence.
- No audio import/serialization errors observed. Shared Editor history also contains unrelated `HardPointExplosionPool` particle teardown errors; no new errors at final console cursor.
- No agent Play Mode or listening check. Plan remains active only for user in-game acceptance; no runtime loudness/seam claim.
