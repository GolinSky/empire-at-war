---
category: Optimization
status: in-progress
created: 2026-11-19
updated: 2026-11-19
---
# Projectile Visual Reduction Plan

## Goal

- Fewer, faster, smaller missiles and torpedoes on screen; same sustained damage.
- Smaller projectile pools and fewer live particles per launcher.
- Then apply a milder version of the same pattern to bolts.

## Decision

- Chosen: change family base stats in `WeaponsData.asset` + trail values in the two missile prefabs. No C# gameplay change needed.
- Why: every missile weapon derives from 5 family rules; `DamagePerSecond = damage × shotsPerSalvo / CycleSeconds`.
- Avoid: per-weapon overrides, LOD/culling code, changing `MissileShot` lifecycle.
- Rule: hold `damage × shots / reload` constant per family → sustained DPS unchanged.

## Current Values (source-verified 2026-11-19)

Family rules: `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`.

| Family | Damage | Shots | Interval | Reload | Range | Speed | Flight at max range | Size |
|---|---|---|---|---|---|---|---|---|
| Missile (4) | 32 | 4 | 0.25 s | 5 s | 550 | 45.5 | 12.1 s | 1.2 |
| Torpedo (5) | 72 | 2 | 0.4 s | 6 s | 350 | 32.5 | 10.8 s | 9 |
| IonTorpedo (6) | 51.2 | 1 | 0 | 7.5 s | 350 | 32.5 | 10.8 s | 9 |
| FighterMissile (22) | 32 | 1 | — | 7.5 s | 80 | 45.5 | 1.8 s | 1.2 |
| FighterTorpedo (23) | 72 | 1 | — | 4 s | 60 | 32.5 | 1.8 s | 9 |

Trail (`Trail` particle system, world emission by distance):

| Prefab | Rate / unit | Lifetime | Max particles | Live particles per shot | Trail length |
|---|---|---|---|---|---|
| `ConcussionMissileShot.prefab` | 6 | 0.8 s | 300 | ≈218 | 36 units |
| `ProtonTorpedoShot.prefab` | 24 | 0.54 s | 300 | 300 (capped; 421 uncapped) | 17.5 units |

- Trail width = `profile.Size.x × 0.6` (set in `MissileShot.Play`) → torpedo trail particle 5.4, head 9.
- One Missile launcher at max range: ≈12 missiles flying → ≈2,600 trail particles.
- One Torpedo launcher at max range: ≈4 flying → ≈1,200 trail particles.
- Pool cap = `WeaponHardPoint.ExpectedShotsInFlight` = `shots × (1 + ceil((range / speed + PostImpactSeconds) / cycle))` → Missile ≈16, Torpedo 6 per launcher; Missile under the 0.1 / 0.1 boost ≈112.

## Phase 1 — Missiles and torpedoes

### Target values

| Family | Damage | Shots | Reload | Speed | DPS before → after |
|---|---|---|---|---|---|
| Missile | 32 → 256 | 4 → 1 | 5 → 10 s | 45.5 → 91 | 25.6 → 25.6 |
| Torpedo | 72 → 288 | 2 → 1 | 6 → 12 s | 32.5 → 65 | 24 → 24 |
| IonTorpedo | 51.2 → 102.4 | 1 | 7.5 → 15 s | 32.5 → 65 | 6.83 → 6.83 |
| FighterMissile | 32 | 1 | 7.5 s | 45.5 → 91 | unchanged |
| FighterTorpedo | 72 | 1 | 4 s | 32.5 → 65 | unchanged |

- Rejected 2026-11-19: Missile with 2 shots (damage 128, interval 0.25 s).
- Fighter families: speed + trail only. `FighterTorpedoHardPoint` fires one torpedo per attack run → reload does not set its cadence.

| Prefab | Rate / unit | Lifetime | Max particles | Live per shot | Trail length |
|---|---|---|---|---|---|
| Concussion | 6 → 1.5 | 0.8 → 0.3 s | 300 → 64 | ≈41 | 27 units |
| Proton torpedo | 24 → 3 | 0.54 → 0.25 s | 300 → 64 | ≈49 | 16 units |

