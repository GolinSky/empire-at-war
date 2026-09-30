---
category: Refactoring
status: in-progress
created: 2026-09-30
---
# Audio System Refactor Plan

## Goal
- `IAudioService` = playback primitive only: callers pass `AudioSource` + `AudioClip`; no enums, ids, music, scenes, alarms.
- `IMusicService` = music/ambience: data, scene swap, faction, fades.
- `IShipSfxService` = one scene service for all ship SFX (weapons, engine, abilities, hyperspace, alarm, voice) that owns audibility + pollution control and may **refuse** requests.
- Hard rule: SFX of on-screen ships stay audible at max zoom (camera height 940).

## Current Problems (verified 2026-09-30)

### Architecture
- `AudioService` mixes music playlist/fade, scene events, `IGameModelObserver` faction lookup, dialog source, a 2 s global SFX throttle and alarm throttle (`CanPlayAlarm`/`RegisterAlarmPlaying`).
- `AudioType` enum has one value (`Dialog`) and `PlayOneShot` ignores it.
- MVP split with no real view: `WeaponAudioView`/`IWeaponAudioView`/`WeaponAudioPresenter`, `ShipSfxView`/`IShipSfxView` (8 members)/`ShipSfxPresenter`. Views only wrap `AudioSource` calls.
- `WeaponAudioBudget` keys voices by `object[] _owners` / `object[] _sounds` → untyped; any object accepted; `ReferenceEquals` only.
- `ShipEngineAudioModel` is a smoothing state tracker named as a model; `AudioShipModel` only re-raises `OneShot` enum events; `AudioShipDialogModel` returns `(FactionType, ClipType, int)` tuples so data can index them back → 5 `Random`s + tuple round-trip for "pick random clip".
- `ShipInstaller.BindAudio` uses `FromNewScriptableObject` → clones `AudioShipData` and `AudioShipDialogData` **per ship**; each clone sync-loads the Addressable hyperspace clip and releases it in `LateDispose`.
- Ship audio wiring split across `AudioShipComponent` (alarm, hyperspace, weapon forwarding) + per-ship `ShipSfxPresenter` (engine, abilities) + scene `WeaponAudioPresenter` (weapons). Pause handled 3× via `Time.timeScale == 0f` polling.
- `AudioShipData.backgroundClips` / `GetAmbientClip()` unused.

### Why SFX is not heard
- Weapon + ShipSfx gain = `1 - InverseLerp(min, max, worldDistanceToCameraFocus)` with world units `20–120` (weapons) / `15–100` (ship SFX). At max zoom (h=940, pitch 55°, FOV 29.9°) the visible half-width is ≈550 u → everything >120 u from screen centre is silent while visible.
- Zoom gain `sqrt(180 / height)` → 0.44 at h=940, multiplied on top of distance attenuation.
- `AudioViewComponent.prefab` (alarm + hyperspace, on 11 ship prefabs) uses 3D sources, Log rolloff: alarm `minDistance 1` → ≈0.001 volume at listener distance ≈1150 u; hyperspace `minDistance 300` → ≈0.26.
- Voice starvation: `m_RealVoiceCount: 32`. Every ship instantiates `ShipSfx.prefab` (4 sources) + 2 alarm/hyperspace sources; ability execution loops call `Play()` even at gain 0 → hold real voices. `MusicSource`/`AudioDialogSource` have priority **256 (lowest)** → music/voice are first to be virtualized.
- Mixer has only `Master`; no groups/exposed params → no category ducking or volume settings.

## Decision

