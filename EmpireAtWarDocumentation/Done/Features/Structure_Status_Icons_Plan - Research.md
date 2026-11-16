---
category: Features
status: done
created: 2026-11-16
plan: "[[Done/Features/Structure_Status_Icons_Plan]]"
---
# Structure Status Icons — Source Research

## Goal

- Source snapshot for [[Done/Features/Structure_Status_Icons_Plan]], inspected 2026-11-16.
- Historical pre-implementation snapshot. Implementation/compilation and artwork evidence are recorded in the completed plan; tests and performance measurements remain pending.

## Implementation

### Mining and satellite lifecycle

- `Assets/Scripts/Services/CaptureSites/CaptureSitesSystem.cs`: `UpdateState` creates persistent site presenters and subscribes to `Captured`, `Lost`, `ConstructionCompleted`. Aggregate notifications lack the site identity/position required by icons.
- `Assets/Scripts/Entities/CaptureSites/CaptureSitePresenter.cs`: `SetConstructionProgress` detects operational completion; `ReleaseFacility` clears `FacilityId` and releases ownership. The site and its `Center`/`FacilityPosition` survive the spawned facility.
- `Assets/Scripts/Services/CaptureSites/SiteFacilityBuilder.cs`: `Build` connects construction progress and mining/platform/satellite `OnRelease` callbacks to the site.
- `Assets/Scripts/Entities/CaptureSites/CaptureSiteModel.cs`: actual destruction during construction or operation → neutral owner/state, zero progress, `Lost(previousOwner, type)`.
- Empty-site neutralization does **not** emit `Lost` or `Captured`. A new site-aware ownership notification must clear captured reminders without changing the existing destruction/announcer contract.
- `CaptureSitesSystem.TryCancel` closes build selection; it is not a construction-cancellation event.
- `Assets/Scripts/Services/BattleNotifications/CaptureSiteNotifier.cs`: capture/completion use local current owner; destruction uses local previous owner. Preserve this audience by default.
- `Assets/Scripts/Entities/CaptureSites/SiteFacilityType.cs`: `Mining`, `BattleAsteroid`, five satellite types. Mining sites can build `BattleAsteroid` (`SiteFacilityBuilder.Build` hooks its `OnRelease` too).
- Other `ICaptureSiteEvents` consumers: `Services/CaptureSites/CaptureSiteAnnouncer.cs`, `Services/TacticalMarkers/AllyAiMarkerEmitter.cs` (subscribes to a list of site-event sources). Keep their event contracts unchanged. (Validated 2026-11-16.)

### Relays, loading and online state

- `Assets/Scripts/Services/ReinforcementZones/ReinforcementZonesSystem.cs`: `TickZones` detects relay ownership changes. Existing `OwnershipChanged` lacks source and previous/new owner payload.
- Relays have capture/ownership only. User chose a 2 s displayed lifetime for their captured world icon.
- `Assets/Scripts/Services/Saves/CaptureSitesSaveSource.cs`: restores state and reconnects destruction/progress callbacks without replaying historical notifications.
- `Assets/Scripts/Services/Networking/States/CaptureSitesReplicatedState.cs`: `_hasBaseline` suppresses initial notifications; `ReplayHostChange` currently emits `SiteLost` on any owner → none transition.
- New destroyed-icon classification must inspect the previous constructing/operational state; empty-site neutralization is not destruction.
- Relay `RestoreZones` invokes `OwnershipChanged` on every snapshot. Deduplicate actual ownership transitions and suppress initial/save baseline effects.
- `OwnershipChanged` consumers: `MapNotifier`, `EnemyUnitCommander`, `AllyAiMarkerEmitter`, plus minimap polling. `EnemyUnitCommander` refreshes on restore, so keep that firing; add a separate per-relay transition event.

### Existing minimap

- `Assets/Scripts/Entities/MiniMap/Presenter/MiniMapAlertPresenter.cs`: `LateTick` polls site/relay owners; `PingOwnerChange` emits generic `Captured`/`Lost`. Startup snapshots owners to avoid load pings.
- The same presenter tracks local-entity damage for combat alerts. Migrating structure events must preserve the separate combat path.
- `Assets/Scripts/Entities/MiniMap/MiniMapUi.cs`: generic alert pool `MAX_ALERTS = 8`; when full, reuse the oldest. Current durations: captured 4 s, lost 5 s, combat 3 s.
- `Assets/Scripts/Entities/MiniMap/Ui/MiniMapAlertView.cs`: every active view has `Update`; uses unscaled time, pulses scale and fades during the final 40% of its duration. This does not provide a full 3 s hold followed by fading.
- `Assets/Scripts/Entities/MiniMap/Model/MiniMapData.cs`: `OnAlertAdded`/`AddAlert` carry alert type and position, without stable source identity or structure sprite.
- `Assets/Scripts/Entities/MiniMap/Presenter/CaptureSiteMiniMapPresenter.cs`: ordinary site markers choose `CaptureSite`/`SatelliteSite`; revealed operational sites hide their site marker. Keep this base-location behavior separate from transient event icons.
- `Assets/Prefabs/Ui/MiniMap/MiniMapUi.prefab` and `Assets/Settings/Data/Models/MiniMap/MiniMapData.asset`: implementation binding/configuration review targets; no serialized changes or visual checks made during planning.

