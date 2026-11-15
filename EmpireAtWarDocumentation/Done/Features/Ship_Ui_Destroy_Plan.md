---
category: Features
status: done
created: 2026-11-15
updated: 2026-11-15
completed: 2026-11-15
acceptance: pending-user
---
# Destroy ship from ship UI

## Goal
- Right-click one ship card → menu → **Destroy ship** → generic Yes/No confirmation → authoritative ship death.
- Preserve existing HUD appearance, selection, focus, abilities, auto-power, tooltips and grouped-card behavior.
- Support offline, online host and remote co-op/PvP clients through the same validated command path.

## Decision
- User confirmed 2026-11-15: **single ship only; disabled on groups**. Individual ship cards within an expanded group may target their own ship; group headers and collapsed summaries cannot destroy.
- Implemented: only the requesting player's own living ships; no allied-player ships, enemies, stations, squadrons or reserve/build cards.
- Implemented: immediate normal death, no countdown and no refund. Preserve existing death effects, wreck and cleanup rules; do not introduce new damage or rewards.
- Why: reuse existing command, popup and death infrastructure; minimize changes to working UI.
- Avoid: direct Unity object destruction, client-side predicted death, batch destruction, a new generic menu framework or unrelated UI refactors.

## Implementation

### 1. Authoritative command
- [x] Add single-target `DestroyShipCommand` under `Assets/Scripts/Services/PlayerCommands`; extend `IPlayerCommandSink`, `AuthorityCommandSink` and `NetworkCommandSink`.
- [x] Extend `PlayerCommandKind`, `PlayerCommandMessage`, `PlayerCommandSerialization`, and `HostCommandReceiver.IssuerOf/Apply`. Append the new kind to preserve existing wire enum values. Bumped `OnlineMatchService.LOBBY_PROTOCOL_VERSION` from 2 to 3 so incompatible builds cannot enter a match together.
- [x] Authenticate issuer from the connection slot using the existing receiver; never trust a client-provided owner.
- [x] Resolve target by stable entity ID at execution; require live match, correct ship type, living entity, exact owner and destroy facade. Alliance membership is insufficient.
- [x] Invoke `IEntityDestroyFacade.Destroy()` → `ShipDestroyFacade` → `HealthModel.Kill()`. Reject stale, wrong-type, unauthorized or duplicate requests without repeating death.
- [x] Reuse `HostTickReplicator` death/despawn and `ClientReplicationReceiver` replica death handling. No extra destruction broadcast; clients wait for authority.
- Verify: one accepted command produces one normal death on all peers; invalid commands cannot affect a target.

### 2. Ship card input and menu
- [x] Add a narrowly scoped right-click input binding to the actual ship-card hit area; forward entity ID and pointer position to a focused flow owner.
- [x] Wire both the selected single ship and reusable individual group entries. Group headers/collapsed cards never resolve an arbitrary first ship.
- [x] Keep one menu per local HUD, positioned near the pointer and clamped within the canvas. One destructive action: **Destroy ship**.
- [x] Preserve left-click selection, focus, ability buttons, auto-power and tooltip behavior; no root background overlay, hierarchy rebuild or restyling.
- [x] Escape/outside click closes the menu; consume dismissal input so it cannot issue a world order. Use existing UI hit testing/cancel routing.
- [x] Close/invalidate the flow on card rebinding, selection change, target death, route hide, battle exit and disconnect.
- Verify: group counts 1–4 keep exact card targets; transition 4 → 5 disables grouped destruction; transition 5 → 2 does not retain stale callbacks.

### 3. Generic confirmation
- [x] Close the menu before calling `IConfirmPopupService.ShowAccept` with `AcceptPopupContent`.
- [x] Text: title **Destroy ship?**, message **Destroy {ship name}? This cannot be undone.**, buttons **Yes** / **No**.
- [x] Only `ConfirmPopupResult.Accepted` can submit. No, Escape, dismissal and teardown submit nothing.
- [x] Capture the target ID at menu opening; never read the current selection to choose a target after Yes.
- [x] Revalidate target, ownership and active flow before sending; host revalidates independently. Permit only one pending confirmation/request for this flow.
- [x] Use an invalidation token/version for late callbacks. `IConfirmPopupService.ShowAccept` returns the existing `PopupHandle`; `Close(handle)` dismisses it on invalidation.
- [x] Verify popup pointer blocking and battle hotkey suppression explicitly; existing popup stacking alone does not establish a full battle input lock. The flow holds `IInputLock` through the menu and confirmation without pausing the match.
- Verify: ship dying while the popup is open, changed selection, repeated Yes and disconnect cannot destroy a different ship or execute after teardown.