- Sizes unchanged: torpedo 9, missile 1.2 (user, 2026-11-19).
- Expected result per launcher at max range: Missile 12 flying → 1, ≈2,600 → ≈41 particles; Torpedo 4 → 1, ≈1,200 → ≈49. Pool cap Missile ≈16 → 2, boosted ≈112 → ≈9.

### Steps

1. [x] Record baseline: `BattleSimulator` TTK + Overkill for 4 matchups (missile ship vs capital, vs frigate, torpedo ship vs capital, bomber squadron vs capital). → verify: export saved.
2. [x] Edit the 5 family rules in `WeaponsData.asset` (values above). → verify: `BalanceDpsEstimator` DPS per missile weapon equals baseline ±0.1%.
3. [x] Unit overrides: Executor `Heavy Missile Launcher` (type 34, speed 97.5, reload ×2 → 20 s) and MC30c `Cluster Bomb Launcher` (type 81, speed 117). Keep both speeds (already ≥91). → verify: Executor missile DPS unchanged. No edit needed: overrides are multipliers on the family.
4. [x] Edit `Trail` in both prefabs (rate, lifetime, max particles) via Unity API; save + reserialize. → verify: read back values, console clean.
5. [x] Missile defense → Phase 1b.
6. [x] Rerun step 1 matchups. → verify: TTK within ±5%; Overkill not above baseline by more than 10%. **Gate missed because of speed ×2** (see Results).
7. [x] Review tests (Combat, Abilities, Balance, Audio). Replace pinned numbers with relationships where touched.
8. [ ] User in-game check: missile/torpedo look, trail length, launch SFX density, 256/288 impact size, Full Salvo / Torpedo Salvo feel.

### Results (2026-11-19, Editor-side simulator, not Play Mode)

- Method: eval scripts drive `BattleSimulation` directly, 100 seeds per matchup, distance = attacker max range. Unity eval main-thread limit is 5 s → run ≤10 seeds per call.
- `BalanceDpsEstimator`: every unit's DPS identical before/after (all 100+ units).
- Matchups: M0 Victory I ×3 vs MC80; M1 Recalcitrant vs Pelta; M2 Procurator vs Providence; M3 TIE Bomber ×6 vs MC80. All side-A wins.

| Matchup | TTK before | Salvo/reload only | Full change | Overkill/run before → after |
|---|---|---|---|---|
| M0 | 63.5 s | 62.6 s (−1.4%) | 50.5 s (−20%) | 40.0 → 44.7 (+12%) |
| M1 | 24.8 s | 24.6 s (−1.0%) | 21.3 s (−14%) | 7.6 → 7.9 (+4%) |
| M2 | 33.9 s | 33.8 s (−0.2%) | 31.1 s (−8%) | 32.0 → 19.8 (−38%) |
| M3 | 36.0 s | 36.0 s (0%) | 30.6 s (−15%) | 32.3 → 34.1 (+6%) |

- Salvo/reload change alone holds TTK within ±1.5% and lowers overkill.
- Whole TTK drop comes from speed ×2: simulator fights at max range, so every missile/torpedo lands ≈6 s earlier (fighter torpedoes ≈1 s earlier per run). Overkill per run stays ≤0.15% of damage dealt.
- Decision needed: accept faster first hits, or keep speed ×2 only for visuals and raise reload slightly. Not changed (speed ×2 was a user decision).
- Stale text: unit descriptions still say "4-burst" / "8-burst" / "5-burst" for missile launchers (`EmpireFaction.asset` lines ≈22, 133, 156). Not edited.

## Phase 1c — Remove squadron missile defense

- Decided 2026-11-19 (user): remove missile defense from TIE Avenger and from any other squadron. Ships keep theirs (Phase 1b).
- Search result (squadrons only): TIE Avenger is the only one. All 31 `Assets/Settings/Data/Squadron/*SquadronData.asset` carry the fields; only `TIEAvengerSquadronData.asset` has `SeekerWarheadRange` > 0 (50). No squadron prefab has a `MissileInterceptorHardPoint`. `SquadronInstaller` registers no other countermeasure.