### Target structure
| Type | Kind | Responsibility |
|---|---|---|
| `IAudioService` / `AudioService` | ProjectContext service | `Play(source, clip, volume, loop)`, `PlayOneShot(source, clip, volume)`, `Stop(source)`, `Pause(source)`, `UnPause(source)`, `SetGamePaused(bool)` → `AudioListener.pause`. Nothing else. |
| `IMusicService` / `MusicService` | ProjectContext service | Owns `MusicSource` (`ignoreListenerPause = true`), `MusicAudioData`, scene activation, faction lookup, random track + fade. Plays through `IAudioService`. Optional battle ambience bed moves here. |
| `IShipSfxService` / `ShipSfxService` | Skirmish scene service, `ITickable` | Single entry for ship SFX **and** the only place that decides play / refuse / steal: audibility, priority, caps, cooldowns. Owns source pool + voice slots, follows emitters, fades, expires loop leases. No separate budget/audibility classes. |
| `ShipSfxSources` | MonoBehaviour (scene prefab) | `[SerializeField] AudioSource[] sfx` pool + `[SerializeField] AudioSource voice`. Holder only. Not a view, no interface, no logic. |
| `ShipSfxData` | ScriptableObject | All ship clips as `SfxProfile`s, caps, audibility curve values. One instance, bound once. |
| `SfxProfile` | `[Serializable]` data | `clip`/`clips`, `volume`, `priority`, `maxInstances`, `loop`, `cooldown`. |
| `AudioShipComponent` | MonoComponent (single, no MVP split) | Per-ship requester: weapons, engine, abilities, hyperspace, alarm. Keeps per-ship state (`ShipEngineAudioState`, ability states, alarm timer). |
| `AudioDialogShipComponent` | MonoComponent | Local-player voice lines via `IShipSfxService.TryPlayVoice`. |

- Why: one owner of mixing/budget → pollution control is possible; no per-ship sources → no Unity voice starvation.
- Avoid: views/presenters for audio, `object` keys, enums as audio ids, per-ship `ScriptableObject` clones, 3D rolloff for RTS SFX.

### `IShipSfxService` API (request = may be refused)
```csharp
public interface IShipSfxService
{
    // false → culled (inaudible / budget / cooldown). Callers must not assume playback.
    bool TryPlayOneShot(IEntity ship, SfxProfile sfx, Vector3 position);
    bool TryPlayWeaponShot(IEntity ship, WeaponProfile weapon, Transform muzzle);
    // Lease: loop keeps playing only while refreshed each tick; otherwise fades out.
    bool TryHoldLoop(IEntity ship, SfxProfile loop, Vector3 position, float volumeScale, float pitch);
    bool TryPlayVoice(AudioClip clip);
    void ReleaseShip(IEntity ship);
}
```
- `Try*` naming + `bool` result makes dropping explicit at every call site.
- `AudioShipComponent` never touches an `AudioSource`.

### Pollution control (inside `ShipSfxService`)
- Decision: `WeaponAudioBudget` is deleted, not ported. Its rules become private logic of `ShipSfxService`.
- Voice slot = private struct in the service: `IEntity owner`, `SfxProfile sound`, `Transform emitter`, `float endsAt`, `float gain`, `bool loop`. Typed; no `object`.
- Effective priority = `profile.priority × audibleGain`; a new request steals the lowest-effective voice only if it is higher.
- Caps from `ShipSfxData`: total pool voices (start 20 of 32 real), per ship (2), per sound (`maxInstances`), starts per 60 ms window (3), loop leases (engines ≤ 4 nearest).
- Per-profile `cooldown` replaces `AudioService` alarm/SFX throttles.
- Default priority order: voice > alarm > hyperspace > ability cue > heavy weapon > ability loop > light weapon > engine.
- Unity `AudioSource.priority`: voice 0, music 16, ship pool 128; UI later.

### Voice lines (2D, no audibility)
- Voice lines are 2D: `spatialBlend 0`, no pan, no distance/zoom/viewport gain. Unit replies play for the local player's selected ship wherever the camera is.
- Dedicated `ShipSfxSources.voice` source; never taken from the SFX pool and never stolen.
- Rule in service: one voice line at a time; new request refused while playing or within `voiceCooldown` (current 2 s); `Sfx` group ducked while voice plays (step 7).

