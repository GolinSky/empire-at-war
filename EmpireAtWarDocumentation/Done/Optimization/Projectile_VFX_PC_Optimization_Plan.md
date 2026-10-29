---
category: Optimization
status: done
created: 2026-10-11
updated: 2026-10-11
completed: 2026-10-29
disposition: merged
merged_into: "[[TODOs/Optimization/Battle_FPS_Recovery_Plan]]"
---
# Projectile VFX PC Optimization Plan

> Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]: open items transferred there; this note is history.

## Goal
- Reduce projectile rendering cost and texture memory on PC without changing gameplay or runtime effect logic.
- Cover all 13 prefabs in `Assets/Prefabs/Vfx/Projectiles`, inherited effects, materials, textures, and runtime visual variations.
- Audit completed 2026-10-11; implementation and performance acceptance remain pending.

## Rules
- Preserve targeting, trajectories, speed, hit/miss outcomes, damage, fire cadence, barrel count, impact timing, pause and time scale.
- Preserve effect leases, pooling/prewarm/retirement, callbacks, interception, stop/drain behavior and owner-death behavior.
- Preserve runtime color, size, beam width, faction overrides, emissive appearance, multi-bolt spacing and transparent ordering.
- No C# gameplay/view lifecycle changes, shared-emitter rewrite, VFX Graph migration, culling/pause changes, particle lifetime changes or global pipeline changes.
- Asset/shader/importer changes only; one measured change at a time. Retain existing local changes, especially both beam smoke overrides at 50/s.
- PC baseline: current desktop quality/API; record actual OS/GPU/API. Treat Windows DX11/DX12 separately when supported; do not impose mobile quality reductions.

## Decision
- Chosen: equivalent material sharing → texture CPU-copy cleanup → targeted shader/material experiments → PC acceptance.
- Why: bolt counts already match 1/2/4/8; runtime variation uses particle/vertex color instead of per-shot material clones.
- Avoid: blanket instancing flags. Existing bolt renderers are stretched billboards; mesh-particle instancing is not enabled merely by their checkbox.
- Coordinate with [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan]] for rendering evidence and [[TODOs/Optimization/Desktop_Asset_Compression_Plan]] for importer ownership.
- Pool/attack optimization stays in [[Done/Optimization/Battle_Attack_Optimization_Plan]]; this plan only records those dependencies.

## TODO

### 1. Freeze comparison inputs
- [x] Audit all 13 prefabs, direct materials, nested explosion dependencies, runtime writers and serialized consumers.
- [ ] Export effective Unity-resolved nested settings and shader/import state; distinguish active renderers from inherited dormant components.
- [ ] Record reference images and material-instance identities for the variation matrix below; capture source revision and current local diff.
- [ ] Use existing complete battle captures to choose candidates. Obtain targeted before/after PC captures for acceptance; no broad idle-scene investigation.
- Verify: same fleet, camera, resolution, quality, time scale, active shots, cold/warm pool state and replay duration.

### 2. Share only equivalent materials
- [ ] Compare `IonShotHead.mat` ↔ `ImpactFlash.mat`, and `IonShotTrail.mat` ↔ `ProtonTorpedoTrail.mat`: same shader/base texture/active keyword families; serialized emission colors differ while `_EMISSION` is absent.
- [ ] Check all consumers and effective pass/queue/blend/UV/fade/color state. Share a material only after rendered equivalence is demonstrated; keep GUIDs and unused assets until separately reviewed.
- [ ] Keep `Projectile.mat` and `DualProjectile.mat` separate initially: clamp versus repeat textures and different multi-bolt shape behavior must survive.
- Verify: identical faction/ion colors and alpha, no cross-shot tint leakage, unchanged ordering; measure material switches and actual draws. Sharing does not guarantee batching.

### 3. Remove unnecessary texture CPU copies
- [ ] Check all runtime/editor consumers of `BlasterBolt.png` and `BlasterBolt_Tiled.png`; disable Read/Write only when no CPU pixel reader needs them.
- [ ] Both sources are 1000×50, readable, no mipmaps, bilinear; first clamps and second repeats. Preserve wrap, alpha and current visual footprint.
- [ ] Inspect effective Standalone GPU format before choosing compression; pilot moderate PC alpha-preserving compression only if memory benefit and thin-bolt quality justify it.
- Verify: imported/runtime memory before/after, no texture-read errors, near/far/oblique and bloom comparisons. This is a memory candidate, not a promised FPS gain.

### 4. Test material/shader cost without behavior changes
- [ ] Profile bolt/muzzle/beam draws separately; test soft-particle and camera fading independently on scoped material copies.
- [ ] Keep fades whenever removing them produces intersection seams or camera popping. Do not disable the camera depth texture globally.
- [ ] Investigate `TractorBeam.mat`: `_FADING_ON` remains listed although camera fading is disabled; verify generated effective behavior before normalizing keywords.
- [ ] If muzzle fragment cost is measurable, test algebraically equivalent simplification of `IonGlow.shader`; retain HDR color/core/glow, vertex alpha, additive blend and all users' appearance.
- [ ] Evaluate additive bolt per-particle sorting only after overlap parity checks; keep particle counts, shape, lifetime and culling unchanged.
- Verify: shader compilation, material/pass identity, GPU timing and render-thread submission cost. No shader rewrite or sort change is required if it produces no measurable benefit.

### 5. Acceptance
- [ ] Run the variation matrix: single/dual/quad/octuple; Triple mount's current fallback; four faction colors; explicit cyan override; small fighter bolts and all visual overrides.
- [ ] Include ion/concussion/proton trails, laser/composite/tractor beams, hypervelocity slug, IonPulseShot and IonPulseWave; superweapon sizes/speeds must not be capped.
- [ ] Exercise simultaneous mixed colors, repeated reuse, shield/armor hits, misses, target/owner death, interception, pause/resume, time-scale changes, offscreen exit/reentry and scene exit.
- [ ] Compare ≥3 matched firing runs per candidate: CPU/render-thread/GPU median and p95/p99, draws/SetPass, particle Update/Geometry, material/texture memory and GC. Accept only gains exceeding run variation with no visual/lifecycle regression.
- [ ] Validate targeted serialized references and relevant existing effect tests; follow AGENTS.md clean-scene + async test rules. No new blanket test suite required for importer-only changes.
- [ ] Import/save changes through Unity, inspect console/shader errors, read back persisted assets. Record each retained/reverted experiment and exact target hardware.

## Edge Cases
- `BoltShot.Play` overwrites prefab culling, space, color, size and lifetime every shot → Inspector-only changes to those values are ineffective or misleading.
- Missile trail/interception `IsAlive(true)` affects pool return → lifetime/emission edits can alter lifecycle; excluded here.
- HypervelocitySlugShot inherits Projectile and adds a muzzle object; inspect effective hierarchy before removal of anything apparently redundant.
- ProtonBeamView has no direct GUID consumer in the scanned `.asset/.prefab/.unity` files; keep it in compatibility checks, do not claim it is unused.
- No new performance capture, Play Mode, Unity tests or asset mutation occurred during this audit.

## Files
- [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan - Research|Asset inventory, runtime contract and evidence]]
- `output/ProjectileVfxAudit_20261011/assets.json`
- `output/ProjectileVfxAudit_20261011/prefab-references.json`