1. [x] Delete `Assets/Scripts/Entities/Squadron/SeekerWarheadCountermeasure.cs` (+ `.meta`, through Unity).
2. [x] `SquadronInstaller.cs`: remove the `Data.SeekerWarheadRange > 0f` registration.
3. [x] `SquadronData.cs`: remove header + `SeekerWarheadRange`, `SeekerWarheadRecharge`, `SeekerWarheadMinimumTravel`.
4. [x] `BalanceUnitAdapter.cs:90`: remove the three field names from the squadron column list (squadrons now register no Hangar data fields).
5. [x] Orphans removed: `TryFindThreat` `damageType` + `minimumTravel`, `IncomingMissile.DistanceTravelled` / `_origin` / `DamageType`, `Launch` `damageType`.
6. [x] `EmpireFaction.asset` TIE Avenger description: sentence deleted.
7. [x] Reserialized 31 squadron data assets: exactly 93 lines removed (3 per asset), nothing else; compile clean.
8. [x] Tests reviewed: no seeker references; `HealthModelTests` registry calls still match (3-argument `Launch`).

- Edge: TIE Avenger loses a defensive trait with no replacement → provisional balance review (cost or stats) after in-game check.

## Phase 2 — Bolts (milder)

| Family | Now | Bolts / s per mount | Proposal | Bolt change |
|---|---|---|---|---|
| PointDefense (7) | 4 dmg × 8, 0.08 s, reload 2 s, speed 117 | 4.0 | 8 dmg × 4, interval 0.16 s | −50% |
| Laser (0) | 6 × 3, 0.1 s, reload 2 s, speed 172.9 | 1.5 | 9 × 2 | −33% |
| Turbolaser (1) | 16 × 3, 0.2 s, reload 4 s, speed 172.9 | 0.75 | 24 × 2 | −33% |
| MassDriver (3) | 35 × 3, 0.5 s, reload 5 s, speed 117 | 0.6 | 52.5 × 2 | −33% |
| Ion (2) | 30 × 2, 0.2 s, reload 6 s, speed 71.5 (7 s flight) | 0.33 | 60 × 1, speed 107 | −50%, flight 4.7 s |
| FighterLaser / Interceptor / Hero (20, 24, 25) | 5 × 2, 0.08 s, reload 0.9 s, speed 182 | 2.2 per fighter | 7.5 × 2, reload 1.35 s | −33% |
| FighterIon (21) | 1.3 × 2, reload 4.91 s | 0.4 | keep | 0 |
| Beam (8) | 640, reload 30 s | — | keep | 0 |

1. [ ] Measure first (needs a user Play Mode capture; agents do not run battles): active effects per family in one large battle (`AttackSequenceDiagnostics`, `Battle.Projectile.*` markers). Tune only families that dominate.
2. [ ] Apply accepted rows; hold `damage × shots / reload` constant.
3. [ ] Rerun `BattleSimulator` matchups; check PointDefense vs strikecraft overkill (8 dmg per bolt against fighter health).
4. [ ] Test review + user in-game check.

- Bolts already fly through `BoltFlightBatch` → cost per bolt is lower than per missile. Phase 2 is mostly a screen-noise change.
- `BoltShot.Play` overwrites prefab culling, space, color, size and lifetime per shot → Inspector edits to those are ineffective.

## Edge Cases

