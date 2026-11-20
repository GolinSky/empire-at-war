---
category: Features
status: in-progress
created: 2026-11-20
system:
  - battle-rendering
  - health
---
# Ship Hull Damage Shader Plan

## Goal

- Ships, stations and defence platforms show soot scorches with burnt, faintly glowing patches as hull drops.
- Keep one `EmpireAtWar/Ship Lit` variant, SRP Batcher and GPU Resident Drawer grouping.
- No mesh swaps, no per-ship textures, no per-frame CPU work.

## Status (2026-11-20)

- Phase 1 implemented and tuned with the user over many preview rounds; last commit `0c44f1fb7`.
- Pending: in-game look (user), GPU cost measurement (user Play Mode), optimizations R1–R5 (see Performance), Phase 2 decision.
- Tests authored/updated, not run (local override).

## Decisions

- Chosen: extend `Ship Lit` `CreateSurfaceData` → `ApplyHullDamage`; per-unit state in `MeshRenderer.SetShaderUserValue`; shared look as globals; per-ship tweaks as `UnityPerMaterial` properties.
  - Why: team colour/outline already use the user value; 1,461 `Ship Lit` materials need no edit; wrecks reuse `CreateSurfaceData` → marks carry over at death.
  - GPU Resident Drawer: Unity 6000.6 docs — the user value changes shading "without using different materials or additional CPU time" and works with GRD.
- Procedural, texture-free marks (3D cell scatter + smooth value noise).
  - Why: the generated detail texture showed as blocky tiles up close; procedural noise is continuous and needs no UVs.
- Avoid: URP Decals (DBuffer, depth-normal prepass, one draw per decal, bleeds onto neighbours); per-ship mask RTs (overlapping UVs, breaks batching); `MaterialPropertyBlock`/keywords/`multi_compile` (batching, variant stalls — [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]); parallax/vertex displacement.
- Dropped by user: darkening the additive window-light overlays (alignment needs unit-root space; facts in History).

## User decisions

- Scope: ships, stations, defence platforms. Mining facilities excluded. Squadrons stay at level 0.
- Marks appear, never grow: each damage step switches on new marks elsewhere. Repair removes the newest first.
- 31 damage steps (was 15).
- Look: soft soot, solid black centre → mark's own random opacity (0.7–1.0, mostly 0.7) in the middle → ~15 % near the outline → fade. Outline between circle and star. Burnt paint = irregular burnt-metal patch at a random spot inside the mark, sunken, with a glowing rim; never repeats the outline. No glow on plain plating ("lava").
- Mark size follows hull size (auto-scale tool, Victory = reference). Per-ship override via material.
- Phases: Phase 1 only. Phase 2/3 need user approval after the in-game check.

## Design as built

### Data path