### Audibility (private in `ShipSfxService`, SFX only)
- Measure in **viewport space**, not world units → zoom-independent.
- `edge = distance of viewport point from (0.5, 0.5)`, normalized so screen corner = 1.
- On screen: `gain = Lerp(1, edgeGain, edge)`; off screen: fade to 0 over `offscreenMargin` viewport units.
- Zoom: `zoomGain = Lerp(1, minZoomGain, InverseLerp(ZoomRange.Min, ZoomRange.Max, cameraHeight))`; `CameraData.ZoomRange` = 60–940.
- Start values: `edgeGain 0.45`, `offscreenMargin 0.15`, `minZoomGain 0.6` → corner at max zoom ≈ 0.27 × profile volume (> 0).
- Pan unchanged: `Clamp((viewport.x − 0.5) × 1.6, −0.8, 0.8)`. All pool sources 2D, `dopplerLevel 0`.
- Enemy fog rule from `ShipSfxPresenter` (`MIN_ENEMY_VISIBILITY 0.5`) moves into the service.

## Implementation

1. [x] **AudioService split**
   - New `IMusicService`/`MusicService` from music code in `AudioService` (scene swap, fade, `MusicAudioData`, `MusicData`).
   - Reduce `AudioService` to playback API; add `SetGamePaused` (`AudioListener.pause`); set `ignoreListenerPause` on music/voice sources.
   - Delete `AudioType`, `CanPlayAlarm`, `RegisterAlarmPlaying`, dialog source from `AudioService`.
   - Bind both in `ProjectContextInstaller`. Verify: menu → skirmish → menu music swaps + fades.
2. [x] **ShipSfx data types**
   - `SfxProfile` (`[Serializable]`).
   - Rename `ShipEngineAudioModel` → `ShipEngineAudioState`; move to `Components/Ship/Audio`.
3. [x] **ShipSfxData consolidation**
   - Merge weapon profiles + distances from `AudioShipData`, ability/engine from `ShipSfxData`, alarm + hyperspace (direct `AudioClip`, no Addressable per-ship load), voice sets.
   - Replace `AudioShipDialogData` 5 dictionaries + `ClipType` enum with `DictionaryWrapper<FactionType, ShipVoiceSet>` (`RandomAudioClips` per line).
   - Migrate `.asset` values through Unity API; save + reserialize changed paths only.
4. [x] **ShipSfxService + ShipSfxSources**
   - Scene prefab `ShipSfxSources` (pool of 2D sources, priority 128); bind service in `SkirmishServiceInstaller`.
   - Implement decide-to-play in the service: audibility → cooldown → caps → priority/steal → assign slot.
   - Port loop-lease/fade/emitter-follow from `WeaponAudioPresenter.Tick`; unify weapon loops, engine, ability execution as leases.
   - Voice path: dedicated 2D source, single-line rule, no audibility.
   - If the service exceeds 200 lines, report it and propose a split; do not split without approval.
   - Stop sources at gain < `MIN_AUDIBLE_GAIN` so they release real voices.
5. [x] **Ship components**
   - `AudioShipComponent`: injects `IShipSfxService`, `ShipSfxData`; ability-state switch + engine logic moved from `ShipSfxPresenter`; reads movement/abilities via narrow read-only sibling interfaces; `Ship` wires events (entity rule).
   - `AudioDialogShipComponent`: resolve faction `ShipVoiceSet` once; call `TryPlayVoice`.
   - `ShipInstaller.BindAudio`: bind shared data once (`FromInstance`), no `ShipSfx` prefab, no presenters.
6. [x] **Delete**: `IShipSfxView`, `ShipSfxView`, `ShipSfxPresenter`, `IWeaponAudioView`, `WeaponAudioView`, `WeaponAudioPresenter`, `WeaponAudioBudget`, `AudioType`, `AudioShipModel`, `IAudioShipModelObserver`, `AudioShipDialogModel`, `IAudioShipDialogModelObserver`, `ShipSfx.prefab`, `WeaponAudio.prefab`, alarm/hyperspace sources in `AudioViewComponent.prefab`, unused `backgroundClips`.
7. [x] **Mixer + source settings**
   - Groups: `Music`, `Voice`, `Sfx`; exposed volume params for later settings UI. Assign groups on all sources.
   - Fix priorities (music/voice no longer 256).
8. [ ] **Manual verification (skirmish)**
   - Max zoom (h=940): weapons, engine, hyperspace audible at screen centre **and** corner.
   - Min zoom (h=60): no clipping; mix normaliser holds.
   - Large battle (≥20 ships firing): voices ≤ pool size; Unity profiler "Playing Audio Sources" ≤ 32; music + voice never cut.
   - Pause (timeScale 0) pauses SFX, not music. Scene exit: no errors, no orphan sources.

