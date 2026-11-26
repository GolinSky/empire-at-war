---
category: Features
status: todo
created: 2026-11-26
system:
  - vfx
  - battle-rendering
  - health
---
# Hull Hit Blast VFX Plan

## Goal

- Play a hull blast on **every hull-damaging hit** on ships, stations and defend platforms, not only when the scorch level rises.
- Never spam: per-unit and battle-wide limits, shared fairly across all visible damaged units.
- Spend nothing on units the player cannot see: off camera, hidden by fog, or too small on screen.

## Current State (source-verified 2026-11-26)

- `UnitHullScorchBlasts` (per unit, `EntityFeatureBindings.RegisterHullDamage`) → listens to `IHealthModelObserver.OnValueChanged` → scorch level `UnitRenderStateComponent.GetHullDamageLevel` (0–31) → on a rise plays `min(rise, 3)` blasts at **random** `IHullBlastSpots` spots (hardpoint `TryGetSurfaceVfx` anchors, 8 attempts).
- Registered by `ShipInstaller`, `SpaceStationInstaller`, `DefendPlatformInstaller`. Squadrons: none.
- `HullScorchBlastPool` wraps `HardPointExplosionPool`; prefab `HullScorchBlastVfx` (Explosion FREE 1, start size 7.5); prewarm `HULL_SCORCH_BLAST_PREWARM` = 8 in `SkirmishScope`.
- Pool limits today: **none**. Empty idle list → `Object.Instantiate` mid-battle (`_grownCount`).
- Pool visibility today: fog → `forceRenderingOff` (still simulates); frustum test only toggles sustained emission. One-shots always play and simulate off screen.
- Scorch marks are procedural per damage level (`ShipHullDamage.hlsl`), not placed at hit points.
- Damage carries no world position: `HealthModel.ApplyDamage(damage, damageType, hardPointId)`. Weapons aim at `hardPointModel.Transform` → the **hit hardpoint id is the hit location**.
- `OnValueChanged` also fires for shield-only hits, shield regen, repair, upgrade, suppression, `Restore`, `ApplyReplica` → it cannot mean "hull was hit".

## Issues To Expect

