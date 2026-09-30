---
category: Refactoring
status: in-progress
created: 2026-09-30
tags:
  - code-audit
  - binding
---
# Explicit Component Binding

[[TODOs/Refactoring/Audit_Remediation_Coordination|← Coordination]] · Wave 1 · Unity lane: yes (prefabs)

## Goal
- Remove the remaining runtime `GetComponent*` / `Shader.Find` lookups (AGENTS "Explicit Component Binding").

## Findings (CO4)

| Site | Lookup | Fix |
|---|---|---|
| `Components/Ui/Base/UiInstaller.cs:31` | `instance.GetComponent<BaseUi>()` after instantiate | instantiate the prefab typed as `BaseUi` (serialized/configured as `BaseUi`) |
| `Components/Utils/DrawCircle.cs:20` | `GetComponent<LineRenderer>()` | `[SerializeField] LineRenderer lineRenderer` (already the field name) |
| `Components/Utils/Ui/SafeAreaHelper.cs:14` | `GetComponent<RectTransform>()` | `[SerializeField] RectTransform rectTransform`, or `(RectTransform)transform` |
| `Entities/ReinforcementZones/ReinforcementZoneView.cs:58` | `_captureCanvas.GetComponent<CanvasScaler>()` | serialize the `CanvasScaler` |
| `Services/SceneContext/ViewInstallers/Base/StaticViewInstaller.cs:22` | `GetComponent<View>()` | `[SerializeField] View view` |
| `Components/Utils/DebugRangeCircle.cs:33` | `Shader.Find("Sprites/Default")` | inject/serialize a `Material` (debug config SO); fails in builds if the shader is stripped |

- Kept: `EntityMediator.cs:88` `collider.GetComponentInParent<IViewEntity>()` → a physics hit has no inspector reference; acceptable.
- `UnitSpawnView` → Teardown/Leaks plan. `AddressableAssetService` → Fail-Fast plan.

## Files (owned)
- The six scripts above + every prefab/scene using them. Find users with `unity command` search or GUID grep in `Assets/Prefabs`, `Assets/Scenes`.

## Steps
1. [x] For each script: add the serialized field, remove the lookup.
2. [x] Unity lane: assign the references on every prefab/scene instance through `SerializedObject`, then `SaveAsPrefabAsset`/`SaveScene`, refresh, check the Console.
3. [x] Extend `SerializedReferenceTests` (or add an Edit Mode test) to assert the new references are non-null on the prefabs. Run only on request.

## Progress (2026-09-30)
- `UiInstaller` → `IAssetService.LoadComponent<BaseUi>` + `Instantiate(BaseUi)`; manual null check removed (LoadComponent throws).
- `DrawCircle` → lookup removed; `lineRenderer` already assigned on all 3 `Borders.prefab` instances.
- `SafeAreaHelper` → `(RectTransform)transform` (no asset change).
- `ReinforcementZoneView` → `[SerializeField] CanvasScaler _captureCanvasScaler`; assigned on `ReinforcementZone.prefab` (Coruscant/Kamino zone prefabs are nested instances → inherit).
- `StaticViewInstaller` → `OnValidate` `GetComponent<View>` removed; `view` already assigned on `Loading.unity`, `CoruscantPlanet`/`KaminoPlanet` prefabs, `Corusant`/`Kamino` scenes.
- `DebugRangeCircle` → takes a `Material`; new `DebugRangeCircleFactory(Material, IRangeDebugObserver)` bound in `SkirmishServiceInstaller` with `[SerializeField] Material rangeDebugLineMaterial` → `Assets/Art/Materials/Vfx/RangeDebugLine.mat` (`Sprites/Default`). `WeaponComponent`/`RadarComponent` inject the factory. Shared material is no longer destroyed per ring.
- `SerializedReferenceTests` → added `_captureCanvasScaler`, `rangeDebugLineMaterial`, `DrawCircle.lineRenderer`, `PlanetInstaller.view`.
- Verified: compile clean, Console has no errors, prefab YAML holds the new references.

## Remaining
- [ ] Run `SerializedReferenceTests` (on request only).
- [ ] Play check: main menu + skirmish, no missing references; range cheat draws rings.

## Edge Cases
- `StaticViewInstaller` is a base class → every derived installer prefab needs the field.
- `DrawCircle`/`SafeAreaHelper` may sit on many UI prefabs → count the users first; report if more than ~20.

## Verification
- Compile clean; no missing-reference errors in the Console when entering the main menu and a skirmish.
