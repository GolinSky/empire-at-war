---
category: Refactoring
status: done
created: 2026-11-06
updated: 2026-11-06
completed: 2026-11-06
acceptance: pending-user
---
# Fog Feature Access and Removal of NoFogOfWar

## Implementation
- [x] Replace startup fog injection with `IReadOnlyList<IBattleRevealStep>`. Reveal runs before the covered frame and fade-out.
- [x] Dispatch map bounds through `IMapBoundsListener`. `FogOfWarSystem` owns the 600-unit reference side, scales from the authored transform and disposes the old grid before replacing the mask.
- [x] Use a shared `IMiniMapOverlaySource` for the single optional texture. Zero sources → disable the minimap fog image.
- [x] Remove `NoFogOfWar`, `RevealedMiniMapFogMask` and both legacy interfaces. Disabled fog registers shared vision with fog off and deactivates the scene fog object.
- [x] Preserve per-entity `VisibleFogFacade` and gameplay visibility rules. Update composition, hazard-loader and order-input fixtures for empty contributor lists.
- Reusable module registration rule → [[Architecture/VCONTAINER_LIFECYCLE]].

## Verification
- Unity Editor compiled the runtime and editor scripts. The loaded fog system exposes both contributor interfaces. Console error count: 0.
- Source scan under `Assets` found zero references to the four removed types. Unity reserialized `SceneContext.prefab` with the reference side on the fog component.
- Tests unrun under the checkout override. Categories: Vision, Map, Ui, GameFlow, Core and BattleInput.
- Fog-on/off startup and minimap, repeated bounds and targeted tests → [[TODOs/User_Acceptance_Queue]].
