---
category: Features
status: done
created: 2026-11-26
completed: 2026-11-26
acceptance: pending-user
system:
  - vfx
  - battle-rendering
  - health
---
# Hull Hit Blast VFX Plan

## Goal

- Play a hull blast on **every hull-damaging hit** on ships, stations and defend platforms, not only when the scorch level rises.
- Never spam: per-unit and battle-wide limits that stretch as more units are blasting.
- Spend nothing on blasts the player cannot see: fog-hidden, off camera, too small on screen.

## Outcome (2026-11-26)

- Implemented and Unity-compiled (0 errors, 0 warnings). Tests authored/updated, **not run** (local override).
- Acceptance pending (user): in-game look + `[VfxPools] HullBlasts …` log line → [[TODOs/User_Acceptance_Queue#Hull Hit Blasts]].

## User Decisions (2026-11-26)

- Spot: area around the hit hardpoint with a random offset; never below/inside the hull. Fallback: random valid spot.
- Level-rise blasts: removed.
- `DamageType.PrecisionStrike` (super weapons, Lucky Shot): no blast.
- No scorch mark at the blast spot; plain flash.

## Implementation

- `HealthModel.HullDamaged(int hardPointIndex, DamageType)` → raised after `OnValueChanged` only when `Hull` dropped and the unit survived. Not for shield-only hits, repair, `Restore`, `ApplyReplica`, killing hit. Redirected hits (destroyed hardpoint) keep the hit index.
- `IHullBlastSpots.HullDamaged` replaces `Restored` (forwarded by `HealthComponent`).
- `UnitHullScorchBlasts` (per unit, injects `[Key(EntityBindType.ViewTransform)] Transform hull`):
  - Skip `PrecisionStrike`; skip while `Time.time < _readyAt`.
  - Spot = hit index if it has VFX and is not below the hull centre plane (`dot(anchor − hull, hull.up) ≥ 0`); else 8 random attempts.
  - Offset: random direction in hull deck plane (`right`/`forward`), flipped towards the hull middle; distance `≤ 5 × scale` (√-uniform); lift `0…0.25 × radius` along `hull.up`.
  - On successful play: `_readyAt = Time.time + pool.UnitInterval`. A skipped blast starts no interval.
- `HullScorchBlastPool.TryPlay(anchor, worldOffset, fog, scale)` → `bool`. Order: fog → per-frame cap / no idle / `InUseCount ≥ MaxActive` → blast sphere vs frustum (planes once per frame) → projected diameter < min px. Never grows (prewarm = `MaxActive`).
- `UnitInterval = limits.UnitInterval × max(1, InUse / (MaxActive × 0.5))` → doubles at full load (fair share across visible armada).
- `HardPointExplosionPool`: added `IdleCount`, `InUseCount` only.
- `HullBlastLimits` (`[Serializable]`, field `SkirmishScope.hullBlastLimits`; initializer defaults apply until tuned in the scene).
- Editor/Dev log at battle close: `[VfxPools] HullBlasts played= skippedFog= skippedOffscreen= skippedSmall= skippedCap=` (cooldown skips not counted; they never reach the pool).

## Important Values

| Limit | Default |
|---|---|
| `maxActive` (also prewarm) | 24 (was prewarm 8) |
| `maxStartsPerFrame` | 4 |
| `unitInterval` | 0.6 s (blast ≈ 2 s → ≈ 3 alive per unit) |
| `minScreenPixels` | 6 px diameter |
| Offset radius | 5 × hardpoint VFX scale (blast start size 7.5 × scale) |

## Decision

- Per-unit "active max 3" from the draft → replaced by `unitInterval` (same bound, no per-owner tracking in the shared pool).
- Frustum test on the blast sphere, not unit bounds. `Why`: the blast itself is what must be visible; no hull-bounds source needed.
- `Avoid`: world hit points from shot visuals — undrawn bolts still deal damage.

## Edge Cases

- Unit fog-hidden → no blast, even if it becomes visible during the blast's lifetime (one-shots are not replayed).
- Online client (`ApplyReplica`) → no hit event → **no hull blasts on clients**. Follow-up only if multiplayer needs it (replica carries no hit index/damage type).
- Unit with no hardpoints → no spots → no blast (unchanged).
- Hardpoint spots all below the hull centre → no blast.

## Files

- `Assets/Scripts/Components/Health/HealthModel.cs`, `HealthComponent.cs`, `IHullBlastSpots.cs`
- `Assets/Scripts/Entities/BaseEntity/UnitHullScorchBlasts.cs`, `EntityFeatureBindings.cs` (doc)
- `Assets/Scripts/Services/UnitExplosion/HullScorchBlastPool.cs`, `IHullScorchBlastPool.cs`, `HullBlastLimits.cs` (new), `HardPointExplosionPool.cs`
- `Assets/Scripts/Services/VContainer/Scopes/SkirmishScope.cs`

## Tests (authored, not run)

- `UnitHullScorchBlastsTests` (`Ships`): rewritten for per-hit blasts — hit spot, offset above/inwards, precision skip, unit interval, skipped play keeps no interval, below/unknown spot fallback, spot without VFX, dispose.
- `HealthModelTests` (`Health`): `HullDamaged` only on hull drop with index/type; none for shield-only, repair, restore, killing hit.
- `HullScorchBlastPoolTests` (`Health`, new): visible play at offset, fog, behind camera, too small, max active without growth, per-frame cap, interval stretch.
- Reviewed, no change: `HardPointExplosionPoolTests` (Play/Tick unchanged), `SerializedReferenceTests` (`hullScorchBlast` still required).

## Guidance

- Reusable rule extracted → [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Hull hit blasts · 2026-11-26]]. VFX table updated → [[TODOs/Optimization/Cinematic_Explosion_Vfx_Optimization]].
