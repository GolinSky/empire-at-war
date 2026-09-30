---
category: Features
status: in-progress
---
# Ship Abilities Plan

## Lifecycle Review

- Reviewed: 2026-09-30; implementation and test sources exist. Commit `dc869bbb` explicitly says the system needs review; no completion/acceptance evidence was found. Keep active pending review.

## Goal

- Ships expose 0..N abilities for single/group selection.
- Player and enemy AI share `ShipAbilityService`.
- Catalog keyed by `ShipAbilityId` owns tuning; `ShipData` owns assigned IDs.

## Responsibilities

- Catalog/settings → configuration and ability construction.
- `ShipAbilitySlot` / `CombatModifiers` → pure runtime state.
- `ShipAbilityService` → activation, slot transitions, timers, cancellation, disposal.
- `<Name>Ability` → behavior; `<Name>Settings` → numbers and `CreateAbility`.
- Presenter/view → ability input, cooldown, pending-target highlight, visuals.

## Rules

- Service alone starts/stops abilities and changes slots.
- New ability instance per activation; no static state/internal timers.
- `Start` changes must be undone in `Stop`; call `Stop` exactly once.
- Entity access uses commands/facades and service contracts; never concrete `Ship`/components.
- Components read modifiers; they know nothing about abilities.
- Dictionary key is the ID; do not repeat it in `ShipAbilityDefinition`.
- Write catalog/symmetry tests; execute only on explicit request.

## Implementation

1. Add enums, definitions, catalog, `ShipData.Abilities`.
2. Add modifier hooks to weapon/health/movement/regen; default multipliers = 1.
3. Add slots, factory, service, engine boost; then other stat abilities.
4. Wire single/group ability bars and targeting cancellation.
5. Add ProtonBeam / ConcentrateFire, enemy AI difficulty policy, assets, and test source.

## Important Values

- `Ready` → `Active` → `Recovering` → `Ready`.
- Catalog: 7 IDs; 7 transparent icons at 256×256, readable at 64 px.
- Starting duration: 8–15 s; recovery: 30–60 s; beam damage ≈3× heaviest shot.
- Venator: `ProtonBeam` + `BoostShieldPower`.

## Edge Cases

- Zero recovery → remove Ready slot from active list before reactivation; no double ticking.
- Cancel → full recovery delay; caster death/scene disposal → Stop once.
- Out-of-range target → reject affected ships, retain targeting, consume nothing.
- Group targeted activation → Ready selected ships share one target.
- Settings rename/move → preserve serialized data with `MovedFrom`; catalog test detects missing settings.
- Beam v1 → first living hardpoint; reuse existing `WeaponType`.

## Files

- [[TODOs/Features/Ship_Abilities_Plan - Research]] — exact APIs, 7 ability definitions, AI presets, hooks, assets, test scenarios.
- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset` — planned catalog.
