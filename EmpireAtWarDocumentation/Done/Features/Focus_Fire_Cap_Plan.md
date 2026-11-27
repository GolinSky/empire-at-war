---
category: Features
status: done
updated: 2026-11-25
completed: 2026-11-25
acceptance: pending-user
---
# Focus Fire Cap

## Goal
- A weak or mid ship no longer dies in seconds when N units are ordered onto it.
- One rule for both sides: a target accepts a limited number of main attackers, scaled by its `ShipClass`.
- Enemy AI spreads its strike ships and focuses less on low difficulty.

## Decision
- Chosen: attacker cap per target (approach 1) + AI spread and difficulty scale (approach 5).
- Why: one visible rule limits player and AI; player no longer has to micro focus fire to keep up.
- Avoid: damage intake cap, diminishing returns per attacker (invisible to the player, harder to balance).

## Current Code (verified 2026-11-25)
- Every attack order, player and AI, ends in the private `UnitOrderService.IssueAttack(receivers, target, offsets, hardPointId, pace)` (`Services/UnitOrders/UnitOrderService.cs:175`).
  - Player: `PlayerOrderInputHandler` → `IPlayerUnitOrders` → `AuthorityCommandSink` → `IUnitOrderService`.
  - AI: `EnemyTaskForceExecutor` → `AiOrderDeduplicator` → `IUnitOrderService`.
- AI focus source: `EnemyTaskForceExecutor.IssueStrikeAttack` (`:933`) sends every strike ship to one target. Also `:415` (defense intercept), `:548` (capture blocking facility), `:737` (ally marker).
- Unordered ships pick a main target with `ShipEngagement.SelectThreat` (`IdleState`, `GuardState`, `HuntState`); squadrons in `Squadron.cs:815`.
- Attack-move picks differently: one shared `AttackMoveEngagement` per order; `SelectTarget` spreads members across the reported enemies (`AttackMoveState.cs:142`). En-route it only answers its own attacker (`FindEnRouteAttacker`).
- `WeaponComponent` also fires at `AttackType.Base` groups (radar list) → guns shoot any hostile in range, with or without a main target.
- "Who attacks X" already exists: `IUnitCombatObserverFacade.IsAttacking(target)`.
- AI tuning per difficulty: `EnemyAiDifficultyProfile`.

## Rules
- Slot = one attacker holding the target as main target (`OrderedTarget` or `MainTarget`).
- Secondary fire stays: guns still shoot a capped target through `AttackType.Base` when it is in range. Cap limits main targets only; measure time-to-kill before changing this (user 2026-11-25).
- Ships and squadrons hold separate slot pools on the same target.
- Attack-move is exempt (user 2026-11-25): `AttackMoveState` is unchanged, claims no slot and is never refused a target. It already spreads its group through `AttackMoveEngagement.SelectTarget` (prefers the less-attacked enemy).
- Cap counts attackers per attacking team, so allies share one cap.
- Cap by target `ShipClass`; `0` = unlimited.
- Ordered attack over the cap → nearest attackers take the free slots; the rest attack-move to the target position and engage other hostiles.
- Freed slot (attacker dead, retargeted, out of reach) → an overflow ship nearby takes it on its next `SelectThreat` search.
- Exempt as targets: `Structure` (stations, platforms, mines), superweapon and ability targets.
- Exempt as attackers: stations (`StationaryAttackFacade`), abilities, superweapons.
- Authority only. Clients keep following host targets (`WeaponComponent._followsHost`).

## Important Values (start values, tune in play)
| Target class | Ship slots | Squadron slots |
|---|---|---|
| Fighter / Bomber / Interceptor / HeroStrikecraft | 2 | 4 |
| Corvette | 1 | 4 |
| Frigate | 1 | 4 |
| Cruiser | 2 | 6 |
| HeavyCruiser | 2 | 6 |
| Capital | 3 | 8 |
| BattleCruiser | 4 | 8 |
| Dreadnought | 5 | 12 |
| Structure | 0 (unlimited) | 0 (unlimited) |

- Squadron cap is separate from the ship cap and 2× the first draft (user 2026-11-25).
- Ship slots lowered 2026-11-27 (user): dreadnoughts died in 43–63 s with 10–12 enemy ships on them (8 slot holders + 4–6 attack-move overflow).
- Heroes count as their class, no extra slots (user 2026-11-27; `heroExtraSlots` and the `FactionCatalog` hero check removed from `FocusFireCaps`). Superseded: the earlier `+1` hero slot.

- AI cap scale (`EnemyAiDifficultyProfile.FocusFireScale`, rounded up, minimum 1): Easy `0.5`, Medium `0.75`, Hard `1`, UltraHard `1`.
- Values live in `UnitOrderSettings` under `[Header("Focus Fire")]`; never hardcode them in code or tests.

