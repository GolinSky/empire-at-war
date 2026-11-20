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

## User feedback on first Phase 1 build (2026-11-20)

- Patches far too big: at low hull the whole ship was one scorch → many small marks.
- Too strong → soft, semi-transparent soot (reference "Variant A — Scorch Stains / Burnt Paint").
- Marks must not grow: each damage step adds a new mark at another spot.
- Burnt plating with embers read as lava → no glow on plating. Glow only inside real openings (reference "Variant C", critical damage) → belongs to Phase 3 breaches.
- Consequence: repair now removes marks in reverse order instead of shrinking them.

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

Status 2026-11-20: implemented, compiled, preview-rendered; reworked the same day after user feedback (scattered soot marks, no glow). In-game judgement pending (TODO 3).

Current mark model (replaces the noise-threshold mask in step 2 below):

- Hull space split into 3D cells of `markSpacing` world units; each cell: one spot (random xyz) + random appear-share `w` (PCG4D hash, 8 cells searched per pixel).
- Mark on when `w < level × markDensity` → each of the 15 levels switches on a new batch of marks elsewhere; marks never grow.
- Mark = disk on the surface: spot must lie within one radius of the surface (`|dot(offset, normalOS)| < r`), distance measured in the surface plane → full-size disk even for deep spots.
- Radius = `markSize × spacing × [0.5, 1]`; clamped in `HullDamageService` to `0.5 / sqrt((1 + breakup)² + 1)` (≈ 0.3) so a mark never leaves the 8-cell search.
- Look: soot `lerp(albedo, sootColor, opacity × (1 − smoothstep(0.35, 1, d)))`, edge frayed by detail height; blistered paint (`blisterColor`, metallic 0.6) only in the middle; smoothness → matte; running lights dim under soot.
- Removed: embers/emission glow, `_Time` flicker, normal bump from height derivatives (pixel-varying early-out made `ddx` unsafe; invisible at RTS zoom).
- Second early-out: pixels farther than `1 + breakup` radii from any active mark skip all texture work.
- Starting values (asset): spacing 8, size 0.3, density 0.85, breakup 0.35, soot opacity 0.7, detail size 4.
- Per-ship override (2026-11-20): `Ship Lit`/`Ship Wreck` material properties `_HullDamageMarkScale` (multiplies spacing and radius together, keeps the 8-cell search valid; fewer, bigger marks) and `_HullDamageOpacityScale` (multiplies soot opacity, result saturated). Default 1 → the 1,461 existing materials keep the shared look. In `UnityPerMaterial` → no batching or variant cost.
- Set a ship's source hull material and its wreck copy to the same values (`WreckMaterialSyncTests`; tool: Tools/Empire At War/Rendering/Materials/Sync Wreck Materials).
- Victory: `Victory_Hull.mat` + `Victory_Hull_Wreck.mat` → mark scale 1.3, opacity scale 1.2. `Victory_Lights.mat` is URP Unlit → window lights never darken under soot (explains the earlier "lights stay lit" note).
- Pitfall: `Material.SetFloat` in the same call as a forced shader reimport was lost on save; set material values after the import finishes.
- 2026-11-20 user: marks too small and too few → +50% both. Search widened from 2×2×2 to 3×3×3 cells (27 PCG4D hashes per damaged pixel, nested x/y/z loop: `int` `%`/`/` drew D3D slow-divide warnings); radius clamp `1 / sqrt((1 + breakup)² + 1)` ≈ 0.59 of spacing; `markSize` 0.3 → 0.45 at unchanged spacing 8 → marks 1.5× wider and 1.5× more often hit the hull (count +50%). Cost not measured.
- 9-ship preview (Victory, Venator, Imperial Venator, Tector, both Arquitens, both Acclamators, Executor): marks read well on Victory / Imperial Venator / Tector / Acclamator Assault. World-unit size does not fit every hull: Arquitens Imperial (radius 23) gets almost none, Executor (radius 1,056) turns to dust, Tector reads as round dots. Open: per-ship size → see TODO 5.

- [x] 1. Input accessors + outline mask
- [x] 2. `ShipHullDamage.hlsl`
- [x] 3. Globals service
- [x] 4. `SetHullDamage` + seed
- [x] 5. Hull → damage wiring (ships, stations, platforms)
- [x] 6. Wreck inherits damage + seed
- [x] 7. Editor preview

As built (deviations from the steps below):

