---
category: Features
status: done
created: 2026-11-16
updated: 2026-11-16
design_state: implemented
completed: 2026-11-16
acceptance: pending-user
---
# Structure Status Icons

## Goal

- Show matching state icons above mining facilities, satellite pads and relays, and on the minimap.
- Make captured, built and destroyed states recognizable; preserve unseen world notifications through fog.
- Keep CPU work proportional to active notifications, with pooled views and event-driven state changes.

## Rules

### User decisions · 2026-11-16

| Structure / event | World icon lifetime | Minimap event icon lifetime |
|---|---|---|
| Mining / satellite captured | Until the player finishes building the facility; clear if ownership is lost | 3 s at full opacity, then fade |
| Mining / satellite built | Until displayed on screen for 2 s | 3 s at full opacity, then fade |
| Mining / satellite destroyed | Until displayed on screen for 2 s, including under fog | 3 s at full opacity, then fade |
| Relay captured | Until displayed on screen for 2 s | 3 s at full opacity, then fade |

- Fog does not hide these icons or gate their timers. A destroyed mine will commonly be under fog.
- World and minimap lifetimes are independent. Minimap expiry never consumes the world icon's unseen time.
- Relays currently have no build/destruction stage. Their captured icon uses the user's explicit 2 s exception.
- Use the same structure/state artwork in both places. Replace generic structure-event pings; retain ordinary minimap location/unit markers and unrelated combat/tactical alerts.

## Decision

### Planning defaults

- **Seen:** 2 s accumulated while the world icon is actually displayed inside the active battle-camera viewport. Offscreen/behind-camera or hidden world HUD → preserve remaining time; fog is irrelevant.
- **Captured until built:** retain the mining/satellite captured icon through construction; successful completion replaces it with built. Failed payment or selection cancellation does not clear it.
- **Audience:** local player's captures/builds and destruction of their facilities, matching existing notification filtering. Ownership at destruction is the previous owner.
- **Clock:** unscaled presentation seconds, independent of battle speed. Suspend a display's clock while that HUD is hidden/covered by a full-screen menu or cinematic mode.
- **Fade:** proposed minimap fade duration = 0.3 s after the complete 3 s hold; total = 3.3 s. The user specified the hold, not the fade duration.
- **Artwork:** one shared structure glyph plus captured/built/destroyed state treatment; distinguish states by shape as well as color. Reuse suitable existing art; final appearance remains a visual acceptance gate.
- **Battle Asteroid (user-confirmed 2026-11-16):** mining sites also build `SiteFacilityType.BattleAsteroid`. It follows the mining facility's captured/built/destroyed rules; glyph chosen by site kind (asteroid site vs satellite pad), not by facility type.
- These defaults are implementation choices, not additional confirmed user requirements.

### Ownership and presentation

- One small presentation-state owner maintains independent world/minimap timers keyed by stable site/relay identity; latest transition replaces the previous icon for that site.
- Pure C# state/timing rules receive explicit elapsed time and displayed-state inputs. A focused presenter adapts gameplay events, camera projection and UI lifecycle.
- Views render icons and alpha only; explicitly bind sprites/graphics. A shared serialized icon mapping keeps world/minimap artwork identical.
- Cache site position/offset independently of the spawned facility. A destroyed facility's released Transform must not own its pending notification.
- Reuse the existing battle HUD/canvas placement with a passive structure-icon collection. Do not push these notifications into ship/squadron grouping or selection: the current layer assumes concrete entity markers.
- Keep existing capture-progress visibility, selection, fog gameplay, capture rules, economy and relay mechanics unchanged.

## Implementation

1. [x] Add site-aware state notifications at existing lifecycle boundaries.
   - Carry stable source identity, kind, position, previous/current owner and the transition needed by the presentation owner; avoid duplicate string identifiers.
   - Mining/satellite: capture, completion, actual facility destruction, and empty-site ownership loss. Relay: capture/ownership transitions only.
   - Reuse authority/replication change paths; suppress initial/save baselines and repeated unchanged snapshots.
   - Add new site-aware events beside the existing ones; do not change `ICaptureSiteEvents` or relay `OwnershipChanged` signatures/firing. Current consumers: `CaptureSiteNotifier`, `CaptureSiteAnnouncer`, `AllyAiMarkerEmitter`, `MapNotifier`, `EnemyUnitCommander` (relies on `RestoreZones` firing `OwnershipChanged`).
   - Site events arrive from several `ICaptureSiteEvents` sources (injected as a list); subscribe to all of them.
   - Verify: empty-site neutralization clears captured without emitting destroyed; construction/operational destruction produces one notification.

2. [x] Implement deterministic presentation state and lifetimes.
   - One world icon and one minimap icon per source; refresh only on a new transition, never on recurring state reads.
   - Consume world time only when displayed; retain pending offscreen events. Keep minimap time independent of world visibility.
   - On startup/load, reconstruct locally owned non-operational mining/satellite reminders; do not replay historical built/destroyed/relay-captured events.
   - Verify: 1 s seen → offscreen → 1 s seen dismisses; 2 s under fog dismisses; 2 s offscreen does not.

3. [x] Add passive pooled world icons above stable site anchors.
   - Cache the battle camera; project active records in one batch. Use constant screen-size artwork and a clear vertical offset.
   - Render above fog, without fog sampling, per-icon canvases, raycasts, per-icon Update, or entity selection hit targets.
   - Verify: destruction survives facility release; HUD hiding does not consume unseen time; expiry releases the view.

