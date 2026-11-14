---
category: Bugs
status: done
created: 2026-11-14
completed: 2026-11-14
acceptance: pending-user
---
# Boost Weapon Power Firing Rate Fix

## Goal

- Make Boost Weapon Power visibly increase projectiles fired per second and sustained damage against an available target.
- User-confirmed requirement (2026-11-14): projectile fire rate within each burst MUST increase while Boost Weapon Power is active, in addition to shorter reloads between bursts.
- Hard constraint (2026-11-14): keep the existing Burst job / pooled-shot architecture and its CPU budget. No per-shot allocation, no on-demand `Instantiate` spikes, no new per-frame O(N) main-thread scans.
- Implemented 2026-11-14; Unity compilation, loaded settings and targeted asset persistence verified. Tests not run; gameplay/profiling acceptance pending.

## Important Values

- Catalog key 5 (`rid 7607833764655792207`): duration 7 s, recovery 50 s, `autoPower: 2`; `statModifier` damage 1, fireDelay 0.1, speed 0.5, shieldRegen 0.5.
- Damage per projectile, projectile speed and shots per salvo stay unchanged.
- Salvo = one burst from one `WeaponHardPoint`. Reload timer starts at burst start (runs concurrently with emission).
- Target timing while active: reload ×0.1 and within-burst `ShotInterval` ×0.1.
- Ideal sustained gain approaches 10× only at short range with ≥60 fps. Do not promise exactly 10× DPS.

### Why the boost barely works today (source + `WeaponsData.asset`, 2026-11-14)

Real cycle = max(reload, burst + flight + visual/muzzle tail) because of the effect gate.

| Family | Shots × interval | Reload | Range / speed → flight | Cycle now | Boosted now | Boosted after fix (no cap) |
|---|---|---|---|---|---|---|
| Laser 0 | 3 × 0.1 s | 2 s | 300 / 172.9 → 1.7 s | 2 s | ~1.9 s (≈1.05×) | 0.2 s |
| Turbolaser 1 | 3 × 0.2 s | 4 s | 450 / 172.9 → 2.6 s | 4 s | ~3.0 s (≈1.3×) | 0.4 s |
| Ion 2 | 2 × 0.2 s | 6 s | 500 / 71.5 → 7.0 s | ~7.2 s | ~7.2 s (1×) | 0.6 s |
| Missile 4 | 4 × 0.25 s | 10 s | 550 / 45.5 → 12.1 s | ~12.9 s | ~12.9 s (1×) | 1 s |

- Max-range flight dominates → Ion/Missile get zero benefit; Turbolaser ≈1.3×.

## Implementation

### Verified source findings — 2026-11-14

- `WeaponHardPoint.TryStartScheduledSequence` (`WeaponHardPoint.cs:154-167`): reload = `Reload × FireDelayMultiplier × FullSalvo × Torpedo × MainBattery`, snapshotted into `_reloadTimer` at burst start.
- `WeaponHardPoint.DelayBetweenShots` (`:52-55`): `ShotInterval × FullSalvo × Torpedo × MainBattery`; omits `FireDelayMultiplier` → no within-burst acceleration. Two duplicate multiplier chains = no single timing source.
- `WeaponHardPoint.IsBusy` (`:44`) = `_sequence.IsBusy || !_reloadTimer.IsComplete || turning`. `WeaponComponent.Tick` (`WeaponComponent.cs:478-483`) queues target selection only for non-busy hardpoints.
- `AttackSequenceState.StopEmitting` → `WaitingForEffects` while `_activeEffects > 0`. `TryCompleteEffect` rejects completions whose generation ≠ current. Starting a new burst while old effects fly would leave `_activeEffects` permanently inflated → hardpoint locks. Must change the counter, not just the gate.
- `ShotEffect.Update` (`ShotEffect.cs:142-161`): lease completes after travel timer + `IsVisualComplete()` + muzzle flash. Every active effect is a `MonoBehaviour.Update` → active-effect count is a direct CPU cost.
- `ShotEffectCache.Acquire`: empty stack → synchronous `Object.Instantiate` (on-demand spike). Runtime prewarm (`ShotPoolPrewarmer.Tick`) creates ≤1 shot/frame, 0.5 ms budget.
- `WeaponHardPoint.ExpectedShotsInFlight` (`:215-220`) = `ShotsPerSalvo × (1 + ceil(flight / CycleSeconds))`, unboosted. Roster prewarm uses it. Its comment already assumes overlapping salvos; runtime gate never allows overlap → runtime and design disagree.
- `CombatAttackCoordinator.BeginSequence/CommitDueEvent` (`CombatAttackCoordinator.cs:114-156, 442-476`): `NextTime = now + DelayBetweenShots`, `EarliestFrame = frame + 1` → max 1 shot per hardpoint per frame, interval rounds up to frame time, no catch-up. Due detection = `AttackDueJob` (Burst, `IJobParallelFor`) at ≥64 records, serial below.
- Every hit → ≥1 `ImpactRecord` (`WeaponComponent.cs:958-962`, beams `ceil(DamageDuration / 0.1)` ticks). 10× shots → 10× impact records in the due job.
- Timer clock: `com.script-utils.time` `Timer` uses `Time.time`; `ITimer.TimeLeft` + `AppendTime` exist → reload retiming needs no new timer API. Same clock as coordinator.
- Audio: `ShipSfxService.TryPlayWeaponShot` (`ShipSfxService.cs:104-112`) uses unboosted `weapon.ShotInterval` for loop/lease decision; fires per `ShotEmitted`.