### 4. Performance and prefab integration
- [x] Event-driven only: no new Update loop, scene/entity scans, polling, LINQ in input handlers or per-frame allocations.
- [x] Reuse existing ship cards and one menu instance; clear subscriptions and target state on reuse. One ID lookup and bounded validation per accepted action.
- [x] Preserve existing serialized references, borders, icons, colors, geometry and button states. Use the current destructive action styling.
- [x] Bind references explicitly; keep `ShipUiController` changes to delegation because it is already large.
- [x] Read `PROJECT_ORGANIZATION` before new asset folders and Addressables guidance before any mapping/import changes. Prefer a menu child in the existing HUD prefab when compatible with popup layering.
- [x] Mutate prefabs through Unity-aware tooling; save/import changed assets and inspect Console. Do not modify unrelated assets or Addressables folder structure.
- Verify: repeated open/cancel/rebind does not grow views/listeners; no ongoing work while closed. Runtime allocation/timing claims require an actual capture.

### 5. Regression coverage and acceptance
- [x] Extend `PlayerCommandSinkTests`: valid owner, ally/enemy rejection, invalid ID, wrong entity type, dead target, duplicate command and inactive match.
- [x] Add command serialization/authentication coverage and replica death/despawn coverage; update every `IPlayerCommandSink` fake after interface changes.
- [x] Extend `ShipSelectionReuseTests` for right-click target rebinding, stale callback cleanup and disabled collapsed groups.
- [x] Add UI flow tests: accepted once, No/Escape/dismissed zero commands, selection/death/route invalidation, screen-edge positioning and click-through prevention.
- [x] Review `PopupServiceTests`, `ShipAutoPowerUiTests`, `SelectionInputTests`, `InputLockServiceTests` and `NetworkTickReplicationTests`; retain existing behavioral assertions.
- [x] Review source/test diff together and validate prefab references/compilation without running tests.
- [x] User acceptance queued in [[TODOs/User_Acceptance_Queue]]: offline; host destroys own ship; remote client destroys own ship; ally/enemy attempts fail; all peers see the same death and selection cleanup; repeat under latency and disconnect.
- Categories: `Ui`, `BattleInput`, `Networking`; review `Health` and `Ships` death contracts. Add `Health`/`Ships` execution only if those implementations change.
- Local tests remain disabled unless explicitly requested. No tests were run during planning.

## Files
- `Assets/Scripts/Entities/ShipUi/ShipUi.cs`, `ShipUiController.cs`, `ShipSelectionGroupUi.cs`, `ShipGroupUi.cs`, `Model/ShipUiEntry.cs`.
- `Assets/Prefabs/Ui/Ship/ShipUi.prefab`, `ShipGroupEntryUi.prefab`; `ShipGroupUi.prefab` retains its existing entry reference.
- `Assets/Scripts/Services/Popups/ConfirmPopupService.cs`, `AcceptPopupContent.cs`, `PopupService.cs`; `Assets/Scripts/Entities/Popups/AcceptPopupUiController.cs`.
- `Assets/Scripts/Services/PlayerCommands/`; `Assets/Scripts/Services/Networking/` and its `Messages/` folder.
- `Assets/Scripts/Entities/Ship/EntityFacades/Health/ShipDestroyFacade.cs`; `Assets/Scripts/Entities/Ship/Ship.cs`.
- [[Rules/UI_UX_GUIDELINES]], [[Rules/UI_CODE_BUILD_GUIDE]], [[TODOs/Features/Multiplayer_Plan]].

## Important Values
- Existing individual-card threshold: `MAX_INDIVIDUAL_SHIPS = 4`; 5+ ships collapse into a summary.
- Existing confirmation service creates its popup on demand; do not claim it is already pooled or allocation-free.
- Evidence 2026-11-15: Graphify exploration and live source inspection; test sources reviewed for planned extensions. No implementation, prefab mutation, test execution or runtime verification.
- Serena launcher reported running, but direct MCP connection failed; live text inspection used for planning.

## Outcome · 2026-11-15

- Existing `ShipCard` raycast targets receive right-clicks. A press recorded before an entry rebind is discarded. Expanded groups pass each entry's exact entity ID; collapsed groups and squadrons have no eligible action.
- One prefab-authored menu is reused in the contextual canvas slot. Its dismissal surface captures outside clicks. The generic Yes/No popup uses the shared blocker and keeps the battle/camera input lock until resolved.
- Same command path for offline and host authority. A remote client sends a reliable request and keeps the target pending for the current binding until replica death/removal or invalidation. The authority checks the authenticated slot, exact ownership, ship type, live match, health and destroy facade.
- Unity script compilation completed with zero errors. Unity saved both prefabs and verified the card hit bindings, single inactive menu, action and dismissal surfaces. Existing Addressable keys were inspected.
- `Ui`, `BattleInput` and `Networking` regressions authored and reviewed alongside the implementation. `Abilities` and `Audio` fixture compatibility updated. Existing `Health` and `Ships` death contracts reviewed without changes. Automated tests were not run under the local override.
- Offline, host, remote, latency/disconnect and unrun test gates → [[TODOs/User_Acceptance_Queue]].
- Reusable confirmation-handle and battle input rules → [[Architecture/POPUP_SYSTEM]].
- Serena launcher reported running, but the local endpoint failed to connect. Graphify and live source were used for navigation.