## Implementation

### Phase 1 — Slot registry (pure C#)
1. [x] `UnitOrderSettings` `[Header("Focus Fire")]`: `focusFireCaps` (`FocusFireCap` per `ShipClass`: `shipSlots`, `squadronSlots`) + `heroExtraSlots`; `GetFocusFireCap(class, isSquadron)` throws for a missing class. Asset reserialized with the table values.
2. [x] `IFocusFireSlots` / `FocusFireSlots`: `GetCap`, `CountHolders`, `FreeSlots`, `Holds`, `HasSlotFor`, `TryClaim`, `Release(attackerId)`, `Changed`. Cap lookup split out: `IFocusFireCaps` / `FocusFireCaps` (settings + `FactionCatalog` hero check).
3. [x] Keys by entity id; `IEntityLocator.EntityRemoved` drops attacker and target claims; destroyed holders stop counting at once. Allies share via `IPlayerRelations.IsAllied`.
4. [x] `SkirmishScope`: `FocusFireCaps`, `FocusFireSlots` before `UnitOrderService`. Per unit: `IFocusFireClaim` / `FocusFireClaim` in ship and squadron scopes.

### Phase 2 — Ordered attacks (player + AI)
1. [x] `UnitOrderService.SplitFocusFire`: holders keep slots, nearest others fill free slots, per pool (ships / squadrons).
2. [x] Holders → `Attack` / `AttackHardPoint`; overflow → `IssueAttackMove(overflow, targetPosition)`. Explicit-offset overload keeps offsets aligned (`AttackerOffsets`).
3. [x] `AttackIssued` and the Attack `Publish` list holders only; overflow publishes `AttackMove`.
4. [x] `AttackTargetState.Enter` → `TryClaim`, `Exit` → `Release`. `ShipOrderRunner.FindReplacement` prefers targets with a free slot.
5. [x] Hardpoint orders share the split (`IssueCappedAttack`).

### Phase 3 — Unordered target picks
1. [x] `ShipEngagement.SelectThreat(..., IFocusFireClaim)`: an open target beats a full one before every other rule.
2. [x] `IdleState`, `GuardState`, `HuntState` claim on target set, release on drop / `Exit`. `HuntState.FindTarget` prefers open targets. `AttackMoveState` unchanged.
3. [x] `Squadron.Engage` claims (release on attack-move / ability orders), `Disengage` / `Release` release. `SquadronTargetSelector.Tier` ranks full targets after all others; bomber guard join skips a full target.
4. [x] Lone target → still picked (ranking, not filtering); `TryClaim` fails and the unit attacks without a slot.

### Phase 4 — Enemy AI spread (approach 5)
1. [x] `EnemyAiDifficultyProfile.FocusFireScale`: Easy 0.5, Medium 0.75, Hard 1, UltraHard 1.
2. [x] `EnemyTaskForceExecutor.StrikeLimit` = min(ceil(cap × scale) ≥ 1, own holders + team free slots). `SplitStrikeOverflow` in `IssueStrikeAttack` (covers `IssueAttackOrHunt` + `ExecuteLocalEngagement`); overflow attack-moves to the target, re-sent only when its order or target changed (`_overflowTargets`).
3. [x] Defense intercept: interceptors past the limit take a defense point. Capture facility (`:548`): structure → unlimited, unchanged. Marker attack: `IssueCappedMarkerAttack`.
4. [x] Executor splits itself, so the service never overflows AI ships (limit ≤ free slots).
- Decision (carriers): carriers rank first in the strike split, before slot holders; an attack-move would fly them onto the target.

### Phase 5 — Feedback
1. [x] Order feedback: overflow publishes `AttackMove`, so it shows the attack-move marker.
2. [x] `FocusFireCounterUi` (`Entities/FocusFireCounter`, prefab `Assets/Prefabs/Ui/FocusFireCounter/FocusFireCounterUi.prefab`, Addressable `FocusFireCounterUi` in `Ui`, `feedbackCanvas`, battle preload key): `SHIPS n/cap` + `SQD n/cap` 40 px below the target; uncapped pool hidden; full pool `#E86E6C`, open `#6DB8E2`; Rajdhani SemiBold 18; no raycasts.
3. [x] `FocusFireCounterUiController`: hovered hostile first, else the first attack target of the player's selection; hidden with the HUD, behind the camera, or when both pools are unlimited.
4. [x] Counts through `IFocusFireCounts`: offline/host `LocalFocusFireCounts` (registry); client `ReplicatedFocusFireCounts` fed by `ClientFocusFireReceiver`.
- Decision (Open Question 1): new `FocusFireCountMessage { TargetId, Ships, Squadrons }`, host → each client with that client's team counts. `HostFocusFireSender` batches changed targets per tick and sends every held target to a client that finishes loading. Why: `AttackTargetMessage` lists one order's receivers, not team totals.

