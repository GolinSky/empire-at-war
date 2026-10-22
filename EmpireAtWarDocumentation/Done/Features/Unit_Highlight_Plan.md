---
category: Features
status: done
created: 2026-10-22
completed: 2026-10-22
---
# Unit Highlight Plan

Research: [[Done/Features/Unit_Highlight_Plan - Research|current shader, batching and material-change inventory]].

## Decision / Completion

- Completed 2026-10-22: feature implementation, settings asset, entity/hover wiring, prefab integration, and EditMode verification.
- User scope: skip runtime battle testing. Steps 7–8 runtime profiling and in-game acceptance are waived for this completion; no new visual or performance result is claimed.
- Verification: compilation succeeded; 95 distinct EditMode tests passed (96 executions, one overlapping filter); Console contains 0 errors.
- Preserved component GUID `dc6da9f12354d5d4780a4aeec9c5dd09`; all 62 prefab components remain injectable. Resolved prefab tests cover derived entities and inactive station-level renderers.
- Evidence: `Logs/UnitHighlight/2026-10-22/`; settings persisted in `Assets/Settings/Data/Models/Players/UnitHighlightSettings.asset` and bound in `SceneContext.prefab`.

## Goal

- Ship Lit units get brighter on hover and on selection, like Empire at War (2005).
- Zero new materials, zero MPBs, zero new shader variants. SRP Batcher and GPU Resident Drawer instancing stay intact.
- One per-unit entry point owns every runtime shader state of a unit's renderers.

## Decision

- **Chosen**: encode highlight bits in the existing renderer shader user value (`MeshRenderer.SetShaderUserValue`), next to the team index. Tuning values are global shader uniforms.
  - Why: per-renderer data already travels with GRD instance data; no material/CBUFFER change; same-mesh same-material ships keep one instanced draw.
  - Why: CPU cost only on state change (hover enter/exit, select/deselect), not per frame.
- **Avoid**: `MaterialPropertyBlock` → removes renderers from GRD. `renderer.material` → per-ship material copies, leaks. Material swap to a "highlight material" → doubles material count, breaks instancing groups. Highlight keyword → doubles variants.
- **Avoid**: per-frame fade by rewriting user values. EaW snaps. No pulse (decided 2026-10-22).
- **Decided 2026-10-22**: own units brighten on hover and selection; enemies on hover only. `CloakView` stays separate.

## Implementation

### User value layout (32-bit uint)

| Bits | Meaning |
|---|---|
| 0–7 | Team: `0` = none, `n` = palette `n - 1` (unchanged) |
| 8 | Hovered |
| 9 | Selected |
| 10–31 | Reserved (future: attack-target flash, ability target) |

### Shader (`ShipLitInput.hlsl`, `ShipLitForwardPass.hlsl`)

Implemented 2026-10-22:

1. `GetTeamColor()` reads `unity_RendererUserValue & TEAM_USER_VALUE_MASK` (`0xFF`); Ship Wreck inherits it.
2. `GetHighlight()` → bit 8 (`HOVERED_FLAG`) → `_UnitHighlightStrength.x`, bit 9 (`SELECTED_FLAG`) → `.y`; stronger wins (`max`).
3. Vertex: `nointerpolation half highlight : TEXCOORD8` in `Varyings`.
4. `ShipLitPassFragment`: `surface.emission += GetHighlightEmission(surface.albedo, input.highlight)` → `albedo * _UnitHighlightColor.rgb * highlight`. No rim.
   - Emission is unlit → dark side brightens too (EaW look); fog still applies via `MixFog`.
5. Globals outside `UnityPerMaterial`: `half4 _UnitHighlightColor; half4 _UnitHighlightStrength; // x hovered, y selected`. Unset → 0 → no highlight until step 3.
6. ShadowCaster / DepthOnly / DepthNormals: no change. Ship Wreck fragment never calls the highlight (its `ToLitVaryings` leaves `highlight = 0`).

### Single entry point: `UnitRenderStateComponent`

- Renamed `TeamColorView` → `UnitRenderStateComponent`; original `.meta` GUID and 62 prefab references preserved with Unity `move_asset`. Serena was unavailable during rename; exact code references were updated surgically.
- Owns `meshRenderers` (existing serialized list) and the cached `uint` user value.
- API: `SetHighlight(UnitHighlight flags)`; team set in `[Inject] Construct` as today. Writes renderers only when the composed value changes.
- `[Flags] enum UnitHighlight { None = 0, Hovered = 1, Selected = 2 }` in its own file.
- `static uint GetTeamUserValue(...)` kept for `UnitWreckService` (wreck never carries highlight bits).
- Only this component calls `SetShaderUserValue` on unit renderers. `UnitWreckView` keeps its own call (pooled wreck, not a unit).

### Entity wiring (per `AGENTS.md` entity rules)