### Generic `FireDelayMultiplier` consumers (must not gain within-burst effect)

| Ability | fireDelay | Effect if applied to shot spacing |
|---|---|---|
| Boost Weapon Power | 0.1 | intended |
| Assault | 0.25 | unintended 4× bursts |
| Pursuit (`BoostEnginePower`) | 0.25 | unintended |
| Concentrate Fire (ally) | 0.6 | unintended |
| Power to Engines | 2 | bursts slow down |
| Lock S-Foils | 3 | bursts slow down |
| `MissileInterceptorHardPoint` reload | uses generic | unchanged |

- Decision: ability-specific shot-interval channel. Avoid: reusing `FireDelayMultiplier` for spacing.
- Avoid: adding a field to `CombatStatModifier` — it is `[SerializeReference]` data in 12+ catalog entries; a new float deserializes as 0 unless every entry is re-saved.

### 1. Single timing source

- [x] Add `shotIntervalMultiplier` (0.1) to `BoostWeaponPowerSettings`; create `BoostWeaponPowerAbility` (implements `IStoppableShipAbility`, `IReplicaPersistentShipAbility`) that adds `statModifier` and calls `CombatModifiers.SetWeaponPowerShotInterval(x)`; `Stop` restores 1. Pattern = `FullSalvoAbility`.
- [x] Include the new field in `CombatModifiers.HasTemporaryEffects`.
- [x] `WeaponHardPoint`: one private method computes `(reloadMultiplier, intervalMultiplier)`; `TryStartScheduledSequence` and `DelayBetweenShots` both read it. FullSalvo/Torpedo/MainBattery stay in both; `FireDelayMultiplier` reload-only; weapon-power interval interval-only.
- [x] Update `BalanceAbilityAdapter` map for `BoostWeaponPowerSettings` (`shotIntervalMultiplier`, positive).
- Verify: other abilities' reload and spacing numerically unchanged (table above).

### 2. Separate firing readiness from projectile lifetime (CPU-bounded)

- [x] `AttackSequenceState`: `_activeEffects` counts all leased effects of the current pool lifetime, not per generation; completions decrement regardless of generation; `Release` resets it (pool already drops callbacks on release).
- [x] `StopEmitting` → `Ready` immediately; drop `WaitingForEffects` as an end-of-burst state.
- [x] New readiness gate in `TryStart`: refuse when `_activeEffects + ShotsPerSalvo > inFlightCap` (bounded window, not unbounded). Cap per Decision 1: passed into `WeaponHardPoint.SetData` by the owning unit's weapon setup (boosted only when the unit owns ability 5); fixed for the hardpoint's lifetime, no per-frame lookup.
- [x] Keep `ShotEffectPool` lease ownership unchanged; old callbacks cannot touch the newer burst (lease id + `_active` map already guard).
- [x] Update `AttackSequenceDiagnostics` semantics (`RecordUnmatchedCompletion` no longer fires for overlap).
- Verify: overlapping bursts each deal damage once; owner release/restart cannot leave `_activeEffects` stale; pooled reuse inherits no callbacks.

### 3. Retiming on activation, cancellation and expiry

- [x] Reload: `_reloadTimer.AppendTime(TimeLeft × (newMult / oldMult − 1))`. Example 4 s reload, 2 s left → 0.2 s on activation; reverse on expiry.
- [x] Pending inter-shot: new `CombatAttackCoordinator.RetimeSequences(IWeaponPresenter owner)` — one pass over `_sequences`, matching owner, `NextTime = now + (NextTime − now) × ratio` using each hardpoint's cached old/new interval multiplier. Keep `EarliestFrame`; no unconditional shot.
- [x] Trigger only when a hardpoint's effective multipliers change: hardpoint caches last multipliers; `WeaponComponent` handles `CombatModifiers.Changed` → compare → retime. `Changed` fires for unrelated reasons (cloak, focus, suppression) → compare first, no work otherwise.
- [x] Applies to all cadence modifiers (FullSalvo, Torpedo, MainBattery too); same mechanism, cost only on change.
- Verify: activation mid-reload/mid-burst, expiry mid-wait, cancel, auto-power, stacked changes.

