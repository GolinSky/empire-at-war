---
type: engineering-reference
system:
  - TacticalMarkers
  - Networking
  - EnemyAi
  - UI
verified: 2026-11-12
verification: Unity compilation and saved asset checks; authorized EditMode
  BattleInput 296/296, Ui 276/276, Networking 30/30, EnemyAi 360/360, Core
  151/151. In-game/two-client acceptance pending.
---
# Tactical Markers

## Rules

- Markers communicate intent; `PlaceMarkerCommand` never issues unit orders. Allied AI evaluates requests before its task-force executor acts.
- `TacticalMarkerRegistry` owns authority validation, per-player caps/rates, map clamping, repeat refresh, lifetime and target tracking. Client registries mirror host points; never read target transforms locally.
- `HostBroadcast.ToTeam` filters loaded connections by player alliance before sending. Enemy clients never receive marker or reply payloads.
- Hidden targets retain their last known point; visible tracking resumes without exposing hidden movement. Dead targets detach. `MarkerResolutionWatcher` runs on authority only.
- Marker time uses `BattleClock.Elapsed`; pause freezes lifetime. `BattleClock.Restored` clears markers, rates and AI marker state. Markers have no save payload.
- Registry clears on Loading/Ended and clock restore; marker placements and client additions are rejected after Ended.
- Capture-disabled modes inject empty `IReadOnlyList<ICaptureSiteDirectory>` collections; do not require a site singleton.

## Implementation

- Tap rebindable `Battle/PlaceMarker` (`B`) → Look. Hold >0.2 s → six-slice radial; hub release, Esc or command/RMB cancels. Modal input lock and cinematic camera disable placement.
- `MarkerRadialModel` owns selection; `MarkerInputController` submits commands; `MarkerRadialUi` renders state. `WorldMarkerController` pools world views (each `WorldMarkerView` destroys itself) and posts focusable notifications.
- Minimap marks use `MiniMapData` / `MarkType.TacticalLook` through `TacticalRetreat`; colored sprites bypass player-color multiplication. Arrival tweens are released with their pooled `MarkView`.
- `AllyAiMarkerSource` contributes Capture/Defend objectives and Look scouting; Attack/Rally/Retreat use force/intel/priority and existing route-safety checks. Replies: Accepted, Delayed, Declined.
- `AllyAiMarkerEmitter` reports capital contacts, station/site threats, assault staging/attack and retreat. No allied human → no outgoing AI communications; capture completion is an info card without camera focus.
- `AuthorityCommandSink` routes through `PlayerCommandHandlers.TacticalMarkers`; registry registers in `Initialize`, unregisters in `Dispose`. Direct registry injection into the sink creates a cycle through capture-site directories.
- Site target IDs encode flattened directory indices; `PointId` is not unique across mines and satellites. Use `TacticalMarkerTargets.GetSiteId`.
- Capture/Defend requests bias existing objectives after ordinary sources, preserving identity, radius and progress; area-only requests use radius 0.
- Esc focus begins on B press. Radial cancel closes in `LateTick`, preserving the blocker through pointer processing to prevent RMB move orders.
- `SkirmishScope` binds settings/registry; role registrations select authority watcher or client mirror transport. `AiPlayerScope` binds AI source/emitter. Simulation omits marker UI.

## Important Values

- Lifetimes: Look/Attack/Retreat 8 s; Capture/Defend 30 s; Rally 15 s. Resolved objectives remove Capture/Defend early.
- Per-player cap 4; rate 3 placements / 2 battle seconds; same-type repeat within 50 units and 2 s refreshes identity.
- Authority target updates ≤5 Hz; objective resolution checks ≤2 Hz. AI emissions ≤1 marker / 10 battle seconds.

## Edge Cases

- Sprite import must explicitly select `SpriteImportMode.Single`; Sprite texture type alone can leave Multiple mode with no Sprite subasset.
- Create `MaterialPropertyBlock` in `WorldMarkerView.Awake`, never a MonoBehaviour field initializer.
- `AudioMixer.FindMatchingGroups("Sfx")` includes child `Ducked`; select exact group name `Sfx`.
- Pooled world views use `Destroy` in Play Mode and `DestroyImmediate` in EditMode composition teardown.
- World marker hierarchy uses `WorldUi` layer; radial uses `UI`. Preserve view/presenter lifecycle and pooled tween cleanup.

## Files

- Plan: [[Done/Features/Tactical_Markers_Plan]]; acceptance: [[TODOs/User_Acceptance_Queue]].
- Runtime: `Assets/Scripts/Services/TacticalMarkers/`, `Assets/Scripts/Entities/TacticalMarkers/`, `Assets/Scripts/Services/Networking/*TacticalMarker*`.
- Settings/prefabs: `Assets/Settings/Data/TacticalMarkers/`, `Assets/Prefabs/Ui/TacticalMarkers/`; binding owner `Assets/Prefabs/View/LifetimeScopes/SceneContext.prefab`.
- Assets: `Assets/Art/Textures/Ui/TacticalMarkers/`, `Assets/Art/Models/Vfx/TacticalMarkers/`, `Assets/Audio/SFX/TacticalMarkers/`, `Assets/Art/Shaders/TacticalMarker.shader`.
- Authoring: `Tools/TacticalMarkers/generate_art.py`; `TacticalMarkerAssetBuilder.Build()` / `Validate()`. Art preview: `Logs/TacticalMarkers/marker-art-preview.png`.
