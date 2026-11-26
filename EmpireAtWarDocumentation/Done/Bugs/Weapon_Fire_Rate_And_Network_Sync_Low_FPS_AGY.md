---
category: Bugs
status: done
agent_marker: AGY
created: 2026-11-18
disposition: superseded
superseded_by: "[[Review/Bugs/Weapon_Fire_Rate_Low_FPS]]"
completed: 2026-11-26
---
# [AGY] Weapon Fire Rate Degradation and Ability Sync Crash at Low FPS

## Goal
- Eliminate weapon fire-rate throttling and complete firing stalls on the host at 10–20 FPS (`dt = 50–100 ms`).
- Fix `ShipAbilityService` / `HostCombatEventSender` throwing `Exception: Not entity found with id:` when an ability caster is destroyed or despawned, which aborts `OrderedLifecycleCoordinator.Tick()` mid-battle (`5×`) and `LateDispose` at teardown (`4×`).

## Evidence (`Player.log` Host Battles 5–8)
- User report: at 10–20 FPS in 3v4+ AI online battles, ships on the host either stop firing or fire at sharply degraded rate.
- `09_exceptions_and_navigation_failures_AGY.log`: `9` unhandled `Exception: Not entity found with id:` crashes on host (`5` mid-battle inside `ShipAbilityService.Advance` → `Stop` → `HostCombatEventSender.OnAbilityStateChanged` → `ShipAbilityEventMessage.Capture` aborting `OrderedLifecycleCoordinator.Tick()`: IDs `173` (`L05022`), `180` (`L08413`), `170` (`L08624`), `666` (`L52089`), `1050` (`L56893`); `4` at teardown inside `ShipAbilityService.LateDispose`: IDs `1094` (`L13364`), `1213` (`L26887`), `1176` (`L42112`), `1515` (`L64704`)).

## Root Causes (Source-Verified)
1. **Fleet-wide `ShotEffectCache` starvation + 1-per-frame runtime growth (`ShotPoolPrewarmer.cs:26-28, 77-92, 142-212`)**:
   - `WarmLoadout` (`L88-92`) and `AddLoadout` (`L207-211`) set `_targets[cache] = Math.Max(current, target)` across all callers/ships → each projectile pool is prewarmed for **one single ship's loadout**, not `110–163+` concurrent ships in 3v4/3v5 battles.
   - Runtime pool expansion in `ShotPoolPrewarmer.Tick()` (`L26-28, 142-152`) is capped at `MAX_CREATIONS_PER_FRAME = 1` and `BUDGET_MILLISECONDS = 0.5`. At 10 FPS, all projectile types combined can instantiate at most **`10` shots/second**!
2. **Strict FIFO head-of-line blocking in `ShotEffectCache.TryReserve` (`ShotEffectCache.cs:48-82`)**:
   - `TryReserve` enforces `if (_waiting.First == pool.AdmissionNode && _idle.Count - _reservedCount >= count)` (`L65`).
   - Any hardpoint queued behind `_waiting.First` is rejected even when enough idle shots exist for smaller salvos.
   - Because `TargetSelectionBatch.Process()` (`L156-163`) iterates hardpoints sequentially in `LateTick`, if `_waiting.First` appears late in the batch, every waiting hardpoint processed earlier in that frame fails `_waiting.First == pool.AdmissionNode` — capping admissions to **at most `1` hardpoint per frame** (`10` salvos/sec at 10 FPS) even when `_idle` has capacity!
   - When a waiting hardpoint loses its chance for a frame, its `AdmissionNode` stays in `_waiting`; if that hardpoint enters `_turningSalvos` (where `WeaponHardPoint.IsBusy` becomes `Time.time < _turnEndTime`, `WeaponHardPoint.cs:51`) or retries via `_retryAt`, it does not call `TryReserve` until ready again, stalling every mount queued behind `_waiting.First`.