| # | Issue | Consequence | Answer in this plan |
|---|---|---|---|
| 1 | Hit volume | Bolt fleets land many hits/s per ship; `SustainedBeamAbility` damages every frame; `DamageLiveHardPoints` splits one hit | Per-unit cooldown + active cap + global cap + per-frame start cap |
| 2 | Pool growth | Unlimited plays → runtime `Instantiate` hitches | Pool never grows for hull blasts; no idle → skip |
| 3 | Overdraw | Large transparent billboards stacked on one ship (open item #4 in [[TODOs/Optimization/Cinematic_Explosion_Vfx_Optimization]]) | Active caps; measure GPU before raising |
| 4 | Off-screen / fog / far | Simulating effects nobody sees | Gate before `Play`: fog, unit bounds vs frustum, min screen size |
| 5 | Hidden hit, later visible | Skipped blast is not replayed | Accept: one-shots only |
| 6 | "Hull hit" signal | `OnValueChanged` mixes in shield/repair/restore | New `HealthModel` event with hardpoint id, raised only when hull drops |
| 7 | Multiplayer replica | Clients get `ApplyReplica` percentages, no hit ids | Replica hull drop → same event with id `-1` → random spot, same limits |
| 8 | Hit on destroyed / locked hardpoint | `TryGetSurfaceVfx` false; damage redirected | Fall back to a random valid spot |
| 9 | Killing blow | `OnValueChanged` precedes `OnDestroy`; death explosion owns it | Skip when `IsDestroyed` (kept) |
| 10 | Blast place ≠ scorch place | Marks are procedural; blasts at hardpoints | Accept for now (see Open Questions) |
| 11 | Super weapons / Lucky Shot | Already play their own orbital/ability VFX; `PrecisionStrike` id 0 | Proposed: skip `DamageType.PrecisionStrike` |
| 12 | Many units visible | Fixed per-unit cap lets 3 ships eat the global budget | Fair share: per-unit cap shrinks as more units are blasting |

## Decision (proposed, needs user OK)

- Trigger: hull hit event, not scorch-level rise. Remove the level-rise path.
- Spot: hit hardpoint's surface anchor; fallback random spot. `Why`: matches where shots land. `Avoid`: world hit point from shot visuals — bolts can be undrawn and damage is applied separately from visuals (firing independent of visuals, 2026-11-23).
- Shape (AGENTS.md "pick by what the code is"): per-unit rules stay in `UnitHullScorchBlasts`; battle-wide budget and visibility in `HullScorchBlastPool` (service owns its decisions, no extra budget class).

## Limits (proposed defaults, tune in-game)

| Limit | Owner | Default | Rule |
|---|---|---|---|
| Per-unit cooldown | `UnitHullScorchBlasts` | 0.3 s | Hits inside it are dropped |
| Per-unit active max | pool, by owner key | 3 | Effective cap = `clamp(GlobalMax / unitsWithActiveBlasts, 1, 3)` |
| Global active max | pool | 24 | Includes fading blasts |
| Starts per frame | pool | 4 | Spreads volley spikes |
| Min screen size | pool | 6 px | Projected blast diameter (7.5 × scale) |
| Pool growth | pool | 0 | Prewarm = global max (24); no `Instantiate` while fighting |

- Values live in a serialized settings asset (extend an existing VFX settings asset if one fits; else `HullBlastSettings`), not in code constants.
- Drop, never queue: a late blast reads as lag.

## Visibility Gate (pool, before `Play`)

1. `fog.IsHiddenByFog` → skip.
2. Frustum planes computed once per frame (pool already does it in `Tick`); unit hull bounds sphere vs planes → skip when outside. Bounds source: same hull renderer bounds as `UnitExplosionService` (line ~80) — verify a cheap cached radius exists, else add one on the health/render component.
3. Projected size of the blast < min px → skip.
4. Caps (global, per-frame, per-unit fair share) → skip.
- Return `bool` so the unit only starts its cooldown when a blast actually played.

## Implementation Steps

1. [ ] `HealthModel`: add `event Action<int> HullDamaged` (hardpoint id; `-1` unknown). Raise in `ApplyDamage` only when `Hull` dropped and not destroyed; raise `-1` in `ApplyReplica` on hull drop. Not on `Restore`/repair/upgrade. Expose through `IHealthModelObserver` or `IHullBlastSpots`.
2. [ ] `UnitHullScorchBlasts`: subscribe to `HullDamaged`; drop level tracking and `Restored` handling; cooldown via game time; skip `PrecisionStrike` if confirmed (needs damage type on the event → `Action<int, DamageType>`).
3. [ ] `IHullScorchBlastPool.TryPlay(owner, anchor, fog, scale, hullCenter, hullRadius)` → `bool`; global/per-frame/per-owner caps, frustum and screen-size gate; no growth.
4. [ ] `HardPointExplosionPool`: add owner tracking + `ActiveCount(owner)` / no-grow `TryTakeIdle` without changing hardpoint-explosion behavior.
5. [ ] Settings asset + `SkirmishScope` wiring; prewarm = global max.
6. [ ] Diagnostics: extend Editor/Dev `[VfxPools]` line with `played`, `skippedFog`, `skippedOffscreen`, `skippedSmall`, `skippedCap`, `skippedCooldown`.
7. [ ] Tests (review + update; not run under local override): `UnitHullScorchBlastsTests` (rewrite: per-hit, cooldown, destroyed, replica `-1`, fallback spot), `HealthModelTests` (event only on hull drop; not on shield-only/repair/restore), new pool tests (caps, fair share, frustum skip, no growth). `HardPointExplosionPoolTests` reviewed for owner changes. Categories: `Health`, `Ships`, `Combat`.
8. [ ] User: heavy battle → read `[VfxPools]` line, check look, GPU capture with fleet on screen (`RenderAuditAblationCapture`).

## User Decisions (2026-11-26)

- Spot: area around the hit hardpoint with a random offset; never below/inside the hull. Fallback: random valid spot.
- Level-rise blasts: removed.
- `PrecisionStrike` (super weapons, Lucky Shot): skipped.
- No scorch mark at the blast spot; plain flash.

## Files

- `Assets/Scripts/Entities/BaseEntity/UnitHullScorchBlasts.cs`
- `Assets/Scripts/Services/UnitExplosion/HullScorchBlastPool.cs`, `IHullScorchBlastPool.cs`, `HardPointExplosionPool.cs`
- `Assets/Scripts/Components/Health/HealthModel.cs`, `HealthComponent.cs`, `IHullBlastSpots.cs`
- `Assets/Scripts/Services/VContainer/Scopes/SkirmishScope.cs`
- `Assets/Scripts/Tests/Editor/Ships/UnitHullScorchBlastsTests.cs`, `Tests/Editor/Health/HealthModelTests.cs`, `Tests/Editor/Health/HardPointExplosionPoolTests.cs`
