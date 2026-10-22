---
category: Features
status: in-progress
created: 2026-10-22
---
# Audio System Extension Plan - Research

Companion to [[TODOs/Features/Audio_System_Extension_Plan|Audio System Extension Plan]].

## Source Clips
Root `F:/Temp_audio/Republic_at_War_1129810972/Audio` (`$R`). Measured with Python `wave`, 2026-10-22.

| Use | Source | Length | Format |
|---|---|---|---|
| Wrong click | `$R/Interface/SFX/Misc/Bad_Sound/BAD_SOUND.WAV` | 0.15 s | 44.1 kHz mono |
| Orbital available | `$R/Interface/Voices/I000_RHD/Other/I000_RHD9000_ENG.WAV` | — | already imported |
| Orbital bought | `.../I000_RHD9001_ENG.WAV` | — | already imported |
| Orbital firing | `.../I000_RHD9002_ENG.WAV` | — | already imported |
| Ambient loop 1 | `$R/Shared/SFX/Ambient/Ship/amb_ship_loop_1.wav` | 4.02 s | 44.1 kHz stereo |
| Ambient loop 2 | `$R/Shared/SFX/Ambient/Ship/amb_ship_loop_2.wav` | 7.15 s | 44.1 kHz stereo |
| Ability activated | `$R/Space/SFX/Abilities/Controls/GUI_TOGGLE_17.WAV` | 3.67 s | mono |
| Ability canceled | `$R/Space/SFX/Abilities/Controls/GUI_TOGGLE_18.WAV` | 2.93 s | mono |
| Shields down | `$R/Space/SFX/Abilities/Shields/GUI_SHIELDS_DOWN_1.WAV` | 1.07 s | mono |
| Shields up | `$R/Space/SFX/Abilities/Shields/GUI_SHIELDS_UP_1.WAV` | 1.05 s | mono |
| Lucrehulk alarm | `$R/Space/SFX/Misc/Lucrehulk_Alarm/LUCREHULK_ALARM.wav` | 9.07 s | mono |
| Common alarm | `$R/Space/SFX/Misc/Ship_Alarm/SHIP_ALARM.WAV` | 6.69 s | 48 kHz mono |
| Shield impact ×4 | `$R/Space/SFX/Shields/Impacts_and_Destruction/EXP_SHIELD_ABSORB_1..4.WAV` | 1.06–1.71 s | mono |
| Engine fallback | `$R/Space/SFX/Engines/CALAMARI/EGL_CALAMARI_1.WAV` | 1.67 s | mono |

- `Assets/Audio/SFX/PlanetaryAttack/I000_rhd_900{0,1,2}_eng.wav` are byte-identical to the RHD sources (`cmp`) and referenced by no asset → reuse, do not reimport.
- `Assets/Audio/SFX/Alarm/SHIP_ALARM.WAV` **differs** from the source `SHIP_ALARM.WAV` (byte 5). Current `ShipSfxData.alarm.clips` = `ShipAlarm2.mp3` + old `SHIP_ALARM.WAV`, priority 90, cooldown 2 s, maxInstances 1.

## Engine Mapping (proposed)
Rule: folder/file name matches unit name → that folder; otherwise `CALAMARI/EGL_CALAMARI_1.WAV`. Loop clip = non-`_BY_` file when present; `_BY_` files are flybys and may not loop seamlessly.

| Unit | Folder | Loop clip | Match |
|---|---|---|---|
| `Venator`, `VenatorII`, `ImperialVenator` | `VENATOR` | `EGL_VENATOR_1` (1.43 s) | name |
| `Providence` | `PROVIDENCE` | `EGL_PROVIDENCE_1` | name |
| `Dispatcher` | `PROVIDENCE` | `EGL_PROVIDENCE_1` | class (Providence-class) — confirmed |
| `Malevolence` | `MALEVOLENCE` | `EGL_malevolence_1` | name |
| `NebulonB` | `NEBULON` | `EGL_NEBULON_FRIGATE_1` | name |
| `CorellianCorvette` | `CORVETTE` | `EGL_CORVETTE_1` | name |
| `MonCalCruiser`, `HomeOne`, `MC75Profundity`, `MC80Independence`, `MC50`, `MC30c` | `CALAMARI` | `EGL_CALAMARI_1` | class (= fallback anyway) |
| `Delta7` (squadron) | `DELTA` | `EGL_DELTA_BY_1/2` | name, flyby only |
| `V19Torrent` | `V19` | `EGL_V19_By_1..3` | name, flyby only |
| `Vulture` | `VULTURE` | `EGL_VULTURE_1` | name |
| `TIEFighter`, `TIEInterceptor`, `TIEBomber`, `TIEAvenger`, `TIEBrute`, `TIEPunisher` | `TIE` | `EGL_TIE_FIGHTER_1` | family — confirmed |
| `XWing` | `X` | `EGL_X_WING_BY_1/2` | name, flyby only |
| `YWing`, `YWingBomber` | `Y` | `EGL_Y_WING_1` | name |
| All other `ShipType` / `SquadronType` | `CALAMARI` | `EGL_CALAMARI_1` | fallback |

