---
tags:
  - rules
  - tests
updated: 2026-11-21
---
# TEST_GUIDELINES

## Goal

- Tests catch broken wiring and broken logic, not balance changes.
- Retuning a ship, squadron, ability or economy value must not break a test.

## Rules

- Tests that read shipped data (`Assets/Settings`, unit prefabs) never assert exact tuned values.
- Banned exact values: health, shields, damage, DPS, speed, price, build time, capacity, unlock level, ability recovery/damage, income, research multipliers, yaw arcs, hardpoint counts, squadron sizes, loadout counts.
- Assert wiring and relationships instead (see patterns below).
- When behavior depends on a tuned value, read it from the asset; never repeat the number.
- Logic tests that build their own inputs (`ScriptableObject.CreateInstance`, hand-made models) may use literal numbers.
- Fixed design facts may stay exact: hero `MaxCount == 1`, faction membership, enum identity, asset paths and addresses.
- Policy source: root `AGENTS.md` → `Test Maintenance With Every Change`.

## Patterns

| Instead of | Assert |
|---|---|
| `MemberCount == 6` | `MemberCount == view.GetComponentsInChildren<FighterView>().Length` |
| `TotalHull == 1450` | `TotalHull == LeaderHull + (MemberCount - 1) * MemberHull` |
| `LeaderHull == 500` | `LeaderHull > MemberHull`, `MemberHull > 0` |
| `CombatSpeed == 40` | `CombatSpeed >= CruiseSpeed`, `CruiseSpeed > 0` |
| loadout `(Laser, 5)` | loadout sum == registered `WeaponHardPoint` mounts |
| `RecoveryDelay == 30` | `RecoveryDelay > 0`; timing tests advance by `definition.RecoveryDelay` |
| `Damage == 400` | expected damage = `settings.Damage` read from the asset |
| `ShieldRegen == 1.5` | buff `> 1`; penalty in `[0, 1]` |
| `HangarBays.Length == 2` | `Is.SupersetOf(required types)`; launch points == bays |
| `Income == 120` | `share = data.Income / 2`, upgrades `share * multiplier` from research asset |
| yaw `[-90, 90]` | `Is.InRange` coverage: side arc covers its side, broadsides mirrored |
| `waves == 10` | `SurvivalModel.WAVE_COUNT` |

## Edge Cases

- Hero vs others ("Luke exceeds every squadron") is design intent → relational loop over all `SquadronData` is fine.
- NUnit: `Does.Contain(x).And.Contain(y)` on an enum array resolves to the string overload → use `Is.SupersetOf(new[] { x, y })`.
- Transient `ScriptableObject` used across `AssetDatabase.Refresh` → set `hideFlags = HideFlags.DontUnloadUnusedAsset`, destroy in `finally`.
- Scheduler/performance tests: assert limits, order and eventual completion, never a wall-clock budget.

## Reference Fixtures

- Squadrons: `LukeRedSquadronTests`, `DarthVaderSquadronTests`, `SeparatistGinivexHeroTests`.
- Ships: `ExecutorShipTests`, `ISDIIShipTests`, `EmpireHeroCapitalTests`, `CorvetteLoadoutTests`, `RaiderCorvetteTests`.
- Economy/abilities: `EconomyTuningTests` (`TierMultiplier` helper), `ShipAbilityServiceTests`, `PowerToMainBatteriesTests`, `ShieldAbilityMigrationTests`.
- Cleanup commit: `271d02d4c` (2026-11-20), 17 fixtures converted.

## Files

- Policy: `AGENTS.md` (Test Maintenance, Test Selection, Local Test Execution Override).
- Suite map: `Tools/Jenkins/TEST_INVENTORY.md`.
- Tests: `Assets/Scripts/Tests/Editor/`.