### 4. Within-burst projectile acceleration

- [x] `DelayBetweenShots` × weapon-power interval multiplier (0.1). Activation mid-burst retimes via step 3.
- [x] Keep `EarliestFrame = frame + 1` (≤1 shot/hardpoint/frame, no catch-up). Effective floor = frame time: Turbolaser 0.02 s → ~0.033 s at 60 fps, 0.033 s at 30 fps; Laser 0.01 s → frame-bound. Document; do not add multi-shot-per-frame emission.
- [x] Preserve velocity, damage per hit, `ShotsPerSalvo`.
- Verify: shot timestamps shorter within and between bursts; no extra shots.

### 5. Pools, jobs and consumers (CPU budget)

- [x] Prewarm must cover the in-flight cap so boosted bursts never hit `Create(true)` mid-combat. Cap must be ≤ prewarm target per hardpoint.
- [x] `ShotPoolPrewarmer.PrewarmRosterAsync` → `AddLoadout`: for `ShipData` whose `Abilities` contains `BoostWeaponPower`, count each entry with the boosted `ExpectedShotsInFlight`. Squadrons and defend platforms unchanged.
- [x] `WeaponHardPoint.GetPool` passes the same cap as the pool target (single source with the gate).
- [x] Loading budget preserved; runtime log review queued for user acceptance. Check the `[ShotPoolPrewarmer] Roster target` log for boosted prefabs.
- [x] Job path source-reviewed; workload/capacity measurements queued for user capture: no new jobs needed. `AttackDueJob` input count grows with sequences/impacts (`NativeArrayBuffer.EnsureCapacity` grows once); `BoltFlightBatch` (`INITIAL_CAPACITY 256`) grows once. Record capacity growth with `BattlePerformanceCapture` combat workload counters.
- [x] Target selection remains batched; measurement queued for user acceptance. Boosted hardpoints become non-busy ~10× more often → more `QueueTargetSelection` requests into `TargetSelectionBatch` job. Acceptable (batched); measure `TargetRequests`.
- [x] Audio: pass effective interval to `ShipSfxService.TryPlayWeaponShot` so boosted bursts use the loop path instead of one-shot spam. Adds `Audio` category.
- [x] `BalanceDpsEstimator`, `BattleSimulator` (`SimMount` uses `CycleSeconds`, no gate) — removing the gate aligns runtime with these; no formula change (Decision 2).
- [x] Update catalog description key 5: "Weapons fire 10× faster and reload 90% faster. Movement speed and shield regeneration fall to 50% for 7 s. Recovers in 50 s." Sync faction descriptions.
- Verify: no `Battle.Projectile.Instantiate.*` samples during boost in a profiled capture (user-run).

### Decisions — user, 2026-11-14

1. **In-flight cap → Option B (full 10× at any range).**
   - Chosen: hardpoints on units whose `ShipData.Abilities` contains `ShipAbilityId.BoostWeaponPower` (5) get a boosted cap; all other hardpoints keep cap = `ExpectedShotsInFlight(profile)`.
   - Boosted cap = `ExpectedShotsInFlight` with cycle `max(Reload × reloadMult, (ShotsPerSalvo − 1) × ShotInterval × intervalMult)`; both mults read from key 5 settings (0.1 / 0.1 → cycle = `CycleSeconds × 0.1`). Add an overload `ExpectedShotsInFlight(profile, reloadMult, intervalMult)`; keep the existing one as the 1/1 case.
   - Cost accepted: ~4–5× active `ShotEffect`s per boosted hardpoint for 7 s (Turbolaser 6 → 24, Missile 12 → 56); extra idle pool memory for the battle.
   - Why: user wants full boost at all ranges. Avoid: uncapped overlap (on-demand `Instantiate` spikes).
   - Side effect: Full Salvo / Power to Main Batteries / Assault cadence also overlap after the gate change but stay limited by the unboosted cap on units without ability 5 (still faster than today at long range; no new pool cost).
2. **Baseline change → accepted.**
   - Removing the effect gate lets unboosted long-flight weapons fire on reload: Missile ~12.9 → 10 s cycle (+27% DPS at max range), Ion ~7.2 → 6 s (+20%).
   - Why: matches `WeaponProfile.CycleSeconds`, `BalanceDpsEstimator`, `BattleSimulator` (`SimMount`). No baseline compensation; no stat retuning in this fix.