- New `IEntityHighlightFacade : IEntityFacade { void SetHovered(bool isHovered); }`.
- New `UnitHighlightFacade` (pure C#, entity scope): holds hover + selected flags, listens to `ISelectionModelObserver.OnSelected`, calls `IUnitRenderStateComponent.SetHighlight`.
- Register via `EntityFeatureBindings.RegisterHighlight` in Ship, DefendPlatform, Station, MiningFacility installers.
- Selected state follows `SelectionModel` → local-player units only (matches ring). Enemies: hover only (decided).

### Hover source: `UnitHoverService` (Skirmish scope, `ITickable`)

- Uses `SelectionQuery.TryFindAt(pointer)` + `IHardPointHoverObserver` (hardpoint hover → owner highlighted).
- Same gates as `WorldTooltipPresenter.CanHover`: HUD visible, not dragging, not over UI, not fog-hidden, not destroyed.
- Tracks one hovered entity; on change → old `SetHovered(false)`, new `SetHovered(true)`. Clears on `EntityRemoved`, `LateDispose`.
- Exposes `IHoveredEntityObserver`; `WorldTooltipPresenter` reads it instead of running its own query (one pick per frame).

### Global settings

- `UnitHighlightSettings` ScriptableObject in `Assets/Settings/Data/Models/Players`: color, hover strength, selected strength.
- `UnitHighlightService` (Skirmish scope, `IInitializable`) → `Shader.SetGlobalVector` once; registered next to `TeamColorService`.

## Important Values

- Start: hover `0.35`, selected `0.25`, color white. Tune in-game.
- CPU per transition: `renderers.Length` × `SetShaderUserValue`. Large ships have hundreds of renderers; at most 2 transitions per hover change.
- GPU: ~6 ALU per fragment, no texture fetch, no new variant.

## Edge Cases

- Respawn/pool reuse → `UnitHighlightFacade` resets to `None` on respawn/despawn.
- Death while hovered/selected → `EntityRemoved` clears hover; selection already cleared by `SelectionService`.
- Cloak (`CloakView` swaps `sharedMaterial`) → user value persists on renderer; cloak shader ignores it. Highlight returns on decloak.
- Fog-hidden enemy → `forceRenderingOff`; hover service never picks it.
- Station upgrade (`StationLevelView.ApplyLevel`) → user value stays on inactive level renderers if all levels are in `meshRenderers`. Verified by prefab and station-level EditMode tests.
- Wreck → `GetTeamUserValue` returns team only; mask also guards in shader.
- Prefab preview / editor (value 0) → no team, no highlight.

## Files

- `Assets/Art/Shaders/Units/ShipLitInput.hlsl`, `ShipLitForwardPass.hlsl`, `ShipLit.shader` (header comment).
- `Assets/Scripts/Components/TeamColor/TeamColorView.cs` → `UnitRenderStateComponent.cs`; new `UnitHighlight.cs`.
- `Assets/Scripts/Entities/BaseEntity/EntityFeatureBindings.cs`, `EntityFacades/IEntityHighlightFacade.cs`.
- Entity installers: Ship, DefendPlatform, AsteroidDefendPlatform, Station, MiningFacility.
- `Assets/Scripts/Services/UnitWreck/UnitWreckService.cs`, `Services/VContainer/Scopes/SkirmishScope.cs`.
- `Assets/Scripts/Entities/Tooltip/WorldTooltipPresenter.cs`.
- `Assets/Scripts/Editor/Rendering/ShipLitSetupTool.cs` (adds the renamed view).

## Steps

- [x] 1. Shader: team mask + highlight bits + globals. No new keywords/variants.
  - Verified 2026-10-22: `ShaderData.Pass.CompileVariant` on every pass of Ship Lit and Ship Wreck, vertex+fragment, D3D11+Vulkan, keyword sets none / `INSTANCING_ON` / `DOTS_INSTANCING_ON` (+cascade shadows, additional lights, soft shadows, Forward+, SSAO, fog) / vertex lights → all succeeded; `ShaderHasError` false, 0 messages. Visuals unchanged until globals are set (step 3).
- [x] 2. Rename `TeamColorView` → `UnitRenderStateComponent`, add `SetHighlight`; GUID preserved; 62 prefabs still resolve.
- [x] 3. `UnitHighlightSettings` + `UnitHighlightService` globals.
- [x] 4. `IEntityHighlightFacade` + `UnitHighlightFacade` + installer registration for all Ship Lit entity types.
- [x] 5. `UnitHoverService`; tooltip consumes `IHoveredEntityObserver`.
- [x] 6. EditMode tests: user value composition (team kept, bits set/cleared), facade selection/hover combine, wreck value has no highlight bits.
- [x] 7. Runtime profiling waived by user on 2026-10-22; historical evidence retained below. Original scope: measure GPU instancing in a live match (cheat-spawned fleets, Frame Debugger, FPS, Profiler) before any change to the material "Enable GPU Instancing" flags. Keep the flags: they are the intended optimization for repeated ships.
  - 2026-10-22, High Fidelity (GPU Resident Drawer ON), 48 cheat-spawned Republic ships (8× Acclamator/Arquitens/Venator/HeavyDreadnought = flag ON; 8× Thranta/Victory = flag OFF), combat running:
    - Ship Lit forward: 74 draw calls → 464 instances (6.3 per draw), all `HybridBatch` (BRG). Shadows: 232 draws → 1891 instances (8.2 per draw).
    - Ship Lit never batched as `SRPBatch` or classic instanced draws on this tier; instancing comes from the GPU Resident Drawer, which does not read the material flag.
    - Profiler 90 frames: CPU main median 4.98 ms (p95 6.02), GPU median 2.08 ms (p95 2.51); Game view 147 FPS. Editor numbers.
    - Evidence: `Logs/RenderAudit/20261022T090756401Z-frame-debugger/analysis.md`, `Logs/RenderAudit/20261022T090824183Z-profiler/`.
    - Decision: flags kept. GRD-OFF capture not performed; waived by the user for this feature completion. Material instancing flags remain unchanged.
- [x] 8. Runtime in-game acceptance waived by user on 2026-10-22: hover/select appearance, batching capture, and large-fleet hover CPU were not exercised.

## TODO / Decisions

- Resolved 2026-10-22: enemies hover only; own units hover + selected; no pulse; `CloakView` separate.