- Health `OnValueChanged` → `UnitHullDamageSync` (pure C#, `EntityFeatureBindings.RegisterHullDamage`; ship/station/platform installers) → `IUnitRenderStateComponent.SetHullDamage(hull%)` → level `ceil((1 − hull) × 31)` → `SetShaderUserValue` on all listed renderers.
- Seed per unit: `GetEntityId().GetHashCode()` hash → 5 bits; no shared random state.
- Wreck: `IUnitWreckService.Spawn(data, unit, uint renderUserValue, delay)`; callers pass `WreckUserValue` (team + damage + seed, no highlight). Wreck pattern uses the undeformed position.
- Globals: `HullDamageSettings` asset → `HullDamageService.Upload` once per battle (`SkirmishScope`); also used by the editor preview.

User-value layout:

| Bits | Use |
|---|---|
| 0–7 | team |
| 8–10 | highlight (outline code masks to 3 bits) |
| 11–15 | damage level 0–31 |
| 16–20 | pattern seed 0–31 |
| 21–30 | free (Phase 2 hit slot) |

### Shader (`ShipHullDamage.hlsl`)

1. `level == 0` → return (per-draw uniform branch).
2. Position: object space × object scale (+ seed offset), per vertex → interpolated `positionDamage`; ÷ (spacing × material Mark Scale) → cell space.
3. Search 3×3×3 cells (PCG4D hash per cell): spot xyz, appear-share `w` (on when `w < level × density`), size, opacity. Spot must lie within one radius of the surface; distance measured in the surface plane. Shape distance: 6–10 soft lobes (`starStrength`). Keeps the 2 nearest marks + the nearest mark's burnt-patch offset.
4. Pixels beyond `1 + edgeBreakup` outline radii → return (per-pixel branch).
5. Soot: 2-octave edge noise frays the outline; 2 nearest marks blended `1 − (1 − a)(1 − b)` (no seam). Albedo → soot colour; metallic × (1 − soot); smoothness → 0.1; occlusion × (1 − 0.5 soot); emission × (1 − soot).
6. Patch field = 2-octave patch noise + bias from distance to the patch centre (random offset ≤ `burntOffset`); field below rim → return (per-pixel branch).
7. Patch: burnt colour (darker deeper in), inner-wall occlusion, normal tilted inward on the wall (finite differences, no `ddx` inside per-pixel branches), rim emission × `emberStrength`, 5 % fill glow.

### Settings (current asset values)

| Setting | Value | Meaning |
|---|---|---|
| `markRadiusMin` / `markRadiusMax` | 3.5 / 6.1 | world units at Mark Scale 1; spacing = max × √((1 + breakup)² + 1) ≈ 9.5 |
| `markDensity` | 1 | share of spots on at 0 hull |
| `edgeBreakup` / `edgeNoiseFrequency` | 0.2 / 3 | outline wobble |
| `starStrength` / `starMinSize` | 0.45 / 0 | 0 = round, 1 = star |
| `sootColor` | 0.015 | near-black |
| `sootOpacityMin/Max/Edge` | 0.7 / 1 / 0.15 | middle band (random, t²) / outline |
| `emberLength` / `burntOffset` / `burntDepth` | 0.45 / 0.35 / 0.6 | patch radius, centre offset, wall tilt (mark radii) |
| `emberWidth` / `emberStrength` | 0.03 / 1 | rim width (noise units), glow |
| `burntColor` / `emberColor` | (0.24, 0.14, 0.12) / (1, 0.4, 0.1) | |

### Per-ship (material)

- `_HullDamageMarkScale` (0.05–32): scales marks and spacing together. Written by `Tools/Empire At War/Rendering/Materials/Auto Scale Hull Damage Marks` (`HullDamageMarkScaleTool`, one-time; rerun after adding units, overwrites hand tweaks): `1.3 × hullRadius / 58.3`. Ran once: 876 materials + 774 wreck copies (e.g. Arquitens 0.45, Victory 1.3, Tector 2.28, Executor 23.5). Shared `SciFiLamps_*` → geometric mean.
- `_HullDamageOpacityScale` (0–2): soot opacity multiplier. Victory 1.2.
- Keep the wreck copy equal (`WreckMaterialSyncTests`). The tool writes only Mark Scale into existing `<name>_Wreck` copies; never use `Sync Wreck Materials` for this (it resets 15 drifted wrecks and creates 49 unreferenced ISDI/ISDII materials).

### Tools

- `Tools/Empire At War/Hull Damage Preview` (`HullDamagePreviewWindow`): hull slider on the selected unit, uploads settings.
- Auto-scale tool above.
- Dead: `HullDamageTextureBuilder` + `Assets/Art/Textures/Vfx/HullDamageDetail.png` (unreferenced since the procedural rework) → delete (R6).

## Performance investigation (2026-11-20)

### Method

- Static D3D11 bytecode count (SHEX opcodes, declarations excluded) of `ForwardLit`, with damage vs. `ApplyHullDamage` returning at entry. Not GPU time; Metal/Vulkan/DXIL differ.

### Results

| Fragment variant | Without damage | With damage | Search loop body |
|---|---|---|---|
| Ship Lit, no keywords | 153 | 1,264 | 119 (× 27 cells) |
| Ship Lit, shadows + additional lights + cluster + DOTS | 380 | 1,514 | 119 (× 27) |
| Ship Wreck, no keywords | 427 | 1,537 | 119 (× 27) |

- Vertex: unchanged count (51 / 84); +1 `float3` + 1 `half` interpolator.
- Executed estimate per pixel of a damaged unit:
  - Search: 27 hashes always; ~1,300 instr typical (most cells fail the surface test before the shape step), ~3,200 worst (every cell runs `atan2` shape).
  - Near a mark: + 2 edge-noise octaves (~200).
  - Near a patch: + 2 patch octaves + 4 more for the wall slope + transforms (~600).
  - → damaged-unit pixels cost roughly 4–9× the undamaged lit fragment. Undamaged units: one uniform branch (~0).
- Biggest cost: the 27-cell search on every pixel of every damaged unit, including far-away ships where marks are a few pixels wide.

### Already optimal

- CPU: event-driven; ≤ 31 `SetShaderUserValue` per unit life; one global upload per battle; no per-frame work.
- Batching: no MPB, keywords or new variants; per-ship values in `UnityPerMaterial`. All 96 checked variants compile.
- Memory: no textures; 7 global float4s.
- Passes: forward only; shadow, depth, depth-normals and outline passes untouched.

### Recommended optimizations (not implemented)

| # | Change | Expected effect | Look change |
|---|---|---|---|
| R1 | Before hashing a cell, skip it when its centre is farther than `0.87 + maxRadius` cells from the pixel's surface plane | flat hull: 27 → ~9–15 hashed cells; ~40–60 % of search | none |
| R2 | Hoist the surface frame out of the loop; skip the shape (`atan2`/`sin`) when plain distance ≥ current second-nearest | removes most worst-case shape work | none |
| R3 | Distance LOD: `ddx/ddy(positionWS)` before the branches; when a pixel covers > ~¼ mark radius, skip patch + edge noise (soot only) | large win at normal RTS zoom | far: no patch rim/fray (sub-pixel anyway) |
| R4 | Cheap 1-int hash for `w` first; full PCG4D only for active cells | helps at partial damage (level < 1) | none |
| R5 | Quality tier (global float, not a keyword): Performant/Low → no wall tilt, 1 edge octave | lower floor on weak GPUs | slightly flatter patches |
| R6 | Delete the unused texture builder + PNG | repo/import time | none |

- Do R1 + R2 first (no visual change), then measure; R3 next if battle zoom is still costly.
- Measurement needed: `RenderAuditAblationCapture` damage on/off/restored with a heavily damaged fleet on High Fidelity and Performant (user Play Mode).

## Edge Cases

- Repair → level drops → newest marks disappear first.
- Shields up → hull unchanged → no marks; shield-piercing damage → marks under the shield (accepted).
- Multi-renderer units: 56/90 units have several hull transforms → each part has its own mark pattern; marks do not continue across part seams.
- Turrets: marks rotate with the part (object space).
- Team livery: soot covers team paint; team emission dims under soot.
- Additive window-light overlays (`URP/Particles/Unlit`, `URP/Unlit`) are separate renderers → lights stay lit inside marks (dropped by user).
- Save/load: level restores from saved hull. Multiplayer: view-only, no network data.

## Files

- Shaders: `Assets/Art/Shaders/Units/ShipHullDamage.hlsl`, `ShipLitInput.hlsl` (bits, `UnityPerMaterial` props), `ShipLitForwardPass.hlsl` (varyings, call), `ShipWreckForwardPass.hlsl`, `ShipOutlinePass.hlsl` (mask), `ShipLit.shader` / `ShipWreck.shader` (material props).
- C#: `Components/TeamColor/UnitRenderStateComponent.cs`, `IUnitRenderStateComponent.cs`; `Entities/BaseEntity/UnitHullDamageSync.cs`, `EntityFeatureBindings.cs`; `Services/Player/HullDamageSettings.cs`, `HullDamageService.cs`; `Services/UnitWreck/*`; `Components/ViewComponents/Wreck/UnitWreckView.cs`; entity wreck callers; `SkirmishScope.cs`.
- Editor: `Editor/Rendering/HullDamagePreviewWindow.cs`, `HullDamageMarkScaleTool.cs`, `HullDamageTextureBuilder.cs` (dead).
- Data: `Assets/Settings/Data/Models/Players/HullDamageSettings.asset`, `SceneContext.prefab` reference, 876 unit + 774 wreck materials (Mark Scale).
- Tests: `Ships/UnitRenderStateComponentTests` (bits, 31-step quantization, repair, outline unaffected, wreck value), `Ships/DefendPlatformTests`, `Core/SerializedReferenceTests`, `Core/RealEntityCompositionTests`, `BattleInput/UnitHighlightFacadeTests`; `Health/WreckMaterialSyncTests` relevant for material edits.

## Verification

- C# compile clean; 96 shader variants (Ship Lit forward/outline/outline-mask + Wreck forward; D3D, Metal, Vulkan; none / instancing / DOTS / shadows+cluster) clean after every change.
- Previews rendered through the real `HullDamageService` (pixel-identical to the preview path): 9 ships (Victory, Venator, Imperial Venator, Tector, both Arquitens, both Acclamators, Executor), close-ups at 0 % hull, 8-step Tector strip.
- Not done: tests (override), in-game look, GPU timing.

## Lessons

- `Mathf.SmoothStep(from, to, t)` ≠ GLSL `smoothstep(edge0, edge1, x)`.
- `line` is a reserved HLSL word ("unexpected token 'line'").
- Shader include edits are not always reimported with `ShipLit.shader`: force `ImportAsset(…, ForceUpdate)` on the shaders before previews.
- `Material.SetFloat` right after a forced shader reimport can be lost; set values after the import settles.
- A 3D-cell scatter must measure distance along the surface, or marks become faint rings.
- No `ddx` inside per-pixel branches: compute derivatives before them or use finite differences.
- Emissive glow across plating reads as lava at RTS zoom; glow only on small rims.
- Unity strips the DXBC `STAT` chunk; count SHEX opcodes for static cost.

## TODO

1. User: judge in-game; tune with the preview window.
2. Implement R1 + R2, then measure with `RenderAuditAblationCapture` (user Play Mode); decide R3–R5.
3. R6: delete `HullDamageTextureBuilder` + `HullDamageDetail.png`.
4. Phase 2 (hit-placed scorches) only if approved: needs a per-unit slot table (bits 21–30) and root-space positions; would also allow aligned window-light darkening.

## History (2026-11-20, condensed)

- First build: noise-threshold patches with glowing cracks and a generated detail texture → rejected (too big, too strong, grew, "lava").
- Scattered soot marks that appear per step; 3×3×3 search for bigger marks; per-material Mark/Opacity scale; auto-scale tool; random size 1–2×; random opacity; darker centre; +30 % marks.
- Texture removed (tiles); seamless 2-mark blend; solid centre.
- Window-light darkening investigated (236 additive materials; 96 non-engine overlays misaligned with hull transforms) → dropped.
- Stars → sharper stars + ray lines → rounder lobes + organic burnt patch with glowing rim → off-centre sunken patch; 31 steps.
