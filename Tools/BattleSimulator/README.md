# Battle Simulator

Open **Tools > Empire At War > Game Design > Battle Simulator**. The Editor Hub embeds the UI Toolkit window beside Balance Editor.

## Use

1. Search/filter each side by faction, ship/squadron and class; add units and set quantities.
2. Choose targeting, seed, runs (1 or 10–1000), time limit, distance and alpha strike.
3. Keep **Balance draft** off for live assets. On reads the Balance Editor's saved draft (`Library/BalanceEditor/draft.json`, written on every edit); the Balance Editor does not need to be open. Each run captures a read-only snapshot.
4. Set **Guns bearing** per entry for a broadside approximation. The fraction rounds to a whole mount count; a seeded shuffle picks which mounts bear each run. Out-of-arc mounts keep cycling silently and replace bearing mounts that are destroyed.
5. Run. Cancel retains completed seeds only. Batch work advances in approximately 8 ms Editor-update chunks.
6. Inspect first-seed survivors, per-unit statistics, health timeline and event log. Copy Markdown or export to `Library/BattleSimulator/<timestamp-id>/`.

Exports: `results.md`, `summary.csv`, `units.csv`, `timeline.csv`, `events.csv`. CSV numbers use invariant decimal points and text fields are quoted. Summary TTK excludes timeouts; no resolved battles displays `NaN` in the window and `n/a` in exports. Percentiles use linear interpolation. Realized DPS uses total battle duration. Hardpoint damage overlaps hull damage; do not sum all taken columns. Wasted raw damage records impacts on already destroyed targets, and impacts on a subsystem destroyed in flight while the target still has live subsystems (runtime `AttackData.CanTarget`).

## Simulation rules

- Fixed step: **0.05 s**. Default limit: **600 s**. Seed sequence: `seed`, `seed + 1`, … (32-bit wrap).
- Salvo cycles use `WeaponProfile.CycleSeconds`; initial stagger is seeded `0..reload`, or zero for alpha strike. Shots resolve on the first tick at/after their scheduled time.
- Bolts and missiles use distance/speed; distance **-1** uses the shooter's unit range. Beams use prefab `growthDuration` (arrival) and hold timing with the runtime **0.1 s** damage-tick constant. Ion pulses use prefab `flightDuration` regardless of distance.
- Damage, piercing, shields, subsystem loss and squadron member health use `HealthModel`, `HardPointModel`, `SquadronHealthModel` and `DamageMatrixData`. Accuracy uses `WeaponModel.RollHit`; profile biases and default `CombatModifiers` apply.
- Runtime shield timers initially are ready. Ships restart the regeneration timer only when below maximum shields; losing all shield generators blocks regeneration. Squadron passive hull repair uses the runtime model.
- Every living mount schedules its shots before impacts resolve in a tick; simultaneous lethal shots can produce a mutual draw. Already-emitted projectiles remain scheduled when their source dies. Battle ends when a side is eliminated.
- Unit policies: weakest/strongest absolute hull + shields, round-robin per attacker, same class then weakest. Subsystem focus falls back to random live points. An impact whose point died in flight is dropped while other points live; after all installed points are lost, damage continues through destroyed points to hull, following the health model.
- Factory mapping follows explicit `HealthComponent.ShipUnits`, `WeaponComponent.hardPoints`, and squadron fighter gun/additional-gun references. It never matches object names or trusts serialized ship IDs.

## v1 limits

- No movement, arcs, line of sight, range rejection, abilities, hangar launches or missile interception.
- Fighter attack-run restrictions are omitted; stationary squadron guns use normal weapon cycles.
- Units are level 1: ships and squadrons never upgrade at runtime (only stations call `Upgrade`), so hardpoints with `unlockLevel > 1` stay uninstalled. Research multipliers remain 1.
- Carrier rows warn **Hangar not simulated**; results intentionally expose their unsupported fighter contribution.
- No class matrix grid; use **1v1 mirror** or **1 vs N: add one opponent** and rerun to explore break-even counts.
- First-seed timeline/per-unit data are retained; aggregate results cover every completed seed.
- Simulator code never saves gameplay assets. Temporary profile/matrix clones are disposed.

## Data audit — 2026-10-13

- First audit: **65 roster entries; 56 valid; 9 blocked** — weapons missing from `HealthComponent.ShipUnits` (non-targetable, never damaged, fired all battle).
- Fixed in gameplay data: all **237** such weapons added to their ship's health list, with `spawnDestroyedExplosion` enabled: Acclamator Assault 4, Executor 123, ISD III 19, Raider 10, Tector 15, Victory II 12, Victory I 10, MC75 Profundity 22, MC80 Independence 22.
- Raider Corvette had no `hardPointHealth`; added `Weapon: 160 × 1` (CR90/Thranta corvette value).
- Every roster unit must now build: `RosterAudit_BuildsEveryUnit_WithEveryWeaponTargetable`. A weapon outside the health bindings is a data error again.

`BalanceInventory.BuildCombat()` uses the same roster/field registration and canonical draft keys, excluding hangar-bay editing and unrelated global systems. This avoids an existing duplicate Liberator hangar identity truncating the combat roster; the regular Balance Editor validation is unchanged.

## Verification

```powershell
unity command run_script --file Tools/BattleSimulator/AuditBindings.cs --entry AuditBindings.Run --timeout_ms 30000 --timeout 40 --json
unity command run_script --file Tools/BattleSimulator/VerifySimulator.cs --entry VerifySimulator.Run --timeout_ms 30000 --timeout 40 --json
unity command run_script --file Tools/EditorHub/VerifyEditorHub.cs --entry VerifyEditorHub.Run --timeout_ms 30000 --timeout 40 --json
unity command run_script --file Tools/BattleSimulator/VerifyWindow.cs --entry VerifyWindow.Run --timeout_ms 30000 --timeout 40 --json
```

`VerifyWindow` sets the simulator to a 10-run Thranta mirror and clicks its embedded Run button. It leaves those example results open. No gameplay scenes are instantiated.

Before EditMode tests, inspect `unity command list_open_scenes --json`; save dirty named scenes and inspect again. An untitled dirty scene blocks tests. Then:

```powershell
unity command run_tests --mode editor --filter Battle --async_tests true --json
unity command test_status --json
```

- Unity 6000.4.7f1: compile clean.
- Final `Battle` test group: **48 passed / 0 failed** in **4.19 s**, including draft health/weapon isolation, beam timing, CSV culture/escaping and result-view checks. Core-stage run: 42/42.
- Full roster binding audit: **2.605 s** after capturing weapon profiles once per setup.
- **1,000 Thranta mirror seeds: 2.275 s**, excluding asset snapshot setup; **511 A wins / 486 B wins / 3 draws**. Mean TTK **33.854 s**, median **33.7 s**, p10 **31.15 s**, p90 **36.35 s**. This measures a 1v1 case; arbitrary large fleets are not promised the same runtime.
- Cancellation and all five export files passed. Benchmark report: `Library/BattleSimulator/verification.json`.
- Hub verification: **36 tools**, **5 legacy actions**, simulator embedded in Game Design.
- Embedded Run-button check: **10 completed seeds**, **200 px** chart, **1400 px** workspace, no overlapping fleet columns.

## Files

- `Assets/Scripts/Editor/BattleSimulator/`: simulation, snapshots, batch runner, window, results and exports.
- `Assets/Scripts/Tests/Editor/BattleSimulator/`: behavior, data isolation, binding audit and workflow checks.
- `Tools/EditorHub/VerifyEditorHub.cs`: catalog and embedding smoke verification.
