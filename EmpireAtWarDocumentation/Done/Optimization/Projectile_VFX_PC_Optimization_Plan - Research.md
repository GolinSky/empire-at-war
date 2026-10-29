---
category: Optimization
status: todo
created: 2026-10-11
updated: 2026-10-11
---
# Projectile VFX PC Optimization Research

## Rules
- Snapshot: 2026-10-11; Unity `6000.4.7f1`, URP `17.4.0`.
- Evidence: live serialized files + Serena symbols + Graphify navigation; graph findings verified against source.
- Read-only audit; generated JSON under `output/ProjectileVfxAudit_20261011`. No fresh Unity-resolved hierarchy, live render capture, visual comparison or performance test.
- JSON records direct serialized components and nested instance overrides; it is not a flattened Unity hierarchy.
- Existing beam edits and other workspace changes were present before this audit.

## Implementation

### Prefab inventory
| Prefab | Current rendering/settings | Dependency |
| --- | --- | --- |
| Projectile | 1 stretched-billboard particle, burst/cap 1; Projectile material; muzzle line | BoltShot |
| DualProjectile | 1 system, burst/cap 2; shape enabled; DualProjectile material; muzzle line | BoltShot |
| QuadProjectile | 1 system, burst/cap 4; shape enabled; DualProjectile material; muzzle line | BoltShot |
| OctupleProjectile | 1 system, burst/cap 8; shape enabled; DualProjectile material; muzzle line | BoltShot |
| HypervelocitySlugShot | Inherits Projectile; length scale override 8.333333; added/rebound muzzle object | SuperWeaponData |
| ConcussionMissileShot | Head cap 1, lifetime 1000 s, rate 100/s; trail cap 300, lifetime 0.8 s, rate 6/unit; arcHeight 0.06 | MissileShot + FighterExplosionVfx |
| ProtonTorpedoShot | Head cap 1, lifetime 1000 s, rate 100/s; trail cap 300, lifetime 0.18 s, rate 24/unit; arcHeight 0.12 | MissileShot + FighterExplosionVfx |
| IonShot | Head/trail counts and timing as proton; separate ion materials; arcHeight 0.12 | MissileShot + FighterExplosionVfx; ability catalog |
| LaserProjectile | Beam line + muzzle; growth 0.3 s, hold 0.5 s; nested ExplosionVfx | BeamShot; weapon rules |
| ProtonBeamView | Beam line + muzzle; growth 0.3 s, hold 0.5 s; nested ExplosionVfx | BeamShot; no direct serialized consumer found |
| TractorBeamView | Beam line + muzzle; growth 0.3 s, hold 0.5 s; tinted nested ExplosionVfx | BeamShot; ability catalog |
| IonPulseShot | Pulse line + muzzle + manually emitted flash; flight 0.85 s, length 66, flash size 96/duration 0.3 s; flash cap 4 | SuperWeaponData |
| IonPulseWave | One line renderer; runtime generates 64 ring points | IonPulseView; ability catalog |

- Direct bolt/missile/ion renderers have cast/receive shadows and light/reflection probes off. Nested explosion renderers differ; do not generalize root flags to children.
- Direct particle render modes are Billboard or Stretched Billboard; none is Mesh. Their GPU-instancing checkbox is on; all 13 directly referenced materials have material instancing disabled.
- All four ordinary bolt renderers use distance sort mode 1. Multi-bolt systems are one renderer with multiple particles, not 2/4/8 renderers.
- Single bolt duration 1 s; dual/quad/octuple duration 5 s. All are nonlooping, Play On Awake off, one time-zero burst, no continuous emission.
- Bolt sizeScale = 3; no enabled collision, trigger, noise, light or trail module in direct bolt systems.
- Nested FighterExplosionVfx contains default flash, fireball and smoke systems; each authored cap 1000. Capacity is not emitted/live count.
- ExplosionVfx additionally nests ParticleProFX `ppfxPipelineFire`. Source rates are fire 5/s and smoke 100/s; effective overrides must be resolved through both nesting levels.
- LaserProjectile and ProtonBeamView currently override the nested smoke rate to 50/s. Existing rendering plan records seeded 362 → 185 particles after 6 s and drain to zero after 6.1 s; those are prior-task results, not rerun here.
- Nested explosion materials include third-party `matppfx_default`, `matppfx_explosion09`, `matppfx_smoke07`, `matppfx_fire01`, `matppfx_smoke02`, plus overrides. Preserve flipbooks, vertex streams and lifecycle; material changes require transitive-consumer review.