- Interception: interceptor range 40, reload 0.6 s, chance 0.5. Missile time inside range: 0.88 s now → 0.44 s at speed 91. Now ≈1 of 4 missiles lost (≈25% of a salvo); after, one attempt at 0.5 can remove the whole 256-damage salvo. Likely, not verified: chance 0.5 → 0.25 keeps the average loss. Variance rises either way.
- Overkill: single 256 / 288 hits waste more damage on weak hardpoints and small ships than 32 / 72 hits.
- First hit lands ≈6 s earlier at max range → short fights end slightly sooner; sustained DPS equal.
- `HitAccumulator` rolls per shot → fewer shots, longer hit/miss streaks per launcher.
- Abilities: Full Salvo / Torpedo Salvo scale reload and interval. With 1 shot per salvo the interval multiplier does nothing; check ability feel.
- Impact VFX receives damage and size (`ShotEffect.PrepareImpact`) → check a 256-damage impact is not oversized.
- `MissileShot.PostImpactSeconds` = trail lifetime + intercept explosion time → shorter trail returns shots to the pool sooner.
- `ShotEmitted` fires per shot → fewer launch SFX; check missile ships still sound active.
- `BattleSimulator` does not model interception → step 5 uses the estimate, then user check.
- Out of scope: `SuperWeaponData.asset`, `ShipAbilityCatalog.asset` profiles (own serialized salvo values), beams.

## Tests to review

- `WeaponTaxonomyTests.Executor_WeaponProfilesMatchBaseline` pins `HeavyArtilleryRocket` shots 4 / reload 10 → changes with Missile family.
- `WeaponCadenceTests` pins speed 172.9 and 3 shots → Phase 2 only.
- `ShotEffectPoolLifecycleTests`, `TorpedoSalvoModifierTests`, `WeaponLoadoutBakeTests`, `BalanceDpsEstimatorTests`, `ExecutorShipTests` → relationship-based; review, likely no edit.
- Tests are not run locally unless requested.

## Open Questions

- Decided 2026-11-19 (user): Missile salvo = 1 shot.
- Decided 2026-11-19 (user): fighter missiles/torpedoes = speed ×2 + shorter trail only.
- Decided 2026-11-19 (user): torpedo visual size stays 9; torpedo salvo = 1.
- Decided 2026-11-19 (user): missile defense stays; target = stops 50–75% of missiles; must be visibly working.
- Decided 2026-11-19 (user): ship defense rule = B, one roll per mount per missile → Phase 1b.
- Decided 2026-11-19 (user): remove squadron missile defense (TIE Avenger seeker) → Phase 1c. Supersedes the 60 s recharge.

## Phase 1b — Missile defense (target 50–75% stopped)

### Status from source (not run in game)

- Wired: all 8 mounts are in their prefab's hardpoint + injection lists; `IncomingMissileRegistry` registered in `SkirmishServiceRegistrations`; `CombatAttackCoordinator.CommitDueEvent` skips damage when `Missile.IsIntercepted`.
- Only shots that rolled a hit are tracked (`WeaponHardPoint.EmitScheduledShot`) → missed missiles are never shot down.
- Visuals exist: per-mount `LineRenderer` (width 0.3, shown 0.08 s) + `interceptExplosion` particle inside the missile prefab.
- Pooling: laser = one reused `LineRenderer` per mount, never instantiated at runtime; explosion = part of the pooled missile shot. No extra pool.
- Why it looks broken: range 40 vs missile range 350–550 → one attempt window of 0.88 s; flash lasts ≈5 frames.
- Estimate, example 6 launchers on one Munificent: today 24 missiles per wave, ≈3 stopped (≈12%). After Phase 1 with unchanged defense: 6 per wave, ≈1 stopped (≈17%). One launcher alone: 75%.

### Option A — data only (rejected 2026-11-19)

- Range 40 → 100, reload 0.6 → 0.3 s, chance 0.5.
- Per wave at speed 91: 4 attempts per mount → Munificent ≈4 stops, Acclamator ≈12.
- Result depends on volume: ≈100% vs 1–3 missiles, 50% at 8, 25% at 16. Cannot hold 50–75%.

### Option B — one roll per mount per missile (chosen 2026-11-19)