### 6. Verification and closure

- [x] Regression coverage (authored, not run): overlapping bursts, cross-generation completion counting, release/restart reset, cap gate, retime math (reload + pending shot), unchanged spacing for other abilities, unchanged damage/speed.
- [x] Categories: `Combat`, `Abilities`; plus `Audio` (step 5), `Balance` (adapter), `Networking` if replica persistence changes.
- [x] Tests disabled under local override; report as not run.
- [x] Compile; import/save `ShipAbilityCatalog.asset` via Unity persistence flow (re-serialize changed path only).
- [x] User acceptance queued (not performed): same weapon, stationary durable target, fixed distance/game speed; shot timestamps/counts and damage before/during/after; close vs far range; mid-reload/mid-burst activation; expiry; cancel; owner/target death; slow missiles; profiler capture for Instantiate/Update cost.
- [x] Update canonical weapon timing guidance (`Rules/BATTLE_PERFORMANCE_GUIDELINES` shot-storage bullet: in-flight cap contract) and link here before closure.
- [x] Close to `Done/Bugs` with `acceptance: pending-user`; runtime gates → `TODOs/User_Acceptance_Queue.md`.

## Verification and outcome

- Unity loaded `BoostWeaponPowerAbility`, catalog interval/reload `0.1 / 0.1`; Editor compiling/updating both false. Targeted catalog + four faction asset reserialization/save completed; no new Console errors after this task's import cursor.
- Regression source authored/updated: `WeaponCadenceTests`, `AttackSequenceStateTests`, `CombatAttackCoordinatorDueTests`, `ShotEffectPoolLifecycleTests`, `ShipAbilitySymmetryTests`; audio/reflection fixture signatures updated. Tests not run under local override.
- Integration correction: old roster prewarm covered only the largest unit. Pools now register summed live capacity during initialization, reserve it through orphan-flight retirement and pre-size lease dictionaries. Fixed per-pool admission thresholds keep existing ships firing while newly added units wait for ≤1-shot/frame budgeted growth.
- Final emission now ends its salvo immediately; removed the extra trailing inter-shot wait. Pending waits retime on modifier changes; one-shot-per-frame gate remains.
- `Battle.Projectile.Instantiate.*` also wraps budgeted prewarm. User capture must distinguish nested `Prewarm.*` and check `AttackSequenceDiagnostics.PoolExpansions` for on-demand growth.
- Guidance extracted → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Weapon cadence and pooled overlap · 2026-11-14]]; stale Boost values corrected → [[GameDesign/Abilities_And_Superweapons]].
- Gameplay, audio and profiler gates → [[TODOs/User_Acceptance_Queue#Boost Weapon Power Firing Rate Fix]].

## Rules

- Read `Rules/BATTLE_PERFORMANCE_GUIDELINES.md` (shot storage, prewarm budgets: 4 ms loading, ≤1 shot/frame runtime) before pool changes.
- Implementation closed with `acceptance: pending-user`; no test or runtime performance pass claimed.

## Files

- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset` (key 5, rid `7607833764655792207`)
- `Assets/Scripts/Services/ShipAbilities/Abilities/BoostWeaponPower/BoostWeaponPowerSettings.cs` (+ new `BoostWeaponPowerAbility.cs`)
- `Assets/Scripts/Services/ShipAbilities/Abilities/StatModifierAbility.cs`, `FullSalvo/FullSalvoAbility.cs` (pattern)
- `Assets/Scripts/Components/CombatModifiers/CombatModifiers.cs`
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`
- `Assets/Scripts/Components/Weapon/AttackSequenceState.cs`, `AttackSequenceDiagnostics`
- `Assets/Scripts/Components/Weapon/CombatAttackCoordinator.cs`, `AttackDueJob.cs`, `WeaponComponent.cs`
- `Assets/Scripts/Components/ViewComponents/Weapon/ShotEffect.cs`, `ShotEffectPool.cs`, `ShotEffectCache.cs`, `ShotPoolPrewarmer.cs`, `BoltFlightBatch.cs`
- `Assets/Scripts/Services/Audio/ShipSfxService.cs`
- `Assets/Scripts/Editor/Balance/BalanceAbilityAdapter.cs`
- `Assets/Scripts/Entities/Ship/Data/ShipData.cs` (`Abilities`, read-only use)
- `Assets/Scripts/Services/ShipAbilities/ShipAbilityId.cs` (`BoostWeaponPower = 5`)
- [[TODOs/TODOs]]