- Unused folders (no project unit): `E`, `FALCON`, `FREEFALL`, `G400`, `GARGANTUAN`, `GUARDIAN`, `HOUNDS`, `IG2000`, `NANTEX`, `SHUTTLE`, `SPINNING`, `STAR`, `STARBASE`, `TRACTOR`, `TRI`.
- `TRI` = Tri-fighter, not Lucrehulk/`Munificent` → no match.

## Explosion Mapping (proposed)
Folders under `$R/Space/SFX/Explosions`:
- Unit-named: `Exp_Hp_Venator` (1), `Exp_Star_Destroyer` (1), `Exp_Cruiser` (2), `Exp_X_Wing` (2), `Exp_A_Wing` (2), `Exp_Rebel_Fighter` (1), `Exp_Malevolence_Ion_Impact` (weapon impact, not hull).
- Hardpoint-type: `Exp_Hp_Bay`, `Exp_Hp_Engines`, `Exp_Hp_Grav`, `Exp_Hp_Shields` (1 each).
- Generic: `Exp_Metal_Break` (5), `Exp_Asteroid_Ship` (4), `Exp_Bombard`, `Exp_Planet`.

| Unit | Folder | Match |
|---|---|---|
| `Venator`, `VenatorII`, `ImperialVenator` | `Exp_Hp_Venator` | name |
| `ISDI`, `ISDII`, `ISDIII`, `Imperator`, `Victory`, `VictoryI`, `VictoryII`, `Tector`, `Executor`, `Allegiance` | `Exp_Star_Destroyer` | class — confirmed |
| Other ships | `Exp_Cruiser` (fallback) | confirmed |

- Fighters (`Exp_X_Wing`, `Exp_A_Wing`, `Exp_Rebel_Fighter`) only apply if squadrons get a fighter-lost sound; squadrons have no hardpoints.

## Code Hooks (verified 2026-10-22)
- `ShipSfxService` (292 lines): 20-slot pool, per-ship cap 2, engine cap `engineVoices=4` via `slot.Sound == _data.Engine`, global start limiter 3/0.06 s, per-`SfxProfile` cooldown, camera/zoom/fog gain.
- `ShipSfxData`: single `engine`, `acceleration`, `alarm`, `hyperspace`; abilities via `ShipAbilityAudioProfile` (Start/Execution/End/Restore).
- `AudioShipComponent.UpdateAudio` holds engine loop every frame scaled by `ShipEngineAudioState.Speed`; `HandleEnemyDetected` plays alarm.
- `Squadron.cs:263` plays weapon shots only; no engine audio.
- `ShipInstaller` registers `ShipType`; `SquadronInstaller` registers `ShipSfxHandle` (check `SquadronType` registration).
- `IHealthModelObserver`: `OnValueChanged`, `Shields`, `HasShields`, `IsLostShieldGenerator`, `HardPointModels`.
- `HardPointModel.OnHardPointHealthChanged` fires for `ApplyDamage`, `SetHealth`, `Uninstall`, `Repair`, `Restore`; `OnRestored` precedes restore change.
- Shield impact: `ImpactEffectPresenter.Play` → `IShieldTarget.ShowShieldImpact` → `HealthComponent.ShowShieldImpact:285` → `ShieldComponent.ShowImpact`.
- Super weapons: `SuperWeaponModel.OnStateChanged(type, state)`; `StartCharging` (bought) → `Charging`; `CompleteCharging` → `Ready`; `Tick` re-announces after cooldown; `Restore` (save load) also invokes it. `SuperWeaponPresenter.HandleTargetSubmitted` → `Consume` + `Fire`; rejects invalid target silently at line 190.
- Ability rejection: `ShipAbilityService.CanStart` (line 258) rejects ineligible targets.
- Toggle abilities: `ShipAbilityDefinition.IsToggle`; `ShipAbilityState` = `Ready/Active/Recovering`.
- Unit positions: `IEntityLocator.EntityAdded/EntityRemoved`; Burst precedent `RadarPositionJob` (`IJobParallelForTransform` → X/Z arrays).

## Implementation — 2026-10-22
- All mapped imports and code hooks implemented; source files unchanged. Current verification and remaining user listening checks: [[TODOs/Features/Audio_System_Extension_Plan]].
- Optional alarm override uses `ShipUnitAudioProfile.useAlarmOverride`; common alarms retain the shared profile cooldown/instance cap.
- Shield restore notification uses `HealthModel.OnRestored` through `IHealthAudioEvents`; hardpoint cue uses `HardPointModel.OnDestroyedByDamage`.