- Step 3: `HullDamageSettings` asset (`Assets/Settings/Data/Models/Players/HullDamageSettings.asset`) → `HullDamageService` (`SkirmishScope`, ticks quality level). Globals: `_HullDamageMap`, `_HullDamageParams` (detail repeats/unit, 1/spacing, radius, density), `_HullDamageSurface` (soot opacity, edge breakup), `_HullDamageSootColor`, `_HullDamageBlisterColor`, `_HullDamageQuality` (quality level; `< 2` → 1 planar sample).
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

- Hull repair → level drops → newest marks disappear first (replaces "patches shrink" after the 2026-11-20 feedback).
- Marks use world units: a mark is the same size on a CR70 and an Executor, so huge ships carry many more marks. Revisit if the Executor looks speckled.
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
- First build preview (hull 1 / 0.6 / 0.3 / 0): growing patches with glowing cracks → rejected by user (see feedback).
- Rework (2026-11-20): 96 variant compiles again clean; console clean. Preview render (`VictoryShipView`, hull 1 / 0.8 / 0.6 / 0.4 / 0.2 / 0, quality 4): no marks at 100%, a few small soot spots at 80%, more separate spots each step, no glow.
- Shader include edits are not always picked up by `ShipLit.shader` on refresh: force `ImportAsset(..., ForceUpdate)` on `ShipLit.shader`/`ShipWreck.shader` before preview renders.
- Tests: authored/updated, not run (local override). `Ships`: `UnitRenderStateComponentTests` (bit layout, quantization, repair, damage does not enable outline, wreck value), `DefendPlatformTests` (removed stale `_owner`). `Core`: `SerializedReferenceTests` (+`hullDamageSettings`), `RealEntityCompositionTests` stub. `BattleInput`: `UnitHighlightFacadeTests` stub.
- Not measured: GPU cost (`RenderAuditAblationCapture`), in-game look.
- Known: some window lights on the Victory preview stay lit inside burns → likely separate emissive renderers outside `meshRenderers`; check in-game.

## Lessons

- `Mathf.SmoothStep(from, to, t)` interpolates between values; it is not GLSL `smoothstep(edge0, edge1, x)`. Misuse left cracks/embers at ~33% everywhere in the first bake.
- A 3D-cell mark scatter must measure distance along the surface: a plain sphere test turns most marks into faint rings because the hull cuts the sphere off-centre.
- Emissive "embers" on plating read as lava at RTS zoom; keep glow for real openings only.

## TODO

1. ~~Source of the damage texture~~ → generated: `Tools/Empire At War/Bake Hull Damage Texture` (`HullDamageTextureBuilder`) → `Assets/Art/Textures/Vfx/HullDamageDetail.png` (512², linear, repeat, BC7). Authored art can replace the PNG later.
2. ~~GPU Resident Drawer re-batching~~ → Unity 6000.6 docs: RSUV changes per-renderer shading "without using different materials or additional CPU time" and is designed to combine with the GPU Resident Drawer.
3. After Phase 1: user judges in-game → decide Phase 2. Tune `HullDamageSettings` with the preview window (spacing 8, size 0.45, density 0.85, breakup 0.35, soot opacity 0.7 are first guesses).
5. ~~Mark size vs ship size~~ → user chose the auto-scale tool (2026-11-20), see below.

## Mark size range + auto scale (2026-11-20)

- User: random mark size, smallest = current size, largest 2×. Settings now `markRadiusMin` 3.5 / `markRadiusMax` 7 world units (at Mark Scale 1); `markSpacing`/`markSize` removed. Spacing derived in `HullDamageService` = `max × sqrt((1 + breakup)² + 1)` ≈ 11.8 → tightest spacing that keeps the largest mark in the 3×3×3 search. `_HullDamageSurface.z` = min/max; shader `radius = max × lerp(min/max, 1, rand)`. `markDensity` 0.85 → 1.
- Effect: per area about half as many marks as before, each ~2× wider; covered area roughly 2×.
- Tool: `Tools/Empire At War/Rendering/Materials/Auto Scale Hull Damage Marks` (`HullDamageMarkScaleTool`, editor only, one-time; rerun after adding units — overwrites hand tweaks).
  - Mark Scale = `1.3 × hullRadius / VictoryRadius(58.3)`, clamped 0.05–32 (material Range widened to match). Hull radius = half-diagonal of world AABB of `UnitRenderStateComponent.meshRenderers` in the prefab.
  - Materials shared by units of different size → geometric mean + warning (only `SciFiLamps_*`, 0.76–2.3).
  - Writes the same value into existing wreck copies `<name>_Wreck` under `Assets/Art/Materials/Wrecks`; does not call `Sync Wreck Materials`.
  - Result: 876 Ship Lit materials + 774 wreck copies. Examples: Arquitens 0.45, Arquitens Imperial 0.53, Acclamators 1.08–1.09, Venator 1.39–1.55, Imperial Venator 1.56, Tector 2.28, Executor 23.5, Victory 1.3.
