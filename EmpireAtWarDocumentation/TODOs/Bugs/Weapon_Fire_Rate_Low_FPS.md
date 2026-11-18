---
category: Bugs
status: in-progress
created: "2026-11-18\r"
---
# Weapon Fire Rate Drops at Low FPS

## Goal
- Ships keep their designed fire rate at 10–20 FPS (host, online 3 humans vs 4 AI, 2026-11-17 battles 07/08).
- Damage cadence must not depend on frame rate or on visual projectile availability.

## Evidence
- Logs: `Logs/OnlineBattles/20261117T1650Z/` (git-ignored). `battles/INDEX.txt`, `battles/battle07_*`, `battle08_*`.
- Release `Player.log` has no timestamps, FPS or shot counts → symptom is user-reported; cause below is from source.
- User: host + 2 clients at 10–20 FPS; host ships "don't fire or fire slowly".

## Root cause (source, 2026-11-18)
1. **Shots are gated on a free visual projectile.** `WeaponHardPoint.TryStartScheduledSequence` → `ShotEffectPool.TryReserveSalvo` → `ShotEffectCache.TryReserve`. No idle `ShotEffect` → salvo does not start → no damage.
2. **Runtime pool growth = 1 projectile per frame for all prefabs.** `ShotPoolPrewarmer.MAX_CREATIONS_PER_FRAME = 1`, `BUDGET_MILLISECONDS = 0.5`.
   - Roster prewarm covers only the max demand of **one** roster unit per prefab (`PrewarmRosterAsync`).
   - 7 factions × ~20 ships → hundreds of bolts needed; at 15 FPS growth is 15 bolts/s total.
3. **FIFO head-of-line blocking.** `ShotEffectCache.TryReserve` admits only `_waiting.First`; a big salvo at the head blocks 1-shot salvos behind it.
4. **Return depends on visuals.** `ShotEffect.Update` returns a bolt only when `_busyTimer`, `IsVisualComplete()` and muzzle flash are done → in-use time grows with VFX length.
5. **Starved mounts re-run target selection every frame.** `WeaponHardPoint.Attack` ignores `TryAttack`'s result; the mount stays not busy and `_retryAt` is not advanced → `QueueTargetSelection` each frame → more CPU → lower FPS → slower pool growth (feedback loop).
6. **Salvo shots quantize to frames.** `CombatAttackCoordinator`: one shot per frame max (`EarliestFrame = frame + 1`) and `NextTime = now + DelayBetweenShots` (drift, not `NextTime + delay`). At 10 FPS a 0.05 s shot interval becomes 0.1 s.
   - Reload uses `_reloadTimer` (time-based) and starts at salvo start → long salvos eat into reload but do not double it.

### Why missiles kept flying while bolts were missing (user report 2026-11-18)
- Pools are per shot prefab (`ShotPoolPrewarmer._caches`). `WeaponsData.asset`: bolts → `Projectile.prefab` (10 profiles) + `DualProjectile.prefab` (7), shared by every faction's lasers/turbolasers/ion **and fighter lasers**. Missiles → `ConcussionMissileShot.prefab` (4), `ProtonTorpedoShot.prefab` (3), few launchers.
- Demand per mount: bolts reload 0.9–5 s with 2–8-shot salvos; missiles reload 6–7.5 s, 1–2 shots → bolt caches need many times more admissions per second.
- Prewarm = one roster unit's demand per prefab → shortfall ≈ number of units sharing the prefab → huge for bolts, small for missiles.
- Runtime growth is round-robin per starved cache (`PrewarmNext` dequeue → 1 → re-enqueue), not by deficit → missile caches fill fast, bolt caches stay starved.
- Bolt admission FIFO is crowded (fighters + capitals, multi-shot salvos block the head); missile FIFO is short.
- A missile stays leased and visible ~10–12 s (speed 32.5–45.5, range 350–550, plus trail fade in `MissileShot.IsVisualComplete`); a bolt ≤ ~3 s → missiles dominate the screen even at the same launch rate.
- Plan impact: growth must be weighted by deficit/waiting shots, and fighter bolts should not starve capital main batteries (separate caches or priority).

### Added 2026-11-18 (Argon analysis, source-checked)
- `WeaponHardPoint.ExpectedShotsInFlight` counts flight time only. Missile leases also wait for the trail fade (`MissileShot.IsVisualComplete`); `BeamShot` has speed 0 → flight 0 → caps and prewarm are too low for both.
- `WeaponComponent.Engage`: any `TurnTowards > 0` defers the salvo to `FireTurnedSalvos` on a later frame.
- Fixed step 0.02 s with `Maximum Allowed Timestep` 0.333 → several `RadarScanService.FixedTick` + physics steps per frame at 10–20 FPS.
- Fighter torpedo mounts re-ran target selection every frame between attack runs → fixed in [[TODOs/Bugs/Squadron_Count_Stacking_And_FPS]].