3. **`ExpectedShotsInFlight` underestimates active lease duration (`WeaponHardPoint.cs:284-291`, `MissileShot.cs:99-100`)**:
   - `MissileShot.IsVisualComplete()` (`L99-100`) waits until `!_isFlying && !trail.IsAlive(true) && !interceptExplosion.IsAlive(true)`, holding the pooled `ShotEffect` and `_active.Count` for `1–3 s` of particle trail decay after impact, while `ExpectedShotsInFlight` only counts `flightSeconds = Range / ProjectileSpeed`.
   - `BeamShot` (`ProjectileSpeed == 0`) computes `flightSeconds = 0` → `inFlightCap = ShotsPerSalvo`.
   - `_reloadTimer.StartTimer()` (`WeaponHardPoint.cs:217`) starts on Shot 1, not salvo end, so stretched salvos overlap the next reload cycle.
4. **Low-FPS salvo cadence degradation & multi-frame turn handoffs (`CombatAttackCoordinator.cs:149-150, 490-491`, `WeaponComponent.cs:757-794`, `Timer.cs:14`)**:
   - **1-shot-per-frame ceiling (`CombatAttackCoordinator.cs:150, 491`)**: `BeginSequence` and `CommitDueEvent` set `sequence.EarliestFrame = frame + 1` and emit at most `1` shot per hardpoint per frame (`max 10 shots/s` at 10 FPS vs `50 shots/s` needed by `0.02 s` shot intervals under `BoostWeaponPower`).
   - **Intra-salvo drift (`L490`)**: `sequence.NextTime = now + sequence.HardPoint.DelayBetweenShots` resets cadence from actual frame `Time.time` (`+dt = 50–100 ms` drift per shot in every multi-shot salvo).
   - **Tick → LateTick → Next Tick turret turn handoff (`WeaponComponent.cs:757-794`)**: `TurnTowards(unit.Position)` returns `duration > 0f` for any sub-degree target movement (`L759-760`), deferring firing to `_turningSalvos` in next frame's `Tick()` (`+100 ms`), where re-checking `!HasFiringSolution` (`L787`) after target motion over the slow frame can cancel and restart selection.
   - **Strict `<` in `Timer.IsComplete` (`Timer.cs:14`)**: `_timer < CurrentTime` adds 1 extra frame (`100 ms` at 10 FPS) on exact timestamp equality.
5. **Host Ability Sync Exception Aborting `Tick()` (`ShipAbilityEventMessage.cs:18-26`, `HostCombatEventSender.cs:77-82`, `ShipAbilityFacade.cs:23,28`, `SquadronAbilityFacade.cs:19,25`)**:
   - `ShipAbilityEventMessage.Capture(slot)` reads `slot.Owner.Entity.Id` (`L20`), which invokes `_entities.GetEntity(_entityId)` (`ShipAbilityFacade.cs:28` / `SquadronAbilityFacade.cs:25`).
   - When a caster (e.g., despawned `Squadron` or destroyed `Ship` still in `ShipAbilityService._running` or shutting down in `LateDispose`) has already been removed from `EntityLocator`, `GetEntity(_entityId)` throws `Exception: Not entity found with id:` — aborting `OrderedLifecycleCoordinator.Tick()` for the rest of that frame.

## Implementation
1. **Fix `ShipAbilityEventMessage.Capture` & ability cleanup on despawn (`IShipAbilityFacade.cs`, `ShipAbilityFacade.cs`, `SquadronAbilityFacade.cs`, `ShipAbilityEventMessage.cs`, `ShipAbilityService.cs`)**:
   - Expose `long EntityId { get; }` directly on `IShipAbilityFacade` (implemented in `ShipAbilityFacade` and `SquadronAbilityFacade` using existing `_entityId`, and on `IHardPoint`/`ITargetedShipAbility` targets safely).
   - Use `slot.Owner.EntityId` in `ShipAbilityEventMessage.Capture(slot)` instead of `slot.Owner.Entity.Id`.
   - In `ShipAbilityService.LateDispose()`, unsubscribe/skip broadcast in `HostCombatEventSender` once battle teardown begins; stop/deactivate caster abilities cleanly before `EntityLocator` removal.