- Rule: each mount tries each missile once. Ship stop rate = `1 − (1 − chance)^mounts alive in range`, independent of volume.
- Values: range 80, reload 0.1 s; Munificent chance 0.3 → 51%; Acclamator chance 0.2 → 74%. Destroyed mounts lower the rate.
- Code: `IncomingMissile` remembers mounts that tried; `IncomingMissileRegistry.TryFindThreat` skips them; `MissileInterceptorHardPoint` passes itself.
- Tests: `HealthModelTests` interceptor case (sets chance 1) → add "second attempt on same missile is skipped".

### Steps

1. [x] User picks A or B → B.
2. [x] Code: one roll per mount per missile (`IncomingMissile.WasAttemptedBy` / `RecordAttempt`, `TryFindThreat(owner, position, range, interceptor, out threat)`, `MissileInterceptorHardPoint` passes `this`) + `HealthModelTests.PointDefense_RollsOncePerMissile`.
3. [x] Apply values on 8 mounts: range 80, reload 0.1 s; chance 0.3 on `MunificentShipView.prefab` × 2, 0.2 on `AcclamatorAssaultShipView.prefab` × 6. Read back via Unity API.
4. [x] Visibility: `LASER_FLASH_DURATION` 0.08 → 0.15 s, `LineRenderer.widthMultiplier` 0.3 → 0.5 on the 8 mounts (user confirms look).
5. [ ] Squadron missile defense removal → Phase 1c (replaces the 60 s recharge decision).
6. [ ] User in-game check: count launched vs shot down on Munificent and Acclamator.

## Missile Defense Inventory (2026-11-19)

- `MissileInterceptorHardPoint`: 8 mounts total → `AcclamatorAssaultShipView.prefab` × 6, `MunificentShipView.prefab` × 2. All: range 40, reload 0.6 s, `interceptChance` 0.5.
- `SeekerWarheadCountermeasure`: `TIEAvengerSquadronData.asset` only → range 50, recharge 10 s per fighter, minimum travel 10, always succeeds, concussion missiles only.
- Estimate, 1 missile at speed 91 (0.44 s inside range → 1 attempt per mount): Munificent stops 75% at chance 0.5, 44% at 0.25; Acclamator 98% at 0.5, 82% at 0.25. Today (4 missiles, 0.88 s): Munificent ≈25–50%, Acclamator near 100%.
- TIE Avenger has two unrelated things: (1) offensive weapons `Light Fighter Laser Cannon` (type 133) + `Light Fighter Missile Launcher` (type 134, family FighterMissile, ×10) on normal `WeaponHardPoint`s; (2) the seeker countermeasure = squadron-level class, no hardpoint, no prefab mount, not shared with ships. Enabled by `SquadronData.SeekerWarheadRange > 0` in `SquadronInstaller`.
- Seeker has no own visual: missile just plays its `interceptExplosion`. 10 fighters, separate recharge each → squadron stops up to 10 concussion missiles per recharge period. Torpedoes are ignored.
- Full removal touches: 8 prefab mounts + their `ShipData` hardpoint entries, `MissileInterceptorHardPoint`, `SeekerWarheadCountermeasure`, `IncomingMissileRegistry`, `interceptable` flags, `MissileShot.interceptExplosion`, `BalancePrefabBindings`, `HealthModelTests`, tooltips.

## Files

- `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`
- `Assets/Prefabs/Vfx/Projectiles/ConcussionMissileShot.prefab`, `ProtonTorpedoShot.prefab`
- `Assets/Scripts/Components/ViewComponents/Weapon/MissileShot.cs`, `ShotEffect.cs`
- `Assets/Scripts/Components/ViewComponents/Health/WeaponHardPoint.cs`, `MissileInterceptorHardPoint.cs`, `FighterTorpedoHardPoint.cs`
- `Assets/Scripts/Components/AttackComponent/WeaponProfile.cs`, `WeaponFamilyRule.cs`, `WeaponProfileFactory.cs`
- Related: [[Rules/BATTLE_PERFORMANCE_GUIDELINES#Weapon cadence and pooled overlap · 2026-11-14]], [[TODOs/Bugs/Weapon_Fire_Rate_Low_FPS]], [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan]]
