---
category: Refactoring
status: done
created: 2026-11-11
updated: 2026-11-11
completed: 2026-11-11
acceptance: pending-user
---
# Shared Invite Friends Popup

## Goal

- Phase 1: replace the queued accept popup with a small popup core plus feature popup services (wrappers).
- Phase 2: move the Invite Friends picker out of both lobbies into one shared popup served by a Steam popup service.
- Required case: picker open → friend's invite dialog opens on top → answer/Escape returns to the picker with its selection intact.

## Decision

- Design and rules: [[Architecture/POPUP_SYSTEM]]. That note is the contract; this plan is the execution.
- Chosen: core `IPopupService` (stack, handles, Escape priority, shared blocker, lifetime) + wrappers per feature area (`IConfirmPopupService`, `ISteamPopupService`).
- Results: `Accepted | Declined | Dismissed`. Any close not made by the player → `Dismissed`; callers never persist it (consent asks again).
- Popups stack immediately; no queue. Consent and invite dialogs may be visible together (intentional change).
- `ShowAccept` moves from `IPopupService` to `IConfirmPopupService`; no compatibility shim.
- Use existing `IUiService`, `UiController`, `UiCancelRouter` and `UiPlacement.modalSlot`. No new navigation framework.
- Avoid: Steam in the core; wrapper re-implementing stack/focus/Escape/blocker/lifetime; one wrapper per popup.

## Implementation

### Verified baseline — 2026-11-11

- `PopupService` queues accept dialogs via singleton `AcceptPopupUiController : IPopupController` (one view, one callback; `LateDispose` drops the callback).
- `IPopupService.ShowAccept` callers: `MainMenuUiController.RequestUsageDataConsent`, `OnlineRouteUiController.ShowInvitePopup`; stub in `MainMenuUiControllerLifecycleTests`.
- `UiCancelRouter` dispatches Escape to the most recently *focused* handler. `OnlineRouteUiController.Route()` → `LobbyScreen.Show()` / `Open()` call `Focus()` and can outrank an open popup.
- `AcceptPopupUi.prefab` has its own full-screen `Blocker`. Popup canvas sort 20.
- `TMP_Dropdown.Show()` list canvas sorts at 30000 → above every popup. `SteamInviteView.RefreshAvatars()` re-shows an expanded list on avatar load.
- `UiService.CreateUi<T>` loads address `typeof(T).Name` (`AssetGroups/Ui.asset`).
- `UiRegistrations.Register` runs in `MainMenuScope` and `SkirmishScope`; `OnlineMatchService` lives in `ProjectScope`.
- `SkirmishUi.prefab` / `SurvivalUi.prefab` embed `SteamInvites` (`steamInvites` field); each online lobby controller creates a `SteamInvitePresenter` and intercepts Escape via `TryClose()`.
- `Tools/BattleBuilder/BuildSteamInviteUi.cs` (`Run`, `Build<T>`, `UpgradeAvatars`) builds both embedded copies; `SteamInviteUiPrefabTests` (`Ui`) expects them.

### Phase 1 — Popup core + Confirm wrapper

- [x] Core contract: `PopupHandle`, popup content contract (open, covered/uncovered, cancel, dispose with close reason), `IPopupService.Open/Close`. Finalize names; update [[Architecture/POPUP_SYSTEM]] table.
- [x] `PopupService`: ordered stack, immediate open on top, `Close(handle)` for top or covered entries, closed-handle close is a no-op, teardown dismisses top → bottom.
- [x] Escape priority: add a popup cancel layer to `UiCancelRouter` dispatched before screens; screens' later `Focus()` never outranks it. Empty stack → existing fallthrough.
- [x] Shared blocker (dedicated, reusable): one core-owned blocker view in `modalSlot`, kept directly under the top popup, hidden when empty, click does nothing. Prefab + `UiPlacement` + `Ui.asset` entry.
- [x] Remove the `Blocker` child from `AcceptPopupUi.prefab` once the shared blocker covers it.
- [x] Covered/uncovered notifications from the core to the popup below/above the top.
- [x] Confirm wrapper: `IConfirmPopupService.ShowAccept(AcceptPopupContent, Action<ConfirmPopupResult>)`; per-instance accept controller replaces singleton `AcceptPopupUiController` / `IPopupController`. Escape → `Declined`; non-player close → `Dismissed`.
- [x] Migrate callers: consent persists only `Accepted`/`Declined`; online game-invite dialog ignores `Dismissed`. Update `PopupServiceStub`.
- [x] Registrations: core + confirm wrapper in `UiRegistrations`.
- [x] Verify by source review: no content/Steam types in core, exactly-once completion, `Dismissed` on every non-player close, sibling/blocker order matches the stack, removing a covered popup keeps top focus.
- [x] Tests (passed): simultaneous popups, independent results, Escape reaches top after a later screen `Focus()`, covered removal, teardown → `Dismissed`, consent not saved on `Dismissed`.

### Phase 2 — Steam popup service + shared invite picker

