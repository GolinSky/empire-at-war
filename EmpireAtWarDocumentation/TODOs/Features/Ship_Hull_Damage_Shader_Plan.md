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

- Ships show scorch marks, burnt plating and glowing breaches as hull drops; later, marks sit where hits landed.
- Keep one `EmpireAtWar/Ship Lit` variant, SRP Batcher and GPU Resident Drawer grouping.
- No mesh swaps, no per-ship materials, no per-ship textures.

## Decision

- Chosen: extend `Ship Lit` (`CreateSurfaceData`), damage state per renderer through `SetShaderUserValue`, shared damage textures as globals.
- Why: team color and outline already use this path; 1,461 materials reference `ShipLit.shader` and need no edit; wrecks reuse `CreateSurfaceData` → damage carries over at death.
- Avoid: URP Decal feature → DBuffer + forced depth-normal prepass, one draw per decal, projection bleeds onto neighbouring hulls.
- Avoid: per-ship painted mask RT → imported hull UVs overlap/mirror, one texture per ship breaks batching.
- Avoid: `MaterialPropertyBlock`, material keywords, new `multi_compile` → break batching or add variants (see Metal compile stalls in [[Rules/BATTLE_PERFORMANCE_GUIDELINES]]).
- Avoid v1: parallax, vertex displacement. `Ship Lit` excludes parallax by design.

## User decisions (2026-11-20)

- Repair: scorches shrink when hull is repaired (level follows `HullPercentage`).
- Scope: ships, stations and defence platforms.
- Phases: build Phase 1 only. Judge in-game, then decide on Phase 2. Phase 2/3 stay unapproved.

## Verified source facts (2026-11-20)

- `unity_RendererUserValue`: bits 0–7 team, 8–10 hover/selected/attack. Bits 11–31 free.
- `UnitRenderStateComponent.ApplyUserValue`: `(userValue >> HIGHLIGHT_SHIFT) != 0` decides the outline layer → must mask to 3 bits once higher bits carry data.
- `ShipOutlinePass.hlsl:28`: `flags = unity_RendererUserValue >> HIGHLIGHT_USER_VALUE_SHIFT` → same mask needed.
- `UnitWreckView.cs:87` writes only the team value → must copy damage bits.
- `ShipWreckForwardPass.hlsl:111` calls `CreateSurfaceData` → shader work is shared.
- `IHealthModelObserver`: `HullPercentage`, `OnValueChanged`. `ImpactEffectPresenter.Play` already resolves `ImpactSurface.Armor` + world position + damage.
- Ship views hold 1–7 `MeshRenderer`s (CR70 1, Executor 7), each with its own object space.
- GPU Resident Drawer: on for High Fidelity/Ultra/Cinematic, off for Performant/Low/Balanced.

## Implementation

### Phase 1 — hull-driven procedural damage (no hit positions)

Status 2026-11-20: implemented, compiled, preview-rendered. In-game judgement pending (TODO 3).

- [x] 1. Input accessors + outline mask
- [x] 2. `ShipHullDamage.hlsl`
- [x] 3. Globals service
- [x] 4. `SetHullDamage` + seed
- [x] 5. Hull → damage wiring (ships, stations, platforms)
- [x] 6. Wreck inherits damage + seed
- [x] 7. Editor preview

As built (deviations from the steps below):

