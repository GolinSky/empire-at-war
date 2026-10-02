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
- `BoostWeaponPower` / Fire All Batteries: damage ×2; weapon reload time ×0.3; speed ×1; damage taken ×1; active 7 s; recovery 50 s after the effect ends.
- `Assault`: damage ×2; weapon reload time ×0.5; speed ×2; damage taken ×1; active 7 s; recovery 50 s after the effect ends. Enemy-target attack order retained.
- 2026-10-02 balance pass: both descriptions updated; weapons-only burst has ≈6.67× theoretical DPS; Assault has ≈4× theoretical DPS plus double speed.
- Verification: live catalog readback matches saved asset; catalog is not dirty; no new Unity console errors after the edit. No automated tests or Play Mode run; wider implementation acceptance remains pending.
- Existing unsaved `ProtonBeamSettings.damage = 6000` was preserved when saving the shared catalog; its existing description still says 600.
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
