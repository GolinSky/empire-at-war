---
category: Bugs
status: in-progress
created: 2026-09-30
tags:
  - code-audit
  - bug
---
# Scene Teardown and Material Leaks

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · Wave 1 · Unity lane: yes (prefabs)

## Goal
- No `MissingReferenceException` from pooled views when the battle scene unloads.
- No leaked `Material` instances from `Renderer.material`.

## Findings
- **RT1 (P2, Likely):** `UnitWreckService.Dispose:60` and `UnitExplosionService.Dispose:57` → `pool.Release(view)` → `view.Hide()` → `gameObject.SetActive(false)`. During scene unload the pooled views (children of a scene root created with `new GameObject`) can be destroyed before Zenject disposes the service. Commit `356a6460` fixed the same class of bug in `MiniMapUi`.
- **RT2 (P2, Confirmed):** `Renderer.material` returns a new instance that the caller must destroy.
  - `UnitSpawnView.cs:25`: one instance per renderer for every placement preview; `Destroy()` only destroys the GameObject.
  - `FogOfWarSystem.cs:79`: `_fogMaterial` is never destroyed.
- **RT3 (P3):** both pool services repeat the same code: lazy root GameObject, `ObjectPool`, active list, tick release, dispose.
- **CO4 part:** `UnitSpawnView.cs:22` calls `GetComponentsInChildren<MeshRenderer>()`.

## Files (owned)
- `Assets/Scripts/Services/UnitWreck/UnitWreckService.cs`
- `Assets/Scripts/Services/UnitExplosion/UnitExplosionService.cs`
- `Assets/Scripts/Entities/Reinforcement/UnitSpawnView.cs` + every prefab with a `UnitSpawnView` (the `ReinforcementData` spawn wrappers)
- `Assets/Scripts/Components/ViewComponents/FogOfWarSystem.cs`
- Optional new shared pool type (RT3), e.g. `Services/Pooling/`; read [[Architecture/PROJECT_ORGANIZATION]] first.
- Do **not** edit `ReinforcementService.cs` (owned by Identity Keys).

## Steps
1. [x] RT1: during teardown, skip `Hide()` for views already destroyed. Unity `== null` is the correct check here: `?.` is banned, and teardown order is not controlled. Comment why, as `MiniMapUi` does. Alternative: when the root is already gone, skip the release step and only `pool.Dispose()`.
2. [x] RT2 `UnitSpawnView`: replace `GetComponentsInChildren` with a `[SerializeField] MeshRenderer[] meshRenderers`. Assign it on each spawn prefab (Unity lane). Destroy the created materials in `Destroy()`/`OnDestroy`. Alternative: `MaterialPropertyBlock` for the color tint, which creates no instances. Prefer this if the shader exposes `_Color`/`_BaseColor`.
3. [x] RT2 `FogOfWarSystem`: destroy `_fogMaterial` in `OnDestroy`, or use `sharedMaterial` if the material asset is used only by fog (check it is not shared).
4. [x] RT3 (optional, after 1–3): extract one small pooled-view lifetime collaborator used by both services. Keep spawn/sizing logic in each service. Skip it if the shared part ends up under ~30 lines.
5. [x] Unity: refresh, reserialize only the changed prefabs, save, check the Console for errors.

## Implementation (`38f3e681`)
- `Services/Pooling/ViewPool<TView>`: lazy root + `ObjectPool`. `Release` skips destroyed views, and `actionOnDestroy` guards with `view != null`. Used by both services.
- Wreck pools now use one root per data asset: `UnitWrecks_<data.name>`, replacing the shared `UnitWrecks` root.
- `UnitSpawnView`: `[SerializeField] MeshRenderer[] meshRenderers` + `MaterialPropertyBlock` on `_BaseColor`, **material slot 0 only**. Slot 0 is always `Hologram` (URP Simple Lit). StarDestroyer slots 1+ (`StarDestroyer2_ColorBaked`/`_Emission01`) keep their look, as before.
- Assigned on 17 prefabs (16 in `Prefabs/Ui/Reinforcement/` + `Prefabs/Models/Ships/StarDestroyer1ReinforcementView.prefab`) with the same set as the old `GetComponentsInChildren` (active only).
- `FogOfWarSystem.OnDestroy` destroys `_fogMaterial` and `_fogTexture`; the texture was also leaking.

## Remaining
- [ ] Manual: skirmish → wrecks/explosions → exit to menu → no Console exceptions.
- [ ] Manual: open/cancel placement ×20 → material count flat.

## Edge Cases
- Exiting Play Mode vs loading another scene → both go through Dispose; handle both.
- `UnitSpawnView` is created and destroyed on every placement start/cancel → leak grows per click.
- `ObjectPool.Dispose` calls `actionOnDestroy` → `Object.Destroy(view.gameObject)` on already-destroyed views is safe in Unity, but it is still worth guarding for clarity.

## Verification
- Compile clean (`scriptCompilationFailed=false`).
- Manual: start a skirmish, destroy units (wrecks and explosions spawn), exit to the menu → no exceptions in the Console.
- Manual: open and cancel reinforcement placement about 20 times → Memory Profiler material count stays flat.
- Tests: none required; run only on request.
