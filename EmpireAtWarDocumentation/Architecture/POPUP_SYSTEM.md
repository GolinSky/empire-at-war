---
type: engineering-reference
system:
  - ui
  - popups
design_state: implemented
verified: 2026-11-11
verification: Live source, Unity prefab and Addressables inspection, 1920×1080
  stacking preview, and MainMenu Play Mode service/asset smoke check.
---
# Popup System

## Goal

- One small, content-agnostic popup core: stack, Escape, click blocking, lifetime.
- Feature popup services (wrappers) add typed popups on top of the core, one per feature area.
- Required case: Invite Friends picker open → friend's invite dialog opens above it → answer/Escape returns to the picker unchanged.

## Decision

- Chosen: core `IPopupService` + feature wrappers (`IConfirmPopupService`, `ISteamPopupService`, future `IOnlinePopupService`).
- Why: core stays small and Steam-free; each wrapper owns its dependencies, scope and rules.
- Avoid: one generic service that grows a method per popup kind; per-lobby embedded popups; popups queuing behind each other.

## Rules — Core (`Assets/Scripts/Services/Popups/`)

- Owns: ordered stack, handles, sibling order in `UiPlacement.modalSlot`, shared blocker, Escape priority, covered/uncovered notification, close reasons, teardown.
- Knows no content types: no `AcceptPopupContent`, Steam, consent or lobby code.
- New popups open immediately on top. No queue.
- Only the top popup is interactive and receives Escape. Lower popups stay visible.
- Escape priority: the popup stack is a cancel layer above screens in `UiCancelRouter`. A screen calling `Focus()` while popups are open never outranks them.
- Closing the last popup returns Escape to the previously focused screen (router fallthrough). Core never calls screen `Focus()`.
- Shared blocker: one blocker view owned by the core, placed directly under the top popup, hidden when the stack is empty. Clicking it does nothing (no close). Popup prefabs carry no blocker.
- Each open request gets a `PopupHandle`. `Close(handle)` removes that entry (top or covered) without touching others. Closing an already-closed handle is a no-op — popups can self-complete before their owner leaves.
- Removing a covered popup never changes focus or order of the top popup.
- Scope teardown (`LateDispose`) dismisses all open popups top → bottom.

## Rules — Close reasons and results

- Player answer (button, Escape on top) → popup's own result.
- Any non-player close (owner `Close(handle)`, route leave/hide, session loss, match transition, scope teardown) → `Dismissed`.
- Every request completes exactly once.
- Callers treat `Dismissed` as "no answer": never persist it as a choice. Example: consent ignores `Dismissed` and asks again next launch.

```text
ConfirmPopupResult: Accepted | Declined | Dismissed
Escape on top confirm popup → Declined
Owner/route/teardown close  → Dismissed
```

## Rules — Wrappers (feature popup services)

- Wrappers only build content, create the per-instance popup controller and map results to their own types.
- Stack, focus, Escape, blocker and lifetime stay in the core. No wrapper re-implements them.
- One wrapper per feature area (Confirm, Steam, Online), not per popup.
- A wrapper may use the core and other wrappers' interfaces (e.g. Steam → Confirm for the invite-received dialog).
- Register a wrapper in the scope that owns its dependencies. Steam wrapper → `MainMenuScope` only; never `UiRegistrations` (also used by `SkirmishScope`).
- Callers depend on the wrapper interface, not the core, when a wrapper exists.
- Feature-specific rules live in the wrapper, e.g. picker reuse per lobby.

## Rules — Popup content (per-instance controller + view)

- One controller instance per open popup; no DI singletons holding a single view/callback.
- View: top-level `<Name>PopupUi : BaseUi`; class name = prefab name = Addressables address in `Ui` group; `UiPlacement` maps it to `modalSlot`. Follow [[Rules/UI_CODE_BUILD_GUIDE]].
- Controller creates its view through `IUiService.CreateUi<T>()`; each request instantiates a new view, so it lands as the last child of `modalSlot`.
- Covered: close transient overlays (`TMP_Dropdown` list, tooltips) and keep them closed while covered; keep state and selection. Do not `Hide()` and do not dispose.
- Uncovered: resume normally; do not reset state.
- Dispose: destroy view, detach listeners, release generated assets (e.g. avatar sprites), unsubscribe services.

## Important Values

- Popup canvas sort order 20; slot order Contextual → EndGame → Modal → Tooltip → Fade ([[TODOs/Optimization/UI_Canvas_Routing_Optimization_Plan]]).
- `TMP_Dropdown.Show()` list canvas: override sorting 30000 (+ blocker canvas) → renders above every popup unless collapsed while covered.
- UI prefab key: `typeof(TUi).Name` via `IAssetService`.

## Implementation

| Layer | Type | Location |
|---|---|---|
| Core | `IPopupService`, `PopupService`, `PopupHandle`, `IPopupController`, `PopupBlockerUi` | `Services/Popups/`, blocker view `Entities/Popups/` |
| Confirm wrapper | `IConfirmPopupService.ShowAccept(AcceptPopupContent, Action<ConfirmPopupResult>)`, `ConfirmPopupResult` | `Services/Popups/` |
| Confirm popup | `AcceptPopupUi`, `AcceptPopupUiController` | `Entities/Popups/` |
| Steam wrapper | `ISteamPopupService.OpenInvitePicker()` → `PopupHandle`; `CloseInvitePicker(handle)`, `ShowInviteReceived(from, Action<ConfirmPopupResult>)` | main-menu Online feature |
| Steam popup | `SteamInvitePopupUi` + `SteamInvitePopupUiController`, reusing `SteamInvitePresenter` / `SteamInviteView` | `Entities/Popups/` |

- `Open(IPopupController)` creates a view and returns a handle. The controller receives a completion callback for player actions.
- `Close(handle)` dismisses the popup. `IsOpen(handle)` lets a wrapper reuse an existing instance.
- `IPopupController.SetCovered(bool)` manages transient overlays. The core manages `BaseUi` interaction and view destruction.
- `UiRegistrations` registers the popup core and Confirm wrapper in Main Menu and Skirmish scopes. `MainMenuScope` registers the Steam wrapper.
- `MainMenuUiController` ignores dismissed consent requests. `OnlineRouteUiController` only joins if the pending invite still matches the dialog’s lobby.

## Edge Cases

- Invite arrives while picker open → confirm popup on top; picker dropdown collapsed; avatar loads must not re-expand it.
- Accepting an invite that switches lobby → old lobby's owner closes its picker handle (`Dismissed`); no stale picker restored.
- Two Steam invites → two confirm popups; an older dialog can only accept an invite for its own lobby while that invite is still pending.
- Consent and invite dialogs may be visible together (stack, not queue).
- Owner closes a covered popup → top popup keeps focus and position.
- Repeated Invite Friends with picker already open → reuse handle; no second Steam overlay call.

## Files

- Plan: [[Done/Refactoring/Shared_Invite_Friends_Popup_Plan]]
- `Assets/Scripts/Services/Popups/`; `Assets/Scripts/Entities/Popups/`; `Assets/Prefabs/Ui/Popups/`
- `Assets/Scripts/BaseSystem/Ui/{UiCancelRouter,UiController,UiPlacement}.cs`
- `Assets/Scripts/Services/VContainer/Registrations/UiRegistrations.cs`; `Assets/Scripts/Services/VContainer/Scopes/MainMenuScope.cs`
- Related: [[Rules/UI_CODE_BUILD_GUIDE]]; [[Rules/UI_UX_GUIDELINES]]; [[Architecture/VCONTAINER_LIFECYCLE]]
