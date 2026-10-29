---
category: Optimization
status: done
created: 2026-10-28
audit_date: 2026-10-28
completed: 2026-10-29
disposition: merged
merged_into: "[[TODOs/Optimization/Battle_FPS_Recovery_Plan]]"
---
# Battle Rendering Performance Audit

> Merged 2026-10-29 into [[TODOs/Optimization/Battle_FPS_Recovery_Plan]]: open items transferred there; this note is history.

## Goal

- Reduce decorative asteroid rendering cost without changing gameplay or visible rock coverage.
- Preserve both 2026-10-28 audits, VFX findings, capture limitations and verification requirements.
- Evidence: [[Done/Optimization/Battle_Rendering_Performance_Audit - Research|Both audits and measurement tables]].

## Decision

- **Confirmed:** decorative asteroids dominate sustained frame cost in both measured views.
- **Audit A:** enabled → disabled → restored = **39.228 → 100.912 → 39.353 FPS**; about **15.59 ms/frame** removed.
- **Audit B, Performant:** **19.318 → 48.634 → 19.350 FPS**; about **31.19 ms/frame** removed.
- **Why:** on/restored timings closely agree; camera position, rotation and resolution stayed fixed within each comparison.
- **Avoid:** claiming Performant itself regressed performance. Different views/loads were measured; no matched quality-preset comparison was performed.
- Audit/documentation complete; optimization implementation remains **todo**. No production code or assets changed.

## Rules

- **User requirement — instanced materials:** decorative asteroids must use instancing-enabled materials and compatible shader passes; verify actual instanced draws, not only material flags.
- **Existing state:** `DecorAsteroidRenderer.Configure` creates materials with `enableInstancing = true`; `DrawBatch` uses `Graphics.RenderMeshInstanced`. Frame Debugger already confirmed instanced asteroid draws.
- **User requirement — camera frustum culling:** do not submit an asteroid whose transformed mesh bounds are wholly outside the rendering camera's frustum.
- Bounds intersecting a frustum plane must remain eligible; preserve correct visibility for large/scaled/rotated rocks at screen edges. A center-point-only visibility check is insufficient.
- Cull per asteroid before assembling draw batches, or use equivalently precise visible-instance selection. Distance-only filtering and broad batch bounds do not satisfy this requirement.
- Keep gameplay entities, collision, navigation, fog and map generation behavior unchanged. These requirements concern decorative rendering.
- Preserve the user's quality preset and restore all temporary runtime/debug state after measurement.

## Important Values

| Measured view | Enabled mean ms | Disabled mean ms | Restored mean ms | Enabled / disabled triangles |
|---|---:|---:|---:|---:|
| Audit A | 25.499 | 9.908 | 25.422 | 52,060,135 / 286,015 |
| Audit B — Performant | 51.749 | 20.562 | 51.725 | 59,019,361 / 105,083 |

- Both comparisons: 24 s total; 8 s per phase; discard 2 s settling, observe 6 s; actual camera resolution **2560×1440**.
- Existing distance limits: debris **2,200 units**, medium **5,000 units**, large uses camera far clip (**200,000 units** in inspected runtime).
- Mesh triangles: medium **11,080**, large **7,116**, debris **714 / 934**. Instancing reduces submission overhead; it does not remove geometry workload.
- Performant verified: `URP-Performant`, render scale **1**, **2× MSAA**, HDR off, SRP Batcher on, shadow distance **50**.
- Performant capture still rendered **128-sample nebula**, **17 Bloom events**, and motion blur; no SSAO or shadow draws in that captured frame.

## Implementation

- **Priority 1:** preserve instanced materials; add per-asteroid camera-frustum rejection.
- **Priority 1:** evaluate lower-poly meshes / LOD and a dedicated large-decor distance limit after culling measurements. Medium rocks currently cost more triangles each than large rocks.
- **Priority 2:** attribute remaining **20.562 ms/frame** without asteroids in Audit B; measure nebula coverage/sample cost and post-processing individually.
- **Priority 2:** review legacy explosion particle materials, actual URP pass behavior, shadow flags and offscreen simulation. Pool population alone is not a leak.
- **Tooling:** repair Frame Debugger capture/window exceptions and stale Rendering Debugger camera selector before relying on unattended captures.
- **2026-10-29:** PC comparison, regression root cause (`b19830c53`) and phased fixes → [[TODOs/Optimization/Battle_FPS_Recovery_Plan]].
- Coordinate related work with [[Done/Optimization/Battle_Particle_Rendering_Optimization_Plan]], [[Done/Optimization/Projectile_VFX_PC_Optimization_Plan]] and [[Done/Features/Space_Hazards_Rework_Plan]].

## Edge Cases

- Capture phases observe different frames. Camera moved between some snapshots/captures, but stayed fixed within each accepted on/off/restored comparison.
- Editor GPU counters can exceed observed frame intervals; do not treat them as independently validated GPU latency, derive FPS from them, or add overlapping CPU/GPU timings.
- Frame Debugger index counts were already expanded by instance count; divide by 3 for triangles, do not multiply by instances again.
- A shader reporting `isSupported=true` does not establish correct URP pass behavior. A shadow-enabled renderer does not prove it issued a shadow draw.
- Idle pooled VFX and high particle capacity do not prove runtime cost; confirm playing/visible particles and captured draws.
- Initial `Screen` size **2940×1514** was not reliable evidence of actual rendering resolution; captures/camera showed **2560×1440**.
- No automated tests were run. Preserve the repository test-execution restriction unless the user explicitly requests tests.

## TODO

- [x] Collect both runtime audits and verify asteroid restoration.
- [x] Capture Profiler, Render Graph, Rendering Debugger and Frame Debugger evidence; document rejected/partial captures.
- [x] Inventory static and live VFX; record measured findings separately from hypotheses.
- [ ] Implement and verify camera-frustum culling with instanced asteroid materials.
- [ ] Verify offscreen rejection, frustum-edge visibility, rotated/scaled bounds, camera movement and unchanged gameplay.
- [ ] Repeat fixed-camera on/off/restored and before/after captures with matched quality, workload and actual render resolution.
- [ ] Measure nebula/post-processing GPU cost and representative explosion/projectile load.
- [ ] Review mesh LOD/distance policy, legacy particle materials, shadow flags and culling behavior.
- [ ] Fix audit-tool exceptions and validate GPU-counter interpretation; confirm results in a Development Player.

## Files

- `Assets/Scripts/Components/Environment/DecorAsteroidRenderer.cs`
- `Assets/Scripts/Components/Environment/DecorAsteroidMesh.cs`
- `Assets/Scripts/Editor/RenderAuditOrchestrator.cs`, `RenderAuditProfilerTool.cs`, `RenderAuditFrameDebuggerTool.cs`, `RenderAuditRenderingDebuggerTool.cs`
- `Assets/Scripts/Editor/BattleInstancingAudit.cs`
- `Assets/Art/Shaders/Vfx/NebulaFilamentVolume.shader`
- `Assets/Settings/Render Pipelines/URP-Performant.asset`, `URP-Performant-Renderer.asset`
- [[Done/Optimization/Battle_Rendering_Performance_Audit - Research|Research]] → complete measurements, VFX inventory, issues and raw-artifact locations.
