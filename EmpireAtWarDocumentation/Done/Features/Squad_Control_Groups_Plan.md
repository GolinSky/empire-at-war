---
category: Features
status: done
design_state: approved
created: 2026-11-16
updated: 2026-11-16
completed: 2026-11-16
acceptance: pending-user
---
# Squad Control Groups

## Goal

- Save and recall 10 battle-local selection bookmarks; player-facing label “Squads”.
- Implementation completed 2026-11-16; user play/platform acceptance remains in [[TODOs/User_Acceptance_Queue]].

## Decision

- Approved: fixed physical top-row keys `1–9, 0`; 0 is last. Numpad excluded; squad actions hidden from rebinding; digits reserved from other action overrides.
- Hold Ctrl to preview; Ctrl + digit assigns. Plain digit recalls. Ctrl default on Windows/Mac; Mac-only Settings → Camera dropdown offers Command.
- Left/right modifier variants supported. Shift, Alt or unchosen extra modifier + digit → no squad action. Existing Ctrl+A / Ctrl+Shift+A remain available.
- Eligible: owned mobile ships, mobile heroes/support and selectable squadrons. Each squadron counts once; mixed groups and overlapping membership allowed; no member cap.
- Exclude structures, enemies and uncontrolled allies. Filter assignment to eligible units; zero eligible units never clears a slot.
- Recall replaces selection through the ordinary selection service; no orders, camera movement, formations, naming, Shift-add/remove or separate Clear action.
- Different occupied membership → generic Replace/Cancel popup. Identical membership → highlight without confirmation.
- Keep confirmation on Alt-tab. Hide preview on focus loss; require fresh input after focus, locks, text input or rebinding.
- Battle continues during confirmation; gameplay input locked. Snapshot candidates at request; revalidate survivors on acceptance; no survivors → preserve old group.
- Save committed memberships with battle and owner. Restore surviving eligible IDs after entities; old saves and new battles start empty.
- Remove dead/despawned/no-longer-owned members. Never add replacement lives automatically, including pooled squadrons reusing an ID.

## Implementation

### Data, selection and save

- `ControlGroupModel`: pure C#, ten digit-indexed lists of `long` entity IDs, equality, pruning and change events.
- `ControlGroupService`: local eligibility, overlap, batch recall, locator removal and read-time death/ownership pruning.
- `ISelectionService.SelectEntities`: one diffed selection transaction, normal markers/audio/death subscriptions; clears prior tap state.
- `ControlGroupAssignmentFlow`: captured IDs, versioned callback, full input-lock handle; closes owned popup on battle exit/disposal.
- Pending transactions track every `EntityRemoved` ID. A pooled squadron may reuse its ID; removal permanently excludes that candidate from this transaction.
- `ControlGroupSaveSource : IBattleSaveSource`: clear before restore, restore local owner after units; only committed surviving memberships captured.
- `BattleSave.ControlGroups` uses serializable `ControlGroupSave[]`; validator rejects out-of-range/duplicate owner-slot records. Unknown members are filtered.
- Existing save version retained; missing field defaults empty. Restored IDs reserve generator space; pool reuse is handled independently.

### Input and settings

- Battle map: ten `ControlGroup0…9` Button actions plus Ctrl and Command modifier actions; generated `GameInputActions.cs` regenerated.
- `ControlGroupInput`: capture modifier state at digit event; one request per frame in display order; hold/repeat/release cannot cause a second recall.
- Battle action-map locks and rebinding suspend shortcuts; explicit application-focus and selected TMP/legacy text-field checks prevent accidental commands.
- `ControlGroupBindings` excludes fixed actions from catalog; `InputBindingService` rejects reserved digits and strips conflicting persisted overrides.
- `InputSettingsData.ControlGroupModifier` participates in draft/apply/discard/equality; separate from binding JSON.
- `SettingsUi.squadModifierRow`: explicit prefab reference, Mac-only visibility; Camera category scroll area, Control/Command options.

### Preview and notifications

- `ControlGroupUi` / `ControlGroupSlotView`: 676 × 68 canvas-unit strip, top-center at y = −48 in Skirmish, −160 in Survival, and −288 in Battle Simulation. Ten 64 × 68 slots ordered 1–9 then 0, each with a slate `#202E40` fill and two 28 × 28 aspect-preserving icon frames. Notification cards start at y = −124 in Skirmish and −236 in Survival.
- Number + up to two type icons + total count. Empty = dashed outline and +; exact selection has a marker; assignment highlight lasts 0.6 s.
- `ControlGroupUiController`: visible only with eligible held modifier in Running/Paused; refresh on changes or every 0.15 s while visible; no pointer raycast targets.
- `ControlGroupIconSelector` reuses `IShipIconProvider` and faction data; distinct types, hero first, then explicit class rank; stable type identity breaks ties.
- Class rank: HeavyCapital > Capital > Cruiser > Frigate > Corvette > HeroStrikecraft > Bomber > Interceptor > Fighter. Type frequency never affects priority.
- `IBattleNotifications.Post` uses Feedback priority; group key includes digit and message so different errors never retain an earlier message.
- Empty recall preserves selection. Invalid assignment preserves slot. Partial confirmation survival reports actual assigned count.
- Prefab `Assets/Prefabs/Ui/ControlGroups/ControlGroupUi.prefab`: Addressables Ui group, address `ControlGroupUi`, registered in preload and dynamic canvas placement.
- Services/controllers registered in `SkirmishScope`; reuse shared confirmation and icon provider.