- Do not use `Sync Wreck Materials` for this: it (a) reset 15 deliberate-or-drifted wreck values (Gladiator/HeavyDreadnought/Pelta `_TeamRimStrength` 0, Procurator metallic/smoothness, MC80bRemonda/Mediator invalid keywords, Invincible base colour) and (b) created 49 wreck materials for ISDI/ISDII that no wreck prefab references. Both reverted/deleted. The drift itself is pre-existing → `WreckMaterialSyncTests` may already flag these 15; not touched.
- Random opacity (2026-11-20, user): per-mark opacity `lerp(sootOpacityMin 0.7, sootOpacityMax 1.0, t²)`, `t` = mark's own hash (`frac(w × 113.9)`) → most marks near 0.7, fully dark ones rare. `sootOpacity` renamed `sootOpacityMin` (`[FormerlySerializedAs]`, asset keeps 0.7); max goes to `_HullDamageSurface.w`. Mark search returns (distance, t) of the nearest mark. Material Soot Opacity Scale multiplies the result, saturated. Verified: 96 variants clean, 9-ship preview; settings + service compiled together in memory only (project compile still blocked by the unrelated spawn-point work).
- Dark centre + more marks (2026-11-20, user): opacity = `lerp(max(sootOpacityCenter 0.95, own), own, smoothstep(0, centerFade 0.6, frayed))` → dark core fading slowly to the mark's own opacity, then the soft edge. New global `_HullDamageCore` (x centre opacity, y fade share). Mark count +30% → spacing × 1/√1.3: `markRadiusMax` 7 → 6.1 (min stays 3.5; spacing ≈ 10.3). Verified: 96 variants clean, 9-ship preview + Tector close-up; C# compiled in memory only (project compile still blocked).
- Solid scorch, no tiles (2026-11-20, user: "ugly strange tile on edges", wants solid black centre):
  - Detail texture removed from the marks (its fray + blister flakes showed as blocky tiles up close). Edge now waves with 2 octaves of smooth 3D value noise at `edgeNoiseFrequency` (3 per spacing) × `edgeBreakup` (0.35 radii). No texture samples, no quality branch → `_HullDamageMap`, `_HullDamageQuality`, `_HullDamageBlisterColor`, `detailMap`, `detailSize`, `blisterColor`, `HullDamageService.Tick/UploadQuality` deleted. `HullDamageDetail.png` + `HullDamageTextureBuilder` now unused (kept; A channel was meant for Phase 3 breach glow).
  - Two nearest marks blended `1 − (1 − a)(1 − b)` → no hard seam where marks overlap (nearest-only switched opacity abruptly).
  - Solid core: opacity = `sootOpacityCenter` (1.0) up to `0.5 × centerFade`, fades to the mark's own opacity by `centerFade` (0.8), edge fade 0.6 → 1.0 radius. Soot removes metallic fully (`metallic × (1 − soot)`), occlusion × (1 − 0.5 soot), smoothness → 0.1. `sootColor` 0.06 → 0.015.
  - Fixed: `UNITY_BRANCH` had been separated from its `if` by an inserted declaration.
  - Known: additive `URP/Particles/Unlit` light-grid overlays (Tector `*_MeshAdditive`, Executor ambient, Victory lights) draw on top and are not darkened by soot → window-light dots stay visible inside marks. Fix would need those overlay materials to read the damage value.
  - Verified: 96 variants clean; project compile clean again; close-ups (Executor, Victory, Tector, Acclamator Assault at 0% hull); real `HullDamageService.Upload` render pixel-identical to the preview path. Settings asset reserialized (`sootOpacity` → `sootOpacityMin` migrated).
