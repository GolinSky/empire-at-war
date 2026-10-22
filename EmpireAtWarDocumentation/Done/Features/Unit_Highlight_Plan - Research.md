---
category: Features
status: done
created: 2026-10-22
completed: 2026-10-22
---
# Unit Highlight Plan - Research

Plan: [[Done/Features/Unit_Highlight_Plan|Unit Highlight Plan]]. Pre-implementation source snapshot from 2026-10-22. Historical type names and missing-hover findings below describe that baseline.

## Implementation outcome

- `UnitRenderStateComponent` now owns team/highlight composition; `UnitHighlightFacade` combines selection and hover.
- `UnitHoverService` supplies one entity/hardpoint snapshot to highlighting and world tooltips; gates and removal/disposal are covered by EditMode tests.
- Settings asset: white, hover `0.35`, selected `0.25`; global uniforms initialized by the Skirmish scope.
- 95 distinct EditMode tests passed; compilation succeeded; 62 original prefab script references preserved. Runtime battle visuals and performance capture skipped by user request.

## Ship Lit Shader (current)

- Shader `EmpireAtWar/Ship Lit` → `Assets/Art/Shaders/Units/ShipLit.shader` + `ShipLitInput.hlsl`, `ShipLitForwardPass.hlsl`, `ShipLitDepthNormalsPass.hlsl`.
- 1315 materials use it; ships, stations, defend platforms, mining facilities.
- One variant for all unit materials: no material keywords; only URP pipeline `multi_compile`s.
- `UnityPerMaterial` CBUFFER shared by all passes (`ShipLitInput.hlsl`) → SRP Batcher compatible.
- `#pragma multi_compile_instancing` + `#pragma instancing_options renderinglayer` + `DOTS.hlsl` → GPU Resident Drawer (BRG) compatible.
- Team color = global `_TeamColors[8]` (set once by `TeamColorService.Initialize`) + per-renderer `unity_RendererUserValue`.
- User value encoding: `0` = no team, `n` = palette `n - 1`. Read once per vertex in `GetTeamColor()`, passed `nointerpolation` (`TEXCOORD4`).
- `EmpireAtWar/Ship Wreck` (`ShipWreck.shader`) includes `ShipLitInput.hlsl` with `SHIP_WRECK`; also calls `GetTeamColor()`.

## Batching / Instancing (current)

| Setting | Value |
|---|---|
| SRP Batcher | ON in all 6 URP assets |
| GPU Resident Drawer | ON: High Fidelity (default, `m_CurrentQuality: 3`), Ultra, Cinematic; OFF: Performant, Low, Balanced |
| Material "Enable GPU Instancing" | ON 36 / OFF 1279 Ship Lit materials |
| BRG variant stripping | `m_BrgStripping: 2` |

- SRP Batcher wins over legacy material GPU instancing; the material checkbox is ignored for Ship Lit and only adds variants. Unity recommends it OFF in URP.
- Real "same ship + same material → one instanced draw" = GPU Resident Drawer only (High Fidelity+).
- GRD excludes renderers using `MaterialPropertyBlock` → falls back to SRP Batcher per-draw path. This is why team color uses `SetShaderUserValue`.
- Instancing-flag ON materials: HeavyDreadnought 12, Munificent 7, Lucrehulk 4, SciFiLamps 3, Acclamator 2, Venator 2, XQ6Platform 1, Arquitens 1, Providence 1, Recusant 1, RefuelingStation 1, `Materials/Units` 1.

## Runtime Material Changes (current)

### Ship Lit unit renderers

- `TeamColorView` (`Assets/Scripts/Components/TeamColor/TeamColorView.cs`) → `MeshRenderer.SetShaderUserValue(team)` once in `[Inject] Construct(IPlayerRoster, PlayerId)`. Entity scope injection. 62 prefabs.
- `CloakView` → swaps `hullRenderer.sharedMaterial` to `cloakMaterial` and back (shared asset, no instance). 1 prefab (Stealth Corvette). Friendly owners only.
- `FogVisibilityComponent` → `Renderer.forceRenderingOff` (no material change).
- No `MaterialPropertyBlock` and no `renderer.material` on live Ship Lit renderers.

### Other unit-adjacent renderers (not Ship Lit)

- `UnitWreckView` → `new Material(source)` once per source material per pooled wreck (`Awake`), `material.SetFloat` per spawn, `SetShaderUserValue(team)` in `Show`. Destroyed in `OnDestroy`. Fed by `UnitWreckService` via `TeamColorView.GetUserValue`.
- `Shield` → MPB (`_ShieldTime`, `_ShieldColor`, ...).
- `IonStunView` → MPB `_Opacity`.
- `UnitSpawnView` (placement hologram) → MPB `_BaseColor` per slot.

### Scene / service level (not units)

- `CaptureSiteView`, `ReinforcementZoneView`, `PlanetView` → MPB.
- `SpawnAreaOverlay`, `FogOfWarSystem` → `renderer.material` (owned instance).
- `TeamColorService` (Skirmish scope, `IInitializable`) → `Shader.SetGlobalVectorArray("_TeamColors")`.
- `MatchNebula` → `sharedMaterial` assignment.

### Conclusion

- Scope (VContainer services) only sets globals. Per-unit state is written inside the entity by `TeamColorView`.
- No live unit material instancing exists today; adding MPB or `renderer.material` for highlight would break GRD instancing and create per-ship materials.

## Selection / Hover (current)

- Click/marquee/select-all → `SelectionService` → `SelectionContext` → `IEntitySelectionFacade.Select(bool)` → `SelectionFacade` → `SelectionComponent.SetActive` → `SelectionModel.IsSelected` → `OnSelected` → ring `SpriteRenderer.enabled`.
- `SelectionComponent.SetActive` ignores non-local owners (`OtherSelectionContext` entities show no ring).
- No unit hover state exists. Hover is computed only in `WorldTooltipPresenter.Tick` via `SelectionQuery.TryFindAt(pointer)` and `IHardPointHoverObserver.TryGetHovered`, gated by HUD visible, not dragging, not over UI, not fog-hidden, not destroyed.

## Edge Cases Found

- `StationLevelView.ApplyLevel` toggles level model GameObjects; user value persists on inactive renderers → state stays valid if all level renderers are in the view list.
- `_TeamRimStrength`/`_TeamRimPower` are in the shared CBUFFER; used only by Ship Wreck.
- Squadrons do not use `TeamColorView`; out of scope.