4. [x] Integrate matching minimap icons.
   - Extend the minimap event/view path with source identity and shared icon mapping; hold 3 s, then fade for the proposed 0.3 s.
   - Replace generic capture/loss pings for migrated structure events so each transition appears once. Preserve unrelated combat and tactical marker behavior.
   - Pool independently from the existing eight generic alert slots: size from actual mine/pad/relay count during setup, at most one notification per source.
   - Verify: simultaneous events retain their full lifetime; same-site newer events replace older ones; fading restores the underlying normal marker.

5. [x] Wire lifecycle, assets and regression coverage.
   - Explicit bindings and battle-scope registration; unsubscribe and return all views on map replacement, scene exit and disposal.
   - Read project placement/asset-import guidance before creating folders or importing artwork. Import/save only affected assets with Unity tooling; inspect Console.
   - Review/update the fixtures in [[Done/Features/Structure_Status_Icons_Plan - Research#Files]]; add focused timing/event regressions.
   - Verify source and test diffs together, compilation, persisted prefab references and initial/repeated online snapshots.

6. [x] Validate presentation and CPU cost; close implementation ownership when complete.
   - User acceptance: mine + satellite capture/build/destruction; relay capture; fogged destruction; camera away/return; pause/menu/cinematic; burst events; map reload and online client.
   - Compare matched battle workloads with idle icons, many captured sites, a camera pan and simultaneous destruction; record source count and active/visible counts.
   - Extract verified behavior into [[GameDesign/Capture_And_Structures]] and reusable CPU/lifecycle rules into [[Rules/BATTLE_PERFORMANCE_GUIDELINES]] if warranted.
   - After implementation/source verification, queue remaining user-only visual/profiling/test gates under [[TODOs/User_Acceptance_Queue]] and complete the plan lifecycle.

## Important Values

- World transient acknowledgement: **2 s displayed**, independent of fog.
- Minimap: **3 s hold + proposed 0.3 s fade**; latest event restarts the source's minimap lifetime.
- Complexity target: O(active records) in one presentation loop; no scan over all entities, no all-pairs work.
- Allocation target: **0 B/frame from steady-state icon updates after warmup**. Measure it; this plan does not claim achieved performance.
- Prewarm views/buffers from actual source count. No runtime Instantiate/Destroy per event; no coroutine/tween allocation per icon.
- Stationary anchors: reproject on camera/viewport/layout changes or new events; cache projected eligibility while the camera is static. Advance only active clocks/fades.
- Set sprites, colors, visibility and transforms only when changed; disable raycast targets and avoid layout groups/material instances per icon.
- CPU millisecond budget: establish from a matched baseline before acceptance; no unmeasured speedup claim or Jobs/Burst dependency.

## Edge Cases

- Constructing facility destroyed → destroyed icon; ownership resets to neutral, so no captured reminder returns until recapture.
- Empty owned site neutralized → remove captured; never fabricate a destroyed event.
- Destroyed → recaptured → built before acknowledgement → newest state replaces stale state; no queued replay of old icons.
- Viewport accounting uses the icon's rendered position, not renderer visibility or player fog vision. Actual eye attention is not measurable.
- Save/first network snapshot → current reminders only. Subsequent real transitions notify once; unchanged relay snapshots do not restart timers.
- Minimap already faded while world event is offscreen → world event still waits for its 2 s display.
- Passive icons must not intercept selection, orders, tooltip picking or tutorial markers.

## Files

- [[Done/Features/Structure_Status_Icons_Plan - Research]] — verified hook points, current limitations and affected tests.
- [[Rules/UI_UX_GUIDELINES]], [[Rules/UI_CODE_BUILD_GUIDE]], [[Rules/BATTLE_PERFORMANCE_GUIDELINES]] — read during planning.
- [[TODOs/TODOs]] — active Features entry.

## Implementation Evidence
- Completed 2026-11-16: distinct capture/built/destroyed/ownership-loss events; per-map network baselines; pure independent clocks; passive source-sized pools; shared six-sprite artwork; combat alerts retained.
- World 48 px; minimap 26 px; world offset 42 px. Gold flag = captured, blue check = built, red cross = destroyed. Mining-site art also covers Battle Asteroid.
- Unity compilation completed with no errors. Saved `MiniMapData.asset` contains all six sprite references; actual view composites rendered to `Logs/StructureStatusIcons/structure-status-icons.png`.
- New deterministic `StructureStatusIconModelTests`, `StructureStatusIconSourcesTests`, `StructureStatusIconViewTests`; capture/relay fixture extensions cover event and online snapshot contracts. Tests not run under local override.
- Reviewed unchanged `MiniMapUiLifecycleTests`, `UiCanvasArchitectureTests`, capture model/defense-satellite contracts, and `RealEntityCompositionTests`: existing subscriptions, prefab layouts, capture rules and marker counts remain valid.
- User acceptance: live camera/fog/menu/cinematic/online/reload/burst scenarios; ordinary HUD panel overlap and small-icon readability; targeted test execution; matched CPU/GC profiling. No measured zero-allocation or CPU-speedup claim.
- Reusable behavior extracted to [[GameDesign/Capture_And_Structures]] and [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]. Open user gates → [[TODOs/User_Acceptance_Queue]].