- Light overlays darkening (2026-11-20): investigated, dropped by user. Facts kept for later: 236 additive (`SrcAlpha One`) light materials (159 `URP/Particles/Unlit`, 77 `URP/Unlit`) on units; 276 light renderers share a hull renderer transform, 96 non-engine ones do not (Victory_Lights, Imperator, Titan, stations…), 75 engine/glow/lens. 56/90 units have several hull transforms → marks already break at part seams. Aligning would need unit-root space (per-unit slot table, Phase 2 infra).
- Star scorches + burnt paint + opacity profile (2026-11-20, user):
  - Shape: marks above `starMinSize` (0.35 of size range) become stars, strength `starStrength` 0.8: 5–8 rays per mark, ray length 0.7–1.0 radius (hash per ray), gaps pulled to 0.35; angle from a surface frame (`cross(up|x, normalOS)`). Small marks stay round. Shape distance replaces plain distance in the 2-nearest search.
  - Opacity: centre 1.0 → mark's random middle opacity (0.7–1.0, t²) by 0.35 → `sootOpacityEdge` 0.15 by 0.85 → 0 at the outline. `sootOpacityCenter`/`centerFade` removed.
  - Burnt paint back: dull flakes (`blisterColor` 0.2/0.18/0.16) from smooth value noise ×4.1 inside `blisterRadius` 0.3 of the nearest mark; no texture.
  - `edgeBreakup` 0.35 → 0.2 (stronger fray blurred the rays).
  - `_HullDamageCore` = (edge opacity, star strength, star min size, blister radius); `_HullDamageBlisterColor` back.
  - Verified: compile clean, 96 variants clean, console clean; close-ups (Executor, Victory, Tector, Acclamator Assault) + 9-ship sheet via the real `HullDamageService`.
- Sharp stars on every mark + glowing lines (2026-11-20, user; size unchanged):
  - `starMinSize` 0 (smoothstep(min − 0.15, min) → every mark is a star); ray mask `pow(…, 3)` (was 1.5), gaps 0.25 (was 0.35).
  - "Burnt paint" = the user's name for the old glowing lines, wanted smaller and dimmer. Dull flakes (`blisterRadius`, `blisterColor`) replaced by thin emissive lines along the nearest mark's ray centres: `rayOffset` = arc distance to the ray centre line (returned from the 2-nearest search), bent by smooth noise (±width), width `emberWidth` 0.012 radii, from 0.03–0.1 out to `emberLength` 0.45 of the outline, fading toward the tip. Colour `emberColor` (1, 0.4, 0.1) × `emberStrength` 0.35 → `_HullDamageEmberColor` (a = width). No flicker, no texture.
  - Pitfall: `line` is a reserved HLSL word (geometry primitive) → "unexpected token 'line'"; renamed to `crack`.
  - First pass (width 0.035, strength 1, length 0.6) read as lava stars → toned down.
  - Verified: compile clean, 96 variants clean, close-ups + 9-ship sheet via real service.
- Between star and circle + organic burnt patch (2026-11-20, user ref: soft soot halo with an irregular burnt-metal patch, glowing rim, in the middle):
  - Outline: 6–10 soft lobes (ray mask ^1.5), lobe length 0.6–1.0, dips to 0.5, `starStrength` 0.45 → closer to a circle.
  - Glowing ray lines removed (they repeated the star pattern). Burnt patch: field = noise(×4.5, ×10.3 of the edge-noise frequency) − 0.4 + 0.45·inner − 0.6·(1 − inner), `inner` = 1 − smoothstep(0, `emberLength` 0.45, d). Inside threshold 0.42 → `burntColor` (0.24, 0.14, 0.12) + 5% glow; rim band `emberWidth` 0.03 → `emberColor` × `emberStrength` 1.0. Shape comes from noise at that hull spot → never the same twice, never along the outline. The −0.6·(1 − inner) term stops stray specks outside marks.
  - Globals: `_HullDamageBurntColor` new; `_HullDamageEmberColor.a` = rim width; `_HullDamageCore.w` = patch radius.
  - Verified: compile clean, 96 variants clean, close-ups + 9-ship sheet via real service.
- Verification: 96 variant compiles clean; 9-ship preview shows marks at a consistent share of every hull (Executor now visible blotches; Arquitens Imperial now marked). The final tool version was compiled and run in memory (`script run`) because unrelated uncommitted work (`EnemySquadronCommander.cs`, `ShipSpawnPoints.cs`) blocks the project compile; Editor-assembly compile of the tool still pending on that.
4. Phase 2 only, if approved: audit ship view prefabs — do hull renderers share the view-root space? Yes → store root-space hits, no per-frame work. No → store one world-to-root matrix per slot, written per frame for ships that have hits.
