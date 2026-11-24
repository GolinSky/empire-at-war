---
category: Optimization
status: in-progress
created: 2026-11-23
system:
  - vfx
  - battle-rendering
---
# Cinematic Explosion VFX Optimization

## Goal

- Cut memory and GPU cost of the Mirza Beig "Cinematic Explosions FREE" copies used for hardpoint destruction, hull scorch blasts, unit death and orbital hits.

## As built (2026-11-23)

| Prefab (`Assets/Prefabs/Vfx/Explosions/`) | Source | Owner | Size |
|---|---|---|---|
| `HardPointExplosionVfx` | Explosion 2 + nested `HardPointFireSmoke` | `HardPointExplosionPool` (prewarm 16) | start size 10.5 × hardpoint `destroyedVfxScale` |
| `HardPointFireSmoke` | Smoke 1, looping | nested above | start size 7.5, child scale 1 |
| `HullScorchBlastVfx` | Explosion FREE 1 | `HullScorchBlastPool` (prewarm 8), `UnitHullScorchBlasts` | start size 7.5 |
| `UnitDeathExplosion` | Explosion 1 | `UnitExplosionService` destruction pool | hull sphere × 1.2 × `sizeMultiplier` 2 |
| `OrbitalHitExplosion` | Explosion 3 | `UnitExplosionService` orbital pool (`UnitExplosionKind.OrbitalHit`); one per salvo, on the last impact (`salvo.IsComplete` in `SuperWeaponFireService.ApplyHit`) | hull sphere × 1.2 × `sizeMultiplier` 1.5 |

| `MuzzleBlastVfx` (2026-11-24) | Explosion FREE 3 | `MuzzleBlastPool` (prewarm 32), `UnitMuzzleBlasts` (ships, stations, platforms; not squadrons) | diameter = `WeaponProfile.Size.x × 6`; **main batteries only**, one per salvo (`WeaponHardPoint.MainBatterySalvoStarted` → `IWeaponFireEvents`); offset 0.25 × diameter along mount `up` + 0.15 × diameter towards the aim point, kept in mount space while it follows; skipped when fog-hidden; streak `MuzzleFlashView` kept. Color = faction-resolved shot color lerped 0.35 to white (same as the streak) → particle `startColor`; `ExplosionFree3_Flipbook.png` made greyscale (max channel), material emission off, `_BaseColor` 2 (old look was black base + emission 2, which particle color cannot tint) |

- Hardpoint VFX scale per ship: Raider 1, ISD II 2.2, Executor 8.
- Smoke loop: lifetime 4–6 s, 2/s, flipbook frames 24→127 of 128 (first 3 rows = start burst, skipped), random 0–360° rotation, alpha fade-in 0→1 over first 25 %, out after 85 %, World space, 0.4 s start delay.
- Removed from copies: Cinemachine impulse, disabled force fields + Animator, all lights (vendor particle lights, death flash light; `UnitExplosionView.flashLight` removed from code). `CinematicExplosionLight.prefab` deleted.

## Applied

| # | Item | Before → after |
|---|---|---|
| 1 | Texture memory | 8192²/4096² (≈ 680 MB in Editor incl. Explosion 1/3) → max 2048, compressed (smoke albedo RGBA32 → DXT5); 8 textures ≈ 49 MB Editor figure. Normal maps deleted (unused after unlit). |
| 2 | Lights | all removed (user request). |
| 3 | Lit → Unlit | `CinematicExplosion1/2/3` → `URP/Particles/Unlit`; base + emission maps kept. Smoke reverted 2026-11-23: unlit smoke read white and flat (user) → `CinematicSmoke1` back to `URP/Particles/Lit` with vendor settings + `Smoke1_Normal_8x16.png` (2048, DXT5). Rule: smoke needs lighting + normal map for depth; emissive fire does not. |
| 5 | Pool sizes | prewarm counts per pool (`SkirmishScope` consts); Editor/Dev log at battle close: `[VfxPools] <prefab> prewarm= peak= grown=` and `[VfxPools] UnitDeathExplosion peak=…; OrbitalHitExplosion peak=…`. Final sizes need one user battle log. |

## Open

| # | Item | Note |
|---|---|---|
| 4 | Overdraw | big transparent billboards stacked on one ship (many burning hardpoints + smoke) → each covered pixel shaded once per layer. Sizes were just increased (user look choice); measure before cutting. |
| — | Distortion | `_DISTORTION_ON` on `CinematicExplosion1/2/3`, `CinematicExplosionFree1`; `m_RequireOpaqueTexture: 0` on all 6 URP assets. URP `Particles.hlsl` `Distortion()`: offset = `normalTS.xy × strength`; no `_NORMALMAP` → `normalTS = (0,0,1)` → zero offset; result = `lerp(sceneColor, base, saturate(alpha − _DistortionBlend))` (`_DistortionBlend` 0). Opaque texture on → no refraction, full-screen copy per frame, and transparent-queue nebula impostor/hazards missing from the copy. Opaque texture off → mixes in the default texture at soft edges. **Done 2026-11-23 (user approved):** `_DistortionEnabled` 0 + `_DISTORTION_ON` off on all four; opaque texture stays off. |
| — | `CinematicSmoke1._NormalMap` | leftover package texture GUID (`730ed5d4…`) in an unused slot; check it is not pulled into builds. |

## TODO

1. User: in-game look (unlit, sizes, smoke loop) + read `[VfxPools]` lines after one heavy battle → set prewarm counts.
3. Overdraw / GPU: `RenderAuditAblationCapture`, heavily damaged fleet, High Fidelity + Performant.