## Files
- `Assets/Scripts/Services/Audio/*` (service, music, ship SFX)
- `Assets/Scripts/Components/Ship/Audio/*`
- `Assets/Scripts/Entities/Ship/Ship.cs`, `ShipInstaller.cs`
- `Assets/Scripts/Services/SceneContext/ProjectContextInstaller.cs`, `Skirmish/SkirmishServiceInstaller.cs`
- `Assets/Prefabs/View/Audio/*.prefab`, `Assets/Prefabs/View/ViewComponents/AudioViewComponent.prefab`
- `Assets/Settings/Data/Models/Audio/AudioShipData.asset`, `ShipSfxData.asset`, `AudioShipDialogData`
- `Assets/Audio/AudioMixer.mixer`, `ProjectSettings/AudioManager.asset` (real voices 32)

## Edge Cases
- Ship destroyed mid-loop → `ReleaseShip` fades its voices; muzzle `Transform` destroyed → voice stops.
- Scene teardown disables pool sources → service disposes before sources (no `Deactivated` event plumbing).
- Enemy under fog → gain 0 → request refused (no info leak).
- Voice lines: local player only (existing `ShipInstaller` rule); own 2D source, never stolen by SFX.
- `timeScale` 0 during request → refuse one-shots; leases keep state.

## Implementation Evidence (2026-09-30)
- Steps 1–7 implemented. Plan remains active until step 8 acceptance is verified.
- Unity 6000.4.7f1: `recompile_status` reports `completed`, `failed=false`, no compiler errors; console has no errors after migration. Automated tests and Play Mode were not run.
- Unity API migration preserved 14 weapon profiles, 7 ability profiles, and 2 faction voice sets. All 209 retained clip GUIDs are present; no references to retired audio GUIDs remain. Original volume values retained; restore cues retain the previous ×0.7 gain.
- Shared `ShipSfxData` and `ShipSfxSources` prefab are explicitly assigned in `SceneContext.prefab`; no per-ship data clones or audio sources.
- Serialized pool: 20 × 2D sources, Doppler 0, priority 128; dedicated voice priority 0; music priority 16. `AudioViewComponent.prefab` has 0 sources.
- Admission: 2 voices/ship, 4 engines, 3 starts/60 ms; per-profile caps/cooldowns; higher effective priority can steal. Engine leases compete by viewport gain, retaining the nearest audible engines.
- Curves retained as starting values: edge 0.45, off-screen margin 0.15, max-zoom gain 0.6 → calculated corner gain 0.27 before profile volume/mix normalization. This is a code/config result, not listening evidence.
- Mixer: `Music`, `Voice`, `Sfx`; `Sfx/Ducked` isolates voice ducking from later category-volume settings. Exposed: `MasterVolume`, `MusicVolume`, `VoiceVolume`, `SfxVolume`, `SfxDuckVolume`.
- Voice volume 0.5; SFX duck gain 0.4 (≈−7.96 dB). Music and voice set `ignoreListenerPause=true` at runtime; one scene service handles pause.
- `Ship` forwards weapon/ability events and ticks audio requests. Movement is read through `IShipEngineAudioObserver`; components implement `IMonoComponent` without a dummy model.
- `ShipSfxService` is 257 lines (exceeds 200). Kept as the single admission/mixing owner per this plan; possible future split: source playback/lifetime adapter and admission policy, only after approval.
- Obsolete audio assets and script types removed through Unity; deleted Addressables entries/mappings removed without changing group/folder structure. Changed assets saved and selectively reserialized.

## TODO / Open
- Step 8: listen at heights 60/940, exercise ≥20 ships firing, record profiler source/voice counts, verify pause, scene music fades, and exit teardown.
- Verification blocker: open `Assets/Scenes/MainMenuScene.unity` has unsaved changes. Did not enter Play Mode, save, reload, or discard that scene.
- Tune starting values after listening; no final tuning evidence yet.