## Edge Cases
- Target at cap, new order from the same player with other ships → overflow only; current holders keep their slots.
- Player re-orders slot holders onto the same target → no slot change, no churn.
- Slot holder leaves reach (`EnemyAttackTargetRules.KeepsTarget` false) → release.
- Target cloaks or dies → all its slots released.
- Hero ships as targets: plain `ShipClass` cap (hero bonus removed 2026-11-27).
- Attack-move ships on a capped target → allowed, not counted, not shown in the slot counter.
- Overflow from an ordered attack (Phase 2) and AI screens (Phase 4) run attack-move → against a lone target they may still pick it; in a group fight they spread. Accepted leak; include in the time-to-kill measurement.
- `keepTargets: true` AI states (AssaultBase, EngageLocal): a ship keeping its own target keeps its slot.
- Carriers stay in `_strikeShips` (attack order gives standoff, `:870`): count them as slot holders first or exempt them. Decide in Phase 4.
- Online: registry runs on the host; `AttackTargetMessage` carries only real attackers.

## Resolved (user 2026-11-25)
- Secondary fire → keep; cap main targets only, then measure time-to-kill.
- Squadrons → own cap, 2× the first draft.
- Heroes → +1 slot.
- Slot counter on the target marker → in scope (Phase 5).

## Open Questions
1. Online: how the slot counts reach clients for the counter (new message or piggyback on `AttackTargetMessage`). Decide in Phase 5.

## Files
- `Assets/Scripts/Services/UnitOrders/UnitOrderService.cs`, `UnitOrderSettings.cs`, new `FocusFireSlots.cs`, `IFocusFireSlots.cs`
- `Assets/Scripts/Entities/Ship/StateMachine/ShipEngagement.cs`, `IdleState.cs`, `GuardState.cs`, `HuntState.cs`, `AttackTargetState.cs`
- `Assets/Scripts/Entities/Squadron/Squadron.cs`
- `Assets/Scripts/Services/Enemy/EnemyTaskForceExecutor.cs`
- `Assets/Scripts/Entities/EnemyFaction/Models/EnemyAiDifficultyProfile.cs`
- `Assets/Settings` → the `UnitOrderSettings` asset (set through `SerializedObject`)

## Tests
- Read `Rules/TEST_GUIDELINES` before writing tests.
- Categories: `BattleInput` (order split, overflow → attack-move), `Ships` (`SelectThreat` skips capped targets, claim/release in states), `Squadrons`, `EnemyAi` (strike split, difficulty scale, no order churn), `Networking` (attack relay lists real attackers).
- Review: `PlayerOrderInputHandlerTests`, `EnemyOrderRulesTests`, `EnemyUnitCommanderTests`, `AiOrderDeduplicatorTests`, `ShipPursuitTests`.
- No stat-pinning: tests build their own caps; shipped values are only checked as `>= 0` and non-decreasing with hull tier.
- Not run (local override).

- Phase 5 adds category `Ui` (counter view) and the marker prefab.
- 2026-11-25 added: `FocusFireSlotsTests`, `FocusFireSettingsTests` (shipped caps ≥ 0, non-decreasing with hull tier, structure unlimited; scale in (0, 1], non-decreasing with difficulty), 3 split cases in `UnitOrderServiceTests`.
- 2026-11-25 updated for new ctor args (`FreeFocusFireClaim`, `UnlimitedFocusFireSlots` doubles): `ShipPursuitTests`, `ShipStopTests`, `SquadronTargetSelectorTests`, `UnitHighlightSettingsTests`, `EnemyUnitCommanderTests`, `EnemyDecisionSchedulingTests`. Existing expectations unchanged (doubles leave every target open).
- Phase 4/5 tests added: `EnemyUnitCommanderTests` (`MarkerAttack_NearestShipsFillTheScaledCapAndTheRestAttackMoveToTheTarget`, `StrikeOverflow_AttackMovesOnceAndIsNotResent`), `FocusFireCounterUiTests` (`Ui`), `ReplicatedFocusFireCountsTests` (`Networking`), `UiCanvasArchitectureTests` placement case.
- Verification 2026-11-25: Unity compile clean. Tests not run (local override).

## Outcome
- Completed 2026-11-25: all five phases implemented and compiled. User gates → [[TODOs/User_Acceptance_Queue#Focus Fire Cap]].
- Reusable guidance → [[GameDesign/Commands_And_Controls]] (Orders: focus fire cap).
- Not extracted: `UnitOrderService` (~600 lines) and `EnemyTaskForceExecutor` (~1290) grew; a focused `FocusFireOrderSplit` class remains a refactor suggestion, outside this plan.