## Plan
### Phase 1 — Measure (Development build, host)
- [ ] Log per-cache `AdmissionWaits`, `MaxAdmissionWaitSeconds`, `RuntimeCreated`, `PeakInUse` (`ShotEffectCache`, dev only) at battle end and every 30 s.
- [ ] Count target-selection requests per frame from starved mounts (`CombatAttackCoordinator.LateTick` workload).
- [ ] Repro: Skirmish, 3 AI UltraHard + host fleet ≥ 20 capitals, `frameRateLimit` 15 vs uncapped; compare shots/min per ship.

### Phase 2 — Decouple damage from VFX (decision needed)
- [ ] Option A (recommended): damage sequence never waits for a bolt; when no bolt is free, skip the visual (or reuse oldest) but still schedule the impact.
- [x] Option B parts done (2026-11-18): time budget only (`MAX_CREATIONS_PER_FRAME` removed), growth picks the cache with the largest deficit (waiting shots included), roster prewarm sums ship/platform demand per player slot. Option A decision still open.
- [x] Remove FIFO head-of-line blocking (`TryReserve` admits any waiter that fits).

### Phase 2b — Split pools: strikecraft vs ships (user proposal 2026-11-18, agreed)
- [x] Key `ShotPoolPrewarmer._caches` by (shot prefab, pool group), where group = `Strikecraft` (owner class `IsStrikecraft()`) or `Ships` (ships, stations, platforms).
- [x] Separate admission FIFOs → fighter salvos can never block capital salvos, and the reverse.
- [x] Separate prewarm (done via `ShotPoolPrewarmer.BuildStock`: per-faction demand × slots using that faction; fighters = station hangar `MaxActive` × squadron loadout; test `BuildStock_GrowsWithSlotCountPerFaction`): ships from roster ship loadouts; fighters from squadron loadouts × expected live squadrons (station start count + carrier bays), not one unit.
- [ ] Separate growth budget per group, weighted by waiting shots; fighter group may have a hard cap (skip the bolt visual when full; damage still applies once Phase 2 Option A lands).
- Files: `ShotPoolPrewarmer.cs` (`CreatePool`, `GetCache`, `PrewarmRosterAsync`), `WeaponHardPoint.GetPool`.

### Phase 2c — In-flight estimate (must fix)
- [x] (done via `ShotEffect.PostImpactSeconds`; missile trail lifetime + intercept explosion duration/lifetime, bolt muzzle flash not included) `WeaponHardPoint.ExpectedShotsInFlight` counts only `Range / ProjectileSpeed`. Add the post-impact visual lifetime: missile trail fade (`MissileShot.IsVisualComplete` waits for `trail.IsAlive` + intercept explosion) and bolt muzzle flash.
- [x] (growth + hold + fade) Beams: `BeamShot` has `ProjectileSpeed` 0 → flight 0 s → estimate = one salvo; use the beam duration (`DamageDuration`/beam visual length) instead.
- [x] (`ExpectedShotsInFlight_CoversFlightPlusPostImpactLease`, Turbolaser + Torpedo; beam not covered; not run) Test (Combat): estimate ≥ shots alive at steady fire for missile, bolt and beam profiles (relationship, not tuned numbers).

### Phase 3 — Frame independence
- [x] (revised: no per-frame cap, late shots launched ahead by their lateness via `ShotEffect.LateSeconds`; bolts and missiles only) Salvo cadence: `NextTime += DelayBetweenShots`; emit all shots due this frame (cap per frame), keeping the "no catch-up burst after a long frame" rule explicit with a bound.
- [x] (`WeaponComponent.AttackOrDelay`: keeps its place in the storage queue) Failed admission → `DelayRetry` (no per-frame target selection).

## Agent handoff
- Read this plan, its linked plans and the live source first; source wins over this note.
- Follow `AGENTS.md`: no Play Mode, no tests (local override). Update/add the tests listed here and report them as not run.
- Unity assets: change them through Unity tooling, re-serialize the changed paths, check the console.
- Hand back: changed files, checked-off steps, deviations and open questions, then ask the user for review.

## Tests (review/add; not run — local override)
- `Combat`: `CombatAttackCoordinator*Tests`, `ShotEffectCache*`/`ShotPoolPrewarmer*` tests, Boost Weapon Power cadence tests (`e18290ca9`).
- New: cadence at 10 FPS equals cadence at 60 FPS (simulated time steps); starved pool still applies damage (if Option A).

## Files
- `Components/ViewComponents/Weapon/ShotPoolPrewarmer.cs`, `ShotEffectCache.cs`, `ShotEffectPool.cs`, `ShotEffect.cs`
- `Components/ViewComponents/Health/WeaponHardPoint.cs`
- `Components/Weapon/CombatAttackCoordinator.cs`, `WeaponComponent.cs`

## Related
- [[TODOs/Optimization/Online_Battle_Low_FPS]] (FPS side of the same loop).