### Runtime variation contract
| Writer | Values changed | Preserve |
| --- | --- | --- |
| WeaponProfileFactory.Create | Selects family/mount prefab; OverrideVisual replaces prefab/speed/size; OverrideColor replaces color | Dedicated quad/octuple; multi-barrel fallback also covers Triple; no mount/count rebalance |
| WeaponsData.GetProfile(type,faction) | Caches faction-colored profiles through WithColor | Four faction colors; explicit colors bypass faction tint |
| BoltShot.Play | Clear; loop false; Local space; AlwaysSimulate; profile color; 3D size × sizeScale; speed 0.001; lifetime travelTime + 1 s; disable size-over-life; enable emission | Prefab culling/lifetime/size edits do not replace this runtime contract |
| BoltShot.Fly/TrackTarget | Transform position/rotation, target lead, impact position; clear on arrival | No trajectory/timing or update scheduling changes |
| MissileShot.Play/Fly | Head color/size = profile; trail color and size × 0.6; transform/arc; reports actual position for interception | Point defense follows visual; head clears but trail drains |
| BeamShot.ApplyProfile/UpdateBeam | Line color, width = size.x; moving endpoints; hit-effect position/start/stop | Growth/hold and DamageDuration; armor-only hit effect; ability hold override |
| MuzzleFlashView.Play/Tick | Color lerped 35% toward white; width 3×size.x; length 6×size.x; follows muzzle; 0.1 s fade | Peak first 40%; width fades by up to 25%; same vertex-color response |
| IonPulseShot | Line width/color/endpoints; one white flash emitted with prefab size/lifetime fields | 0.85 s flight and 0.3 s completion phase; no lifetime substitution |
| IonPulseView.Show | Transform and 64 ring positions from radius/direction | Geometry/radius and current ability lifecycle |

- Checked view scripts do not call `Renderer.material`, `sharedMaterial` setters, material keyword setters or MaterialPropertyBlock; colors travel through particle/line data.
- Do not introduce per-color materials or MPBs as a supposed cure for current cloning; no cloning is established by these writers.
- WeaponsData snapshot: 13 families, 68 definitions, 3 calibers, 3 range classes, 4 faction colors; mount values 1/2/3/4/8.
- 15 definitions enable OverrideVisual; definition 9 uses explicit cyan `(0,0.78,1,1)`. Snapshot can change with ongoing weapon work; regenerate matrix before implementation.
- Family visual sizes include `(0.45,0.45,0.6)` fighter bolts, `(0.6,0.6,1)` mass-driver bolts and `(1.2,1.2,1.2)` missiles. Family speed values span 25–140 units/s for moving projectiles.
- Ability IonShot: speed 35, white, size 1.2. Tractor: green `(0.2,0.9,0.45)`, size `(0.8,0.4,1)`.
- Superweapon profiles: ion pulse size `(36,3,3)`; slug speed 650 and size 3.6; proton torpedo speed 220 and size 18. Do not optimize only for ordinary size-1 bolts.
- Faction colors: `(1,0.31,0)`, `(1,0.15,0.05)`, `(1,0.04,0)`, `(0.2,1,0.25)`; alpha 1.

### Lifetime and pooling dependencies
- WeaponHardPoint initializes a ShotEffectPool per hardpoint; current maxIdle = profile.ShotsPerSalvo and prewarm is already invoked.
- ShotEffectPool retains a profile, activates/reuses objects, and retires idle/ownerless effects under existing rules.
- ShotEffect.Fire plays muzzle, calls subclass Play, and starts a travel-time lease. Completion also waits for subclass visual completion and muzzle completion.
- MissileShot completion waits for `trail.IsAlive(true)` and `interceptExplosion.IsAlive(true)`; shorter tails or paused offscreen simulation can change pool return.
- BeamShot reports DamageDuration = holdDuration; ordinary Play returns growthDuration. Its completion checks beam enabled state rather than all hit particles.
- IonShot ability and beam abilities also instantiate effects outside the ordinary pool; include both paths in acceptance.
- Pool sizing, ability pooling, readiness changes and shared emitters are excluded. Existing attack/CPU plans own those topics.