## Edge Cases

- Target dies during confirmation → valid captured candidates can still fill the slot.
- Candidate dies or despawns/relaunches under the same ID → removed from pending candidates; old group survives if none remain.
- Escape/Cancel/Dismissed → no replacement; stale callback cannot alter another transaction/battle.
- Save during confirmation → old committed group only; no popup/lock restored.
- Temporarily unselectable member remains stored; recall uses selectable survivors. Occupied stored membership still requires replacement confirmation.
- Multiple digit events in one frame → first in display order wins; no stacked dialogs.
- Preview may appear during Ctrl+A/Ctrl+Shift+A; immediate modifier preview is intentional v1 behavior.
- OS desktop shortcuts can intercept Ctrl+digits on Mac; validate target machine and optional Command. No claim of bypassing OS shortcuts.
- Client-local groups do not introduce network commands; ordinary selection/order authority remains unchanged.

## TODO

- [x] Resolve all product decisions and update ten-slot specification.
- [x] Implement model/service, eligibility/pruning and batch selection.
- [x] Implement fixed input, reserved key handling, settings and fresh-input gating.
- [x] Build/import/save preview prefab, Mac dropdown, Addressables/preload/canvas wiring.
- [x] Implement replacement snapshot, lock/teardown, Alt-tab preservation and pooled-lifetime protection.
- [x] Implement battle save capture/restore and validation.
- [x] Review production/test diffs, author focused regression coverage and compile.
- [x] Extract reusable rules to [[GameDesign/Squad_Control_Groups]].
- User-only gates moved to [[TODOs/User_Acceptance_Queue]]; no unimplemented feature scope remains.

### Verification — 2026-11-16

- Unity `recompile --timeout 60 --json`: completed, 0 errors; Console error query: 0 entries.
- Preview and Mac settings row rendered from saved prefabs in isolated PreviewScenes; no gameplay scenes changed. Dropdown verified by scrolling Camera content.
- Source review corrected notification grouping and occupied-but-unavailable replacement handling; reviewed snapshot, save, pooled reuse and callback lifetime.
- Added `ControlGroupModelTests`, `ControlGroupInputTests`, `ControlGroupAssignmentFlowTests`, `ControlGroupSaveTests`, `ControlGroupPreviewTests`.
- Updated `SelectionInputTests`, input-binding/settings fixtures, `UiCanvasArchitectureTests`, `BattleAssetPreloaderTests` and affected selection-interface mocks.
- Categories: BattleInput, Ui, GameFlow, Core; Abilities/Economy mock signatures updated. CameraInputBindingsTests and AutosaveScheduleTests reviewed, contract unchanged.
- Scoped source whitespace check passed. Generated serialized prefab whitespace and unrelated pre-existing asset changes were not cleaned.
- Automated tests **not run** under local execution override. No live battle, Mac hardware, save/load playthrough or target-resolution acceptance claimed.
- Source-verified implementation complete; `acceptance: pending-user`.

## Files

- Runtime: `Assets/Scripts/Services/ControlGroups/`, `Services/Input/ControlGroup*`, `Services/Saves/ControlGroup*`, `Entities/ControlGroups/`.
- Authoring: `Assets/Scripts/Editor/ControlGroups/ControlGroupUiBuilder.cs`; `Tools/BattleBuilder/BuildSquadModifierUi.cs`.
- Visual QA: `Tools/BattleBuilder/PreviewControlGroups.cs`; [actual prefab render](<F:/Private/empire-at-war/Logs/ControlGroups/squad-preview.png>); [Mac row preview](<F:/Private/empire-at-war/Logs/ControlGroups/settings-modifier.png>).
- Historical nine-slot mockup `C:/Users/golin/.agent/diagrams/squad-preview-compact.html` is superseded by the ten-slot Unity prefab.
- Guidance: [[Architecture/POPUP_SYSTEM]], [[Rules/UI_UX_GUIDELINES]], [[Rules/UI_CODE_BUILD_GUIDE]], [[Architecture/PROJECT_ORGANIZATION]].
