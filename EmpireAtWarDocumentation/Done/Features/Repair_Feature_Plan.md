---
category: Features
status: done
created: 2026-11-21
completed: 2026-11-21
acceptance: pending-user
---
# Repair Feature

## Goal

- Reusable repair capability: any ship, squadron or satellite can carry it through one feature binding.
- Repairer restores **hull + alive hardpoint health** of friendly ships and all structures in range.
- First carrier: **Repair Satellite**, a 6th satellite type built on the existing satellite pad.

## Status

- 2026-11-21: plan drafted from live source; rule questions answered by the user the same day. No code written. Tests not run.
- 2026-11-21: all open questions answered. Ready for implementation; starting values get tested by the user in battle.
- 2026-11-21: steps 1–7 implemented. Unity compile `0` errors; console clean after asset builds. EditMode tests written/updated, **not run** (local override). Step 8 (battle test) and in-game acceptance → `TODOs/User_Acceptance_Queue`. Step 9 (beams) not started: only on user request.

## Outcome (2026-11-21)

- Health: `HealthModel.Repair(fraction)` + `NeedsRepair`; `IRepairableFacade`/`RepairableFacade` registered in `RegisterHealth` (ships + all structures; squadrons excluded).
- Carrier binding: `EntityFeatureBindings.RegisterRepair(builder, profile)`; `DefendPlatformData.RepairProfile` (optional) → `DefendPlatformInstaller`. New carrier = one `RegisterRepair` line + profile on its data.
- `RepairSourceFacade.CanRepair` = alive && `!CombatModifiers.IsConstructing` (new getter) && `!IsIonDisabled`.
- `RepairService` (battle scope, `SkirmishScope`): host only, per-repairer timer (stagger `0.37 × Interval` steps), long frame → one tick, claims by entity id, cleared in `SimulationResetService`. No per-tick allocations (reused candidate buffer, cached comparer).
- Repair Satellite: `SiteFacilityType.RepairSatellite = 7`, `DefendPlatformType.RepairSatellite = 9`; cost `500` / `25 s`; health copied from Turbolaser satellite data (live `3,000` hull / `5,000` shields, not the plan's `1000`); no weapons, no hardpoints → `HullTarget`.
- Enemy AI: `EnemyDefenseSnapshotBuilder.LoadOptions` skips `RepairSatellite`.
- Radius ring: `Components/Repair/RepairRangeView` (world-space `LineRenderer`, `RangeDebugLine.mat`, width `3`) shown while selected; selection is local-player-own only.
- Model: `.blend` → `Tools/Blender/RepairSatellite/Convert.py` (Blender 3.6.23 background) → FBX; 14 meshes, triangle counts match source; 7 visible meshes, 2 `MeshBumpColorize` materials (checked against ALO binary). Visual scaled to the Quad Laser width (`21.9` local, root ×3); footprint ≈ `55` ≤ `SatelliteFootprintRadius 60`.
- Unity build/registration: `Tools/Blender/RepairSatellite/Build.cs` (`Art`, `Prefabs`, `Ring`, `Data`, `Icon`, `Register`).

## Verification (2026-11-21)

- Unity script compile: passed (`0` errors). Art step: all 14 FBX meshes = source triangle counts, UVs present.
- Icon render inspected: correct orientation; team colour on hub.
- Tests written/updated (not run): `HealthModelTests` (3 repair cases), new `RepairServiceTests` (9), `RealEntityCompositionTests` (repair facades, entity count `24 → 26`), `DefenseSatelliteContractTests` (repair contract + footprint + tooltip cases), `CaptureSiteModelTests`, `BattleSaveRepositoryTests`, `FactionAnnouncementTests` (new cases), `CaptureSiteBuildUiTests` (option count from allowed list instead of `5`).
- Reviewed, no change: `EnemyDefenseStrategyTests` (hand-built options), `BalanceInventoryTests`, `EndGameLossCatalogTests`, `BattleAssetPreloaderTests` (data-driven over the catalog/enum).
- Reusable guidance: none beyond this note and the `RegisterRepair` doc comment.

## User Decisions (2026-11-21)

| Rule | Decision |
|---|---|
| Carriers | ship, squadron, satellite; one shared feature. Satellite first |
| Repairs | hull + alive hardpoints |
| Targets | ships and all structures; squadrons excluded |
| Allies | teammates' units repaired too |
| Amount | small percentage of max health only; no flat HP (`0.5 % + 10 HP` was a test value) |
| Target choice | most damaged in range, kept until full |
| Radius | bigger than `100–150`; shown as a ring when the satellite is selected |
| Targets per satellite | at least `10` at once |
| Repair during combat | yes |
| Shield restoration | no |
| Restore destroyed hardpoints | no |
| Ion-disabled repairer | stops repairing |
| Self-repair | never |
| Visual | repair beam to ships only; optional (user unsure it is needed) → last step, droppable |
| Cost / build time | balanced as a mid-tier satellite |
| Enemy AI | does not build repair satellites for now |
| Starting values | accepted for a first battle test |
| Model | `F:\CodexArtifacts\EaW-Repair-Satellite\Repair Satellite.blend` (from `Rv_satallite_repair.alo`, workshop `1129810972`) |

## Important Values (starting point, tune in the profile asset)

| Parameter | Value | Source |
|---|---|---|
| Repair interval | `1 s` | user |
| Repair per tick | `0.5 %` of max (hull and each alive hardpoint) | agent proposal from user's "small percentage" |
| Flat HP | none | user |
| Max targets (satellite) | `10` | user ("at least 10") |
| Max healers per target | `2` | user (first draft, not changed) |
| Repair radius (satellite) | `400` units, to target centre | agent proposal; user asked "bigger" |
| Cost | `500` | mid tier: median of existing `300 / 400 / 500 / 1000 / 1600` |
| Build time | `25 s` | existing rule `15 s + cost / 50` |

- Full repair from 0 by one satellite = `200 s`; by two = `100 s`.
- Percent-only → hull and hardpoints fill at the same rate.

## Source Facts (verified 2026-11-21)

- `HardPointModel.Repair(float value)` exists; returns when `IsDestroyed`, full or `value <= 0` → destroyed and locked hardpoints stay at 0.
- `HealthModel` has no hull repair. `RegenerateShields` is the only restore path besides `Restore`/`Upgrade`.
- `SquadronHealthModel.RepairHull` = passive self-repair (`SquadronData.HullRepairPerSecond`). Stays as is; not part of this feature.
- Ships and every structure get health through `EntityFeatureBindings.RegisterHealth` (`ShipInstaller`, `SpaceStationInstaller`, `DefendPlatformInstaller`, `MiningFacilityInstaller`).
- `SquadronInstaller` registers `HealthFacade` itself, without `RegisterHealth` → squadrons are not targets.
- `HealthFacade.ApplyDamage` runs only when `IMatchSession.IsAuthority`; clients receive health by replication.
- Hull and hardpoint fractions are already saved → no save change for repaired health.
- `UnitHullDamageSync` follows `OnValueChanged` → hull scorches fade with repair.
- Satellites = `DefendPlatform` entities. Current enums: `SiteFacilityType` `2–6`, `DefendPlatformType` `4–8`, `CaptureSiteKind.Satellite = 2`.
- `DefendPlatformInstaller` always registers radar, weapons and stationary combat.
- No gameplay range ring exists; only `Services/Cheats/DebugRangeCircle`.
- `EnemyDefenseStrategy` scores satellites by DPS per cost → a repair satellite scores `0`.

## Decision

### Shape: one battle service + two facades

- `RepairService` (pure C#, battle scope, `ITickable`) owns every rule: range, interval, target choice, target count, max healers per target.
- `IRepairSourceFacade` on the repairer; `IRepairableFacade` on every repair target.
- Why: the healer limit is a cross-entity rule → one owner; entities talk only through facades; carriers need no per-type code.
- Avoid: a repair component per unit type; repair logic inside `Ship`/`Squadron`/`DefendPlatform`; ability-system coupling.

### Carrier binding (the reusable part)

```csharp
// EntityFeatureBindings
public static void RegisterRepair(IContainerBuilder builder, RepairProfile profile)
{
    builder.RegisterInstance(profile);
    builder.Register<RepairSourceFacade>(Lifetime.Singleton).AsImplementedInterfaces();
}
```

- Installer: `if (Data.RepairProfile != null) EntityFeatureBindings.RegisterRepair(builder, Data.RepairProfile);`
- Now: `DefendPlatformInstaller`. Later, same line: `ShipInstaller`, `SquadronInstaller`.
- `RepairProfile` (ScriptableObject; confirm folder with `PROJECT_ORGANIZATION`): `Radius`, `Interval`, `PercentPerTick`, `MaxTargets`, `MaxHealersPerTarget`.
- Unit data gets one optional `RepairProfile` reference. No profile → no feature.

```csharp
public interface IRepairSourceFacade : IEntityFacade
{
    RepairProfile Profile { get; }
    Vector3 Position { get; }
    bool CanRepair { get; }                       // alive, built, not ion-disabled
    IReadOnlyList<IEntity> Targets { get; }
    event Action TargetsChanged;                  // beams
    void SetTargets(IReadOnlyList<IEntity> targets);
}
```

### Target side

```csharp
public interface IRepairableFacade : IEntityFacade
{
    bool NeedsRepair { get; }
    float HullPercentage { get; }
    void Repair(float percent);
}
```

- Registered inside `RegisterHealth` → ships + all structures; squadrons excluded by construction.
- `HealthModel.Repair(float percent)`:
  - `IsDestroyed` → return.
  - `Hull = min(MaxHull, Hull + MaxHull * percent)`.
  - Each hardpoint: `hardPoint.Repair(hardPoint.MaxHealth * percent)` (existing method skips destroyed/locked).
  - Shields, `IsLostShieldGenerator` untouched. One `OnValueChanged`.
- `NeedsRepair` = `Hull < MaxHull` or any alive hardpoint below max.

### Service rules

1. Track repairers from `IEntityLocator.EntityAdded/EntityRemoved` + `TryGetFacade<IRepairSourceFacade>`.
2. Per repairer: own timer; every `Interval` (scaled time) run one tick. Stagger first ticks.
3. `CanRepair` false → release all claims, clear targets.
4. Keep each current target while valid: alive, allied, `NeedsRepair`, centre within `Radius`. Else release its claim.
5. Fill free slots up to `MaxTargets`: allied (`IPlayerRelations.IsAllied`), not self, has `IRepairableFacade`, `NeedsRepair`, in radius, claims `< MaxHealersPerTarget` → lowest `HullPercentage` first.
6. `Repair(PercentPerTick)` on every target; `SetTargets` when the set changed.
7. Host only (`IMatchSession.IsAuthority`).
- Claims: `Dictionary<long, int>` by target id; cleared on target/repairer removal and in `SimulationResetService`.
- No allocations per tick (reused buffers); read `Rules/BATTLE_PERFORMANCE_GUIDELINES` before implementation.

### Repair Satellite

- New 6th satellite type on the existing satellite pad; same capture → select → build flow.
- Enums, append only: `SiteFacilityType.RepairSatellite = 7`, `DefendPlatformType.RepairSatellite = 9`.
- `CaptureSiteData`: add to the `Satellite` allowed list + `facilityCosts` (cost `500`, build `25 s`).
- AI: not built by the enemy. `EnemyDefenseStrategy` / `EnemySiteConstructionController` must skip `RepairSatellite` explicitly if they enumerate the allowed list; no scoring work.
- Health like the other satellites: hull `1000`, shields `1000`, same shield regen; `hardPointHealth` covers every hardpoint type on the prefab.
- No weapons expected. Check `DefendPlatformInstaller` with an empty weapon list (radar/weapons/stationary combat are unconditional); branch only if it fails.
- Hardpoints: read from the model at import; if it has none, the hull is the only target (`HullTarget`).
- Registrations (same list as the five satellites): `DefendPlatformCatalog`, `SatelliteDefendPlatformFactory`/`SiteFacilityBuilder`, `AssetMappingData`, Addressables `View`, tooltip + icon `512 × 512`, `FactionAnnouncementService`, `BalanceStructureAdapter`, cheats enumerating `SiteFacilityType`, tutorial satellite lessons if they list types.
- Import: `.blend` → FBX → prefab per `AssetPipeline/Guides/ALO_MODEL_IMPORT_GUIDE` and `ADDRESSABLES_BUNDLE_GUIDE`; team color via `UnitRenderStateComponent`.
- Save: `StructureSave` already stores `DefendPlatformType`; new enum value only.

### Visuals

- **Radius ring**: world ring of `Profile.Radius` around the satellite while it is selected (local player, own/allied). New small view on the satellite prefab driven by `SelectionModel`; no existing ring to reuse.
- **Repair beam (optional, last)**: one beam from the satellite to each ship target (up to `MaxTargets`), pooled, driven by `TargetsChanged`. Never to structures. Build only after the user confirms it is wanted following the first battle test.
- Online client: beams would need the target list replicated (pattern: `WeaponTargetReplicaFacade`); health itself arrives by replication. Skipped if beams are dropped.
- Read `UI_UX_GUIDELINES` and `Rules/BATTLE_PERFORMANCE_GUIDELINES` before building ring/beam.

## Implementation

1. **Health**: `HealthModel.Repair`, `NeedsRepair`; `IRepairableFacade` + `RepairableFacade` in `RegisterHealth`.
2. **Profile + binding**: `RepairProfile`, `IRepairSourceFacade`, `RepairSourceFacade`, `RegisterRepair`; optional profile on `DefendPlatformData`.
3. **Service**: `RepairService` + battle-scope registration + reset hook.
4. **Tests (EditMode)** → compile.
5. **Satellite import**: model, materials, prefab, icon.
6. **Satellite type**: enums, data asset with `RepairProfile`, costs, allowed list, registrations, build menu row.
7. **Radius ring** on satellite selection.
8. **User battle test** of the starting values.
9. **Optional**: ship repair beams + client target replication (only on user request).
10. **Follow-ups (not in scope until asked)**: enemy AI building repair satellites, ship/squadron carriers, tooltip repair line, `BattleSimulator`/Balance support, repair SFX.

## Tests (EditMode)

- Category `Health`: add `Services/Repair` to the `Health` row in `AGENTS.md` test table. Satellite wiring → `Capture`.
- `HealthModelTests`: hull + damaged alive hardpoints rise; clamp at max; destroyed hardpoint stays 0; locked hardpoint stays 0; shields unchanged; lost shield generator stays lost; destroyed unit → no-op.
- `RepairServiceTests`: out of range → none; hostile/neutral → none; ally → repaired; squadron → none; self → none; up to `MaxTargets` at once, most damaged first; third healer rejected at limit 2; target kept until full, then replaced; interval respected; ion-disabled → none and claims freed; not authority → none; removed target frees its claim.
- Composition: `RealEntityCompositionTests` resolve `IRepairableFacade` for ship + each structure, none for squadron; repair satellite resolves `IRepairSourceFacade`.
- `DefenseSatelliteContractTests`: repair satellite in the allowed list, has a cost, builder maps `SiteFacilityType` → `DefendPlatformType`, data has a profile with positive values.
- Literal numbers only in logic tests; shipped profile values are never pinned.

## Edge Cases

- Target under construction → repaired up to its current `MaxHull`; check it does not shortcut building.
- Satellite under construction or dead → `CanRepair` false.
- Two satellites on one target → `1 %/s`. Three in range → third picks another target.
- More than `MaxTargets` damaged units in range → the least damaged wait.
- Fleet parked in range gets free sustain in combat → balance through cost, radius and percent.
- Repair satellites repair each other (not self).
- Radius to target centre: a wide station counts only when its centre is inside; revisit if it feels wrong.
- If beams are built: 10 per satellite × many satellites → pool and cap; check draw cost; client targets need the replica path (step 9).
- Enemy AI never builds the repair satellite; a human ally or opponent can.
- Old saves: unaffected unless a repair satellite exists in the save.

## Open Questions

- None. All decisions recorded 2026-11-21.
- After the battle test: keep or drop ship beams; tune `0.5 %/s`, radius `400`, `10` targets, `2` satellites per target, cost `500`.

## Files

- Health: `Assets/Scripts/Components/Health/{HealthModel,HealthComponent}.cs`, `Assets/Scripts/Components/Health/HardPoints/HardPointModel.cs`
- Bindings: `Assets/Scripts/Entities/BaseEntity/EntityFeatureBindings.cs`, `Assets/Scripts/Entities/BaseEntity/EntityFacades/`
- Satellite: `Assets/Scripts/Entities/DefendPlatform/{DefendPlatformInstaller,DefendPlatformType}.cs`, `Assets/Scripts/Entities/CaptureSites/{SiteFacilityType,CaptureSiteData,SiteFacilityCost,CaptureSiteBuildUiController}.cs`, `Assets/Scripts/Services/CaptureSites/SiteFacilityBuilder.cs`
- Pattern: `Assets/Scripts/Services/ShipAbilities/Abilities/HarmonicBomb/HarmonicBombService.cs`, `Assets/Scripts/Entities/Ship/EntityFacades/Health/HealthFacade.cs`
- Reset: `Assets/Scripts/Services/BattleService/SimulationResetService.cs`
- AI: `Assets/Scripts/Services/Enemy/EnemySiteConstructionController.cs`, `EnemyDefenseStrategy`
- New: `Assets/Scripts/Services/Repair/RepairService.cs` (+ profile and facades, one type per file)
- Source model: `F:\CodexArtifacts\EaW-Repair-Satellite\Repair Satellite.blend`
- Related: [[TODOs/Features/Defense_Satellites_Plan|Defense Satellites Plan]], [[TODOs/Features/ARC170_Import|ARC-170 Import]] (Astromech Repair is self-repair, separate)

## TODO

- [x] User answers rule questions (2026-11-21)
- [x] User picks first carrier and model (2026-11-21: repair satellite)
- [x] User answers cost, AI, beam and values questions (2026-11-21)
- [x] Step 1: health repair + `IRepairableFacade` (2026-11-21)
- [x] Step 2: `RepairProfile` + `RegisterRepair` (2026-11-21)
- [x] Step 3: `RepairService` (2026-11-21)
- [x] Step 4: EditMode tests written, compile passed; tests not run (local override)
- [x] Step 5: import repair satellite model (2026-11-21)
- [x] Step 6: repair satellite type, data, registrations (2026-11-21)
- [x] Step 7: radius ring on selection (2026-11-21)
- [ ] Step 8: user battle test of starting values → acceptance queue
- [ ] Step 9 (optional): ship beams + client target replication, only if the user asks
- [ ] User in-game acceptance