### Materials and textures
| Material group | Active features | Candidate |
| --- | --- | --- |
| Projectile / DualProjectile | URP Particles/Unlit, additive, transparent, soft particles + camera fade, ZWrite off | Isolate fade cost; preserve shared vertex tint |
| LaserProjectile / ProtonBeam | URP Particles/Unlit, alpha blend, color-add/sub/diff keyword, soft particles + camera fade | Keep separate from additive bolts; preserve occlusion/order |
| TractorBeam | URP Particles/Unlit, additive + soft particles; camera property 0 but _FADING_ON present | Verify effective keyword inconsistency |
| IonShotHead / ImpactFlash | Same star01 base texture, additive, no fading/emission keyword | Possible equivalent shared material after full state comparison |
| IonShotTrail / ProtonTorpedoTrail | Same default.png base texture, additive, no fading/emission keyword | Possible equivalent shared material after full state comparison |
| MuzzleFlash / IonPulseGlow / IonImpactFlash | Custom Ion Glow; additive, textureless, vertex color; brightness 6/6/12 | Keep distinct HDR parameters; profile shader before editing |
| Malevolence_IonPulse | URP Particles/Unlit, additive, no fade, no base texture | Low-priority compatibility coverage |

- Package shader GUID `0406db5a14f94604a8c57ccfbc9f3b46` resolves to installed `Shaders/Particles/ParticlesUnlit.shader`; no legacy-to-URP migration required for these direct materials.
- IonGlow already has UnityPerMaterial; adding that buffer is not an optimization. It has no instancing pragmas and performs radial glow/core math per fragment.
- Candidate head/trail pairs differ in saved emission color (white versus black), but neither enables `_EMISSION`. Treat equivalence as a testable candidate, not proven from names.
- BlasterBolt and BlasterBolt_Tiled: 1000×50 PNG, Read/Write enabled, mipmaps disabled, bilinear, sRGB. Clamp versus Repeat is intentional serialized variation.
- Their Standalone override is disabled; default textureFormat = 4. Confirm actual imported GPU format/memory before assigning compression.
- ProtonBeam texture: readable off, mipmaps off, clamp. Third-party star01/default: 128×128, readable off, mipmaps enabled.
- Material emission-map references without `_EMISSION` are not proof of another texture sample. Inspect compiled active variants rather than deleting saved properties for presumed FPS gains.

## Decision
- Existing research reports a complete historical frame with BlasterBolt 183 draws, BlasterBolt_Tiled 111 and smokeSheet03 247; Ion Glow muzzle-like signatures account for 215 draws.
- These are inherited capture findings, not newly replayed evidence, and texture groups may include other consumers. Do not sum them as exclusive projectile cost.
- Existing rendering plan verified shared live muzzle material and already reduced two beam smoke overrides; repeat neither change blindly.
- No numerical speedup is claimed. Prioritize exact material equivalence and CPU-copy cleanup; retain rendering experiments only with measured PC benefit.
- Excluded visual reductions: fewer barrel particles, reduced superweapon size, shorter flashes/trails, skipped effects and offscreen simulation changes.

## Files
- Plan: [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan]].
- Runtime: `Assets/Scripts/Components/ViewComponents/Weapon/{BoltShot,MissileShot,BeamShot,IonPulseShot,IonPulseView,MuzzleFlashView,ShotEffect,ShotEffectPool}.cs`.
- Profiles: `Assets/Scripts/Components/AttackComponent/{WeaponProfile,WeaponProfileFactory,WeaponsData,WeaponFamilyRule}.cs`.
- Consumers: `Assets/Settings/Data/Models/Weapon/WeaponsData.asset`, `ShipAbilities/ShipAbilityCatalog.asset`, `SuperWeapons/SuperWeaponData.asset`; AssetMappingData and existing Addressables records for some legacy entries.
- Direct GUID search counts are recorded in `output/ProjectileVfxAudit_20261011/prefab-references.json`; not runtime frequency or an exhaustive dynamic-load proof.
- [Unity particle renderer module](https://docs.unity3d.com/Manual/PartSysRendererModule.html) and [particle instancing requirements](https://docs.unity3d.com/Manual/PartSysInstancing.html): consulted through Context7; mesh-instancing guidance does not imply stretched-billboard eligibility.
- Installed URP 17.4 source: `Library/PackageCache/com.unity.render-pipelines.universal@7228970dbdbf/Shaders/Particles/ParticlesUnlit.shader`; confirms fade/emission/instancing shader branches.