### World layer reuse boundary

- `Assets/Scripts/Components/Squadron/Icon/BattleMarkerLayer.cs`: shared screen-space camera canvas; `Tick` casts registered `IBattleMarker` entries to `BattleMarker`, samples motion/visibility and runs grouping.
- `LateTick` hides markers in cinematic mode and delegates layout to `BattleMarkerProjection`. `IBattleMarker` exposes entity identity and selection hit rectangles.
- Structure notifications should reuse canvas placement through a separate passive collection, without extending selection/grouping or acquiring a live facility Transform.
- Current guidance records a 10 Hz shared marker snapshot. Structure state changes need event delivery; fixed-site projection can reuse cached results when the camera/viewport is unchanged.

## Files

### Existing tests reviewed for affected contracts

| Category | Fixture / contract | Required follow-up |
|---|---|---|
| Capture | `CaptureSiteModelTests.TickCapture_Neutralization_DoesNotAnnounceCaptureOrFacilityDestruction` | Preserve neutralization semantics; verify reminder clearing separately |
| Capture | `ConstructionProgress_CompletedFacility_BecomesOperationalAndLocksCapture` | Completion replaces captured once |
| Capture | `ReleaseFacility_DestroyedFacility_ReturnsSiteToNeutral`; `Satellite_RestoresConstructionAndReleasesAfterDestruction` | Cover both construction and operational destruction |
| Capture | `CaptureSitePresenterTests.ConstructionStartedEmitsOnlyAfterSuccessfulPaymentAndNotRestore` | Failed payment/restore must not start fresh event timers |
| Capture | `Capture_StatusRequiresVision_RingRemainsAvailable` | Existing progress UI keeps its fog rule; notification icons ignore fog independently |
| Capture | `ReinforcementZonesSystemTests.RelaySave_JsonRoundTripRestoresCaptureAndRelayServices`; `Relay_CaptureTransfersVisionAndKeepsItWithoutUnits` | Restore suppression and capture transition identity |
| Ui | `MiniMapUiLifecycleTests` | Extend coverage beyond current marker add/remove subscriptions to new event subscriptions and required icon bindings |
| Ui | `UiCanvasArchitectureTests.CaptureSites_KeepOnlyWorldStatusUi` | Preserve one existing site canvas and centralized notification rendering |
| Capture | `DefenseSatelliteContractTests` | Review serialized expectations if satellite assets/bindings change; no blanket prefab refactor |

- No existing fixture references `MiniMapAlertPresenter`; migrating its structure pings needs new `Ui` coverage (combat path kept).
- Test locations: `Assets/Scripts/Tests/Editor/Capture/`, `Assets/Scripts/Tests/Editor/Ui/`; inventory: `Tools/Jenkins/TEST_INVENTORY.md`.
- New deterministic cases: accumulated on-screen time, offscreen retention, fog independence, exact hold/fade boundaries, separate world/minimap expiry, latest-state replacement, actual-destruction classification, duplicate/initial snapshots, pooled reset and teardown.
- Use controlled elapsed-time inputs; no wall-clock sleeps, machine-speed assertions or test execution during implementation unless explicitly requested.
- `Networking` coverage is required if snapshot/replay code changes; inspect its live test fixtures at implementation time rather than assuming capture tests cover replication.

## Edge Cases

- One shared entry per source does not mean one shared expiry: world acknowledgement and minimap lifetime must remain independent.
- Reusing the generic eight-alert eviction policy can cut structure icons short during simultaneous events; prewarm from actual source count instead.
- Fog renderer visibility and destroyed facility lifetime cannot define notification visibility.
- Graphify query failed its freshness check on a busy `graphify-out/service/update.lock`; live source was used and the lock/configuration was not changed.

## TODO

- Implementation and visual/profiling acceptance remain in the parent plan.
- Update [[GameDesign/Capture_And_Structures]] only after behavior is implemented and verified; this research is not current gameplay policy.
- Tests were inspected, not run. No game files changed.