- Step 3: `HullDamageSettings` asset (`Assets/Settings/Data/Models/Players/HullDamageSettings.asset`) → `HullDamageService` (`SkirmishScope`, ticks quality level). Extra globals: `_HullDamageSurface`, `_HullDamageCharColor`, `_HullDamageMetalColor`, `_HullDamageEmberColor`. `_HullDamageQuality` = quality level; `< 2` → 1 planar sample, no embers.
- Step 5: not in the entity classes. `UnitHullDamageSync` (pure C#, `EntityFeatureBindings.RegisterHullDamage`) subscribes `OnValueChanged` → `SetHullDamage`. Why: one class instead of 3 copies; `Ship.cs` already 321 lines. Mining facilities excluded (scope decision).
- Step 6: `IUnitWreckService.Spawn(data, unit, uint renderUserValue, delay)` replaces `PlayerId owner`; callers pass `IUnitRenderStateComponent.WreckUserValue` (team + damage + seed, no highlight). Wreck pattern uses the undeformed position.
- Step 7: `Tools/Empire At War/Hull Damage Preview` window (`HullDamagePreviewWindow`), not `ShipLitSetupTool`.
- Seed: `GetEntityId().GetHashCode()` hash → 5 bits; no shared random state.
- Level: `ceil((1 − hull) × 15)` → any hull loss shows level ≥ 1.

User-value layout:

| Bits | Use |
|---|---|
| 0–7 | team (unchanged) |
| 8–10 | highlight (unchanged) |
| 11–14 | damage level 0–15 |
| 15–19 | pattern seed 0–31 |
| 20–29 | hit slot, 0 = none (Phase 2) |

1. `ShipLitInput.hlsl`: add `HIGHLIGHT_USER_VALUE_MASK 0x7u`, damage/seed accessors; mask in `ShipOutlinePass.hlsl`.
2. New `ShipHullDamage.hlsl`: `ApplyHullDamage(inout SurfaceData, positionDamage, normalWS, level, seed)`.
   - Coordinates: object position × object scale (world-unit pattern, anchored to the hull). New `Varyings` field.
   - Mask: `saturate((noise − threshold(level)) / edge)` on a fixed noise field → patches only grow as hull drops; old marks stay.
   - Height blend: mask + detail height → irregular burnt edge instead of a soft gradient.
   - Detail: one packed global texture, triplanar (3 samples). R height, G cracks, B roughness, A ember mask.
   - Output: albedo → char; smoothness ↓; metallic → bare metal in cores; occlusion ↓; normal from height derivatives; emission = ember colour × edge band × slow flicker.
   - `level == 0` → skip whole block (per-draw coherent branch).
3. Globals set once per battle: `_HullDamageMap`, `_HullDamageParams` (tiling, edge width, ember strength), `_HullDamageQuality`.
   - Quality 0–1: tint + mask only, 1 sample, no embers. 2+: full. Global float, not a keyword.
4. `IUnitRenderStateComponent.SetHullDamage(float hullPercentage)` → quantize to 16 levels → `ApplyUserValue`. Seed assigned in `Construct`.
5. Entity (`Ship`, station, platform) subscribes health `OnValueChanged` → calls `SetHullDamage`. Components do not reference each other.
6. `UnitWreckView`: copy damage + seed bits into the wreck value.
7. `ShipLitSetupTool` / editor: preview slider that sets the user value on a selected ship.

### Phase 2 — localized hit scorches

1. `HullDamageHitTable` service: one global `RGBAFloat` texture, 8 hits × 1,023 slots. Texel = hit position xyz + radius; second texel row block = intensity/age if needed.
2. Slot lease per unit on spawn, release on wreck end. Slot index → bits 20–29.
3. Sources: armor impacts above a damage threshold (from `ImpactEffectPresenter.Play`), every hardpoint destruction (always recorded, larger radius).
4. Policy: 8 hits per ship; new hit replaces the weakest; nearby hits merge and grow. Upload at most once per frame, only when dirty.
5. Shader: loop 8 `LOAD_TEXTURE2D`, sphere falloff, `max` with the Phase 1 mask; same height blend and detail.
6. Hit space: see TODO 1.

### Phase 3 — optional

- Breach look for hits above a size threshold: dark girder albedo + strong normal + ember rim (second reference image), no parallax.
- Sparks/smoke VFX at the largest hit, through the existing pooled hardpoint effects.

## Important Values

- Extra cost at `level == 0`: one branch.
- Damaged pixel, full quality: 3 texture samples + noise; Phase 2 adds 8 texel loads.
- Hit table: 8 × 1,023 × 16 B ≈ 131 KB.
- `SetShaderUserValue` calls: ≤15 per ship life from hull changes.
- Per-pixel budget, tiling, char colours, ember HDR value: unset → tune in preview scene, then measure with `RenderAuditAblationCapture` (damage on/off/restored).

## Edge Cases

- Hull repair → level drops → patches shrink (decided).
- Shields up → no armor impact → no new hit marks; hull % unchanged → nothing appears. Correct.
- Shield-piercing damage → hull drops under shields → marks appear under the shield. Accepted.
- Multi-renderer ships → Phase 1 pattern differs per part (fine); Phase 2 needs a shared space.
- Turrets/rotating parts → pattern rotates with the part (object-anchored). Correct.
- Team livery: damage applies after `ApplyTeamMask` → scorches cover team paint. Team emission stays.
- Outline passes ignore damage bits after the mask fix.
- Squadron fighters: tiny on screen → leave at level 0 unless requested.
- Slot exhaustion (>1,023 units) → slot 0, Phase 1 only.
- Save/load: level restores from saved hull; hit list is not saved (visual only).
- Multiplayer: view-only, no network data.

## Files

- `Assets/Art/Shaders/Units/ShipLitInput.hlsl`, `ShipLitForwardPass.hlsl`, `ShipOutlinePass.hlsl`, new `ShipHullDamage.hlsl`
- `Assets/Scripts/Components/TeamColor/UnitRenderStateComponent.cs`, `IUnitRenderStateComponent.cs`
- `Assets/Scripts/Components/ViewComponents/Wreck/UnitWreckView.cs`
- `Assets/Scripts/Components/Weapon/ImpactEffectPresenter.cs` (Phase 2 source)
- `Assets/Scripts/Entities/Ship/` entity wiring
- Tests: `Tests/Editor/Ships/UnitRenderStateComponentTests.cs` (bit layout, outline layer with damage bits set), `Tests/Editor/Health/WreckMaterialSyncTests.cs`; categories `Ships`, `Health`, `Combat`, `Ui`.
- New art: packed damage texture (height/cracks/roughness/embers), shared by all ships.

## Verification (2026-11-20)

- C# compile: 0 errors, 0 warnings. Unity console: no new errors.
- Shader: 96 variant compiles (Ship Lit `ForwardLit`/`ShipOutline`/`ShipOutlineMask` + Ship Wreck forward; D3D, Metal, Vulkan; none / `INSTANCING_ON` / `DOTS_INSTANCING_ON` / shadows+cluster) → all succeed, no warnings. `ddx` inside the per-draw damage branch compiles cleanly.
- Preview render (`VictoryShipView`, hull 1 / 0.6 / 0.3 / 0, quality 4 and 1): marks grow monotonically, char + glowing cracks at full quality, char only at quality 1.
- Tests: authored/updated, not run (local override). `Ships`: `UnitRenderStateComponentTests` (bit layout, quantization, repair, damage does not enable outline, wreck value), `DefendPlatformTests` (removed stale `_owner`). `Core`: `SerializedReferenceTests` (+`hullDamageSettings`), `RealEntityCompositionTests` stub. `BattleInput`: `UnitHighlightFacadeTests` stub.
- Not measured: GPU cost (`RenderAuditAblationCapture`), in-game look.
- Known: some window lights on the Victory preview stay lit inside burns → likely separate emissive renderers outside `meshRenderers`; check in-game.

## Lessons

- `Mathf.SmoothStep(from, to, t)` interpolates between values; it is not GLSL `smoothstep(edge0, edge1, x)`. Misuse left cracks/embers at ~33% everywhere in the first bake.
- HDR colours go through `Color.linear` in `HullDamageService`: keep inspector values near 1 and drive brightness with `emberStrength`.

## TODO

1. ~~Source of the damage texture~~ → generated: `Tools/Empire At War/Bake Hull Damage Texture` (`HullDamageTextureBuilder`) → `Assets/Art/Textures/Vfx/HullDamageDetail.png` (512², linear, repeat, BC7). Authored art can replace the PNG later.
2. ~~GPU Resident Drawer re-batching~~ → Unity 6000.6 docs: RSUV changes per-renderer shading "without using different materials or additional CPU time" and is designed to combine with the GPU Resident Drawer.
3. After Phase 1: user judges in-game → decide Phase 2. Tune `HullDamageSettings` with the preview window (patch size 12, detail size 12, coverage 0.7, edge 0.08, ember strength 3 are first guesses).
4. Phase 2 only, if approved: audit ship view prefabs — do hull renderers share the view-root space? Yes → store root-space hits, no per-frame work. No → store one world-to-root matrix per slot, written per frame for ships that have hits.