- [x] `SteamInvitePopupUi.prefab` in `Assets/Prefabs/Ui/Popups` from the current picker controls/styling; no own blocker. `UiPlacement` → `modalSlot`; `Ui.asset` address `SteamInvitePopupUi`.
- [x] `SteamInvitePopupUi : BaseUi` + per-instance controller under `Assets/Scripts/Entities/Popups`; reuse/adapt `SteamInvitePresenter` / `SteamInviteView`.
- [x] Covered: collapse `friends` dropdown, block `RefreshAvatars()` re-expanding it; keep selection. Uncovered: no reset.
- [x] `ISteamPopupService`: `OpenInvitePicker()` → `PopupHandle` (reuse if already open; no second `_steam.InviteFriends()`), `ShowInviteReceived(from, Action<ConfirmPopupResult>)` via `IConfirmPopupService`. Register in `MainMenuScope` only.
- [x] `OnlineRouteUiController` uses `ShowInviteReceived`.
- [x] Both online lobby controllers: inject `ISteamPopupService`; Invite Friends opens the picker; keep handle; close it on leave, route hide, session loss and match transition. Remove `SteamInvitePresenter` ownership and `TryClose()` Escape interception.
- [x] Remove `steamInvites` fields/properties from `SkirmishUi` / `SurvivalUi` and the embedded `SteamInvites` hierarchies; keep Invite Friends buttons, `InviteRequested`, `SetInviteAvailable`.
- [x] `BuildSteamInviteUi.Run`, `Build<T>`, `UpgradeAvatars` author/upgrade the shared prefab only.
- [x] Preserve: rich-text-disabled names, selection, refresh, avatars, empty-list message + disabled controls, failure status, close on success, overlay attempt and in-game fallback. Release avatar sprites and listeners on dispose.
- [x] Verify: no obsolete lobby bindings in source/prefabs; one picker asset after rebuild; balanced avatar subscriptions over repeated open/close.
- [x] Tests (passed): update `SteamInviteUiPrefabTests` for the standalone popup and no nested pickers; picker + invite stacking, covered dropdown collapse, lobby leave closes covered picker, duplicate open reuse.

### Persistence (both phases)

- [x] Official `unity` tooling for prefab/asset mutations: load/save/unload for structural prefab edits; targeted refresh/reserialize/save for direct edits; check Console for import/serialization errors.

### Verification — 2026-11-11

- Unity EditMode `Ui` 274 passed. `Core` 150 passed. `RealEntityCompositionTests` emitted four expected EditMode `Destroy` logs from `FactionUnitUi`.
- Regression coverage: stack order and blocker, Escape after screen focus, covered removal, teardown order, consent persistence, shared picker in both lobbies, duplicate open, dropdown collapse and restoration, avatar refresh and disposal.
- MainMenu Play Mode smoke check: services resolved from `MainMenuScope`, both new prefabs loaded from Addressables into Modal, and the top confirm popup completed as `Declined`.
- Unity saved both new prefabs and three migrated prefabs. The 1920×1080 preview confirmed the picker and layered invite. `BuildSteamInviteUi.Run` and `UpgradeAvatars` saved and bound a temporary standalone prefab, which was removed after verification.
- Consent teardown yields `Dismissed` and leaves the stored consent unanswered. Verified by teardown and settings persistence tests.

### Live Steam acceptance

- [ ] Two Steam accounts: receive an invite over the open picker. Later/Escape restores selection. Join switches lobbies and removes the old picker.
- [ ] Both lobby modes: verify overlay and in-game fallback, send success/failure, and disconnect while the picker is open.

### Close

- [x] Updated [[Architecture/POPUP_SYSTEM]] with implemented names, lifecycle, and stacking rules. Removed `flag/revisit`.
- [x] Updated [[TODOs/Features/Multiplayer_Plan]] to reference the shared picker and popup architecture.
- [x] Archived the plan, moved its backlog entry, and recorded live Steam acceptance in [[TODOs/User_Acceptance_Queue]].

## Rules

- Before implementation read [[Architecture/POPUP_SYSTEM]], [[Rules/UI_UX_GUIDELINES]], [[Rules/UI_CODE_BUILD_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]].
- Addressables: read [[AssetPipeline/Guides/ADDRESSABLES_BUNDLE_GUIDE]]; add entries only, preserve `Assets/AddressableAssetsData` structure.
- Popup placement follows [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan]] (Popup sort 20; Contextual → EndGame → Modal → Tooltip → Fade).
- Scope: popup system + invite migration only. No lobby redesign, networking rewrite or unrelated popup conversions.
- Test categories: `Ui`; `Core` if `BaseSystem/Ui` changes; `Networking` only if networking code changes.
- Tests disabled for Codex/Claude unless explicitly requested: author coverage, verify by source, report tests as not run.

## Files

- `Assets/Scripts/Services/Popups/{IPopupService,PopupService,IPopupController,AcceptPopupContent}.cs` + new core/confirm types
- `Assets/Scripts/Entities/Popups/{AcceptPopupUi,AcceptPopupUiController,IAcceptPopupPresenter}.cs` + blocker and Steam invite popup
- `Assets/Scripts/BaseSystem/Ui/{UiCancelRouter,UiController,UiPlacement}.cs`
- `Assets/Scripts/Entities/MainMenu/Main/MainMenuUiController.cs`
- `Assets/Scripts/Entities/MainMenu/Online/{OnlineRouteUiController,OnlineSkirmishLobbyUiController,OnlineSurvivalLobbyUiController,SteamInvitePresenter,SteamInviteView,ISteamInvitePresenter}.cs`
- `Assets/Scripts/Entities/MainMenu/Skirmish/SkirmishUi.cs`; `Assets/Scripts/Entities/MainMenu/Survival/SurvivalUi.cs`
- `Assets/Scripts/Services/VContainer/Registrations/UiRegistrations.cs`; `Assets/Scripts/Services/VContainer/Scopes/MainMenuScope.cs`
- `Assets/Prefabs/Ui/MainMenu/{SkirmishUi,SurvivalUi}.prefab`; `Assets/Prefabs/Ui/Popups/AcceptPopupUi.prefab`; `Assets/AddressableAssetsData/AssetGroups/Ui.asset`
- `Tools/BattleBuilder/BuildSteamInviteUi.cs`
- `Assets/Scripts/Tests/Editor/{SteamInviteUiPrefabTests,MainMenuUiControllerLifecycleTests}.cs`
- Related: [[TODOs/Features/Multiplayer_Plan]]; [[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan]]