2. **Eliminate projectile pool starvation (`ShotPoolPrewarmer.cs`, `WeaponHardPoint.cs`)**:
   - Include post-impact trail/beam hold duration and salvo duration in `WeaponHardPoint.ExpectedShotsInFlight`.
   - Scale roster prewarm targets by expected concurrent battle demand instead of a single unit's max (`Math.Max` → scaled fleet baseline), and increase `ShotPoolPrewarmer.Tick()` growth throughput when `_waitingCount > 0`.
3. **Remove `ShotEffectCache` head-of-line blocking (`ShotEffectCache.cs`, `WeaponComponent.cs`)**:
   - Decouple admission from single-front `_waiting.First == pool.AdmissionNode` in `TryReserve` (e.g., grant waiting pools FIFO-prioritized credits against available `_idle` shots or promote next runnable waiter when front cannot reserve / is busy, and cancel reservation whenever a hardpoint becomes disabled or waits on turn).
4. **Make salvo cadence and turret handoffs FPS-independent (`CombatAttackCoordinator.cs`, `WeaponComponent.cs`)**:
   - In `WeaponComponent.Engage` (`L759`), fire immediately if `turnSeconds <= Time.deltaTime` (or small threshold) instead of deferring a sub-frame turn to next `Tick()`.
   - In `CombatAttackCoordinator.CommitDueEvent`, advance `NextTime += delay` (clamped catch-up window) and allow multiple due shots of the same fast-firing salvo within one slow frame (`EarliestFrame` only when capping runaway catch-up, not `1` shot/frame unconditionally).

## Files
- `Assets/Scripts/Entities/BaseEntity/EntityFacades/IShipAbilityFacade.cs`
- `Assets/Scripts/Entities/Ship/Abilities/ShipAbilityFacade.cs`
- `Assets/Scripts/Entities/Squadron/EntityFacades/SquadronAbilityFacade.cs`
- `Assets/Scripts/Services/Networking/Messages/ShipAbilityEventMessage.cs`
- `Assets/Scripts/Services/Networking/HostCombatEventSender.cs`
- `Assets/Scripts/Services/ShipAbilities/ShipAbilityService.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotPoolPrewarmer.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotEffectCache.cs`
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`
- `Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs`
- `Assets/Scripts/Components/Weapon/WeaponComponent.cs`
- Affected tests to review (do not run under local override): `Combat` (`ShotEffectCacheTests`, `CombatAttackCoordinatorTests`, `WeaponLoadoutBakeTests`), `Abilities`/`Networking` tests

## TODO
- [ ] Add `EntityId` to `IShipAbilityFacade`, replace `slot.Owner.Entity.Id` in `ShipAbilityEventMessage.Capture`, and guard teardown events
- [ ] Fix `ShotEffectCache.TryReserve` head-of-line stall and starvation interaction (done under [[Review/Bugs/Weapon_Fire_Rate_Low_FPS]])
- [ ] Scale `ShotPoolPrewarmer` roster prewarm & runtime growth and fix `ExpectedShotsInFlight` (done under [[Review/Bugs/Weapon_Fire_Rate_Low_FPS]])
- [ ] Decouple `CombatAttackCoordinator` intra-salvo cadence and `WeaponComponent.Engage` sub-frame turns from frame rate (cadence done under [[Review/Bugs/Weapon_Fire_Rate_Low_FPS]]; `Engage` turn handoff and ability-sync crash still open)
- [ ] Review affected EditMode tests in `Combat` and `Networking`

## Disposition 2026-11-26
- Superseded by [[Review/Bugs/Weapon_Fire_Rate_Low_FPS]].
- Root cause 1–2 (shots gated on pooled visuals, FIFO blocking) → fixed in `bd5c4d106` (2026-11-23): only the reload timer gates a salvo; admission queue removed.
- Ability sync crash (`Not entity found with id:`) → caster-id fix restored in `8787a78e1`.
