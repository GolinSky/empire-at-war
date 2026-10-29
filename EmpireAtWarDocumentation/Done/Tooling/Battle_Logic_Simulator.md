---
category: Tooling
status: done
created: 2026-10-13
tags:
  - editor
  - balance
  - simulation
  - editor-hub
completed: 2026-10-13
---
# Battle Logic Simulator

## Goal

- Run headless, logic-only battles in the Editor: ship vs ship, or fleet vs fleet. No scene, no Play Mode, no views.
- Answer balance questions: time to kill, winner, win rate, survivor hull/shields, which subsystems fall first.
- Open from the Editor Hub: `Tools/Empire At War/Game Design/Battle Simulator`.

## Decision

- **Chosen:** separate UI Toolkit window, embedded in the Editor Hub through `[EditorToolInfo(..., typeof(BattleSimulatorWindow))]`. Category `Game Design`, beside Balance Editor.
  - Why: Balance Editor is already 327 lines in its window and is a data editor; simulation is a separate workflow.
  - Avoid: a new tab inside `BalanceCombatView`. Optional later link: "Simulate pinned units" from Balance Combat.
- **Reuse runtime logic, do not reimplement:** `HealthModel`, `SquadronHealthModel`, `HardPointModel`, `CombatModifiers`, `WeaponModel.RollHit`, `DamageMatrixData`, `WeaponsData.GetProfile`. All are pure C#; damage/shield/hardpoint/hull rules stay single-source.
  - Avoid: copying the `ApplyDamage` formula into Editor code. Formula drift would make results wrong after any balance change.
- **Reuse Balance inventory:** `BalanceRegistration` for unit list, faction, class, prefab mounts (`HealthComponent` ShipUnits + `WeaponComponent.hardPoints`).
- **Data source toggle:** live assets (default) or the current Balance Editor draft (via `BalanceWeaponPreview` for weapon profiles). Simulator never writes assets.
- **Deterministic:** fixed timestep, seeded `System.Random`. Same seed + same data → same result. Monte Carlo over N seeds for win rate.

## Simulation Model (v1)

### Tick loop

- Fixed step `dt = 0.05 s` (20 Hz); runtime beam damage granularity is `0.1 s`. Time limit default `600 s` → result `Draw / timeout`.
- Per tick: advance weapon cooldowns → fire ready mounts → resolve scheduled impacts → shield regen → remove destroyed units → check end.
- End: one side has no live units, or time limit.

### Weapons

- Each installed weapon mount fires its `WeaponProfile` cycle: `shotsPerSalvo` shots, `shotInterval` apart, next salvo after `CycleSeconds = max(reload, (shots − 1) × shotInterval)`.
- First salvo at `t = 0 + stagger` (random 0..reload per mount, seeded) to avoid unrealistic alpha strikes. Toggle: "Alpha strike".
- Hit roll: same expression as `WeaponComponent.RollHit` → `matrix accuracy × profile.AccuracyMultiplier × profile.GetAccuracyBias(targetClass) × modifiers.AccuracyMultiplier`.
- Damage: `profile.Damage × GetDamageBias(targetClass) × modifiers.GetDamageMultiplier(target)`, applied through `HealthModel.ApplyDamage(damage, damageType, hardPointId)`.
- `strikecraftOnly` (point defense) fires only at fighter/bomber/interceptor classes; idle otherwise.
- Destroyed weapon hardpoint → mount stops firing. Uninstalled (`UnlockLevel > level`) → never fires.
- Ion-disabled modifiers → mount does not fire (v1: no ion weapons applied unless the profile/ability sets it; record as limitation).

### Targeting

- Unit target policy per side (dropdown): `Focus weakest` (default), `Focus strongest`, `Spread (round-robin)`, `Same class first`.
- Hardpoint choice: random live hardpoint of the target, matching `AttackData` default `HardPointType.Any`. Option: focus `ShieldGenerator` / `Engine` / weapons to model player micro.
- Shields absorb first unless the damage type is shield-piercing — handled by `HealthModel`, not by simulator code.

### Health and shields

- `ShipData` / `SquadronData` hull, shields, `HardPointHealth`, regen value/delay; level 1 by default, optional upgrade level → `HealthModel.Upgrade`.
- Shield regen: same rule as `HealthComponent.Tick` → every `ShieldRegenerateDelay` s add `ShieldRegenerateValue × ShieldRegenMultiplier` unless the shield generator is lost.
- Squadrons: `SquadronHealthModel` with member count from prefab/data.

### Range and timing (v1 assumptions)

- All units start in range of each other; no movement, no firing arcs, no line of sight.
- Optional "Arc factor" per unit: fraction of mounts that can bear (default 100%; preset 50% = broadside approximation).
- Projectile travel time: `distance / projectileSpeed` delay with a user-set engagement distance (default = shooter's `ShipData.Range`). Beams use their damage duration ticks.
- Range mismatch option (v2): longer-range side gets `(rangeA − rangeB) / closingSpeed` seconds of free fire.

## UI

- Two columns: **Side A** / **Side B**. Each: searchable unit picker (reuse Balance picker filters), quantity stepper per unit, faction tint, target policy.
- Settings row: data source (Live / Balance draft), seed, runs (1 / 10 / 100 / 1000), time limit, alpha strike, arc factor, engagement distance, level.
- **Run** button; progress bar for >100 runs; cancel. Editor main thread, chunked by `EditorApplication.update` to stay responsive.
- Results:
  - Summary: winner, win rate A/B/draw, TTK mean / median / p10 / p90, survivors left (count, hull %, shields %).
  - Timeline chart (single seed): total hull + shields per side over time; kill markers.
  - Per-unit table: time destroyed, damage dealt, damage taken (shield / hull / hardpoint), hit rate, DPS realized vs Balance base DPS.
  - Event log (single seed, collapsible): kills, shield down, shield generator lost, hardpoints destroyed.
- Quick presets: `1v1 mirror`, `1 vs N of X` (find break-even count), `Matrix` (v2: every ship vs every ship of a class → TTK grid, CSV export).
- Copy results as Markdown / export CSV to `Library/BattleSimulator/`.

## Rules

- Editor-only code in `Assets/Scripts/Editor/BattleSimulator/`; pure simulation core has no `UnityEditor` dependency and no `UnityEngine.Random`/`Time`.
- Simulator never mutates ScriptableObjects or prefabs. Live data is read; draft data uses in-memory copies (`BalanceWeaponPreview`).
- One top-level type per file; `UPPER_SNAKE_CASE` consts; no constructor null guards; fail fast on missing `HardPointHealth`, missing weapon profile, zero/negative cycle.
- Missing data → visible error on the unit row, run blocked. No silent fallbacks.
- No Play Mode runs by agents; verify via compile + EditMode tests.
- UI Toolkit only; follow Editor Hub / Balance styling (`EditorHub.uss`, `BalanceEditor.uss` tokens).

## Implementation

### Core (pure C#)

- `SimUnit` — wraps `HealthModel` or `SquadronHealthModel`, `CombatModifiers`, list of `SimMount`, side, class.
- `SimMount` — `WeaponProfile`, linked `HardPointModel` (for destroyed check), cooldown, salvo state.
- `SimUnitFactory` — builds `SimUnit` from `BalanceUnit` (data + prefab mounts) and chosen data source.
- `BattleSimulation` — tick loop, impact queue, targeting, end check. Input: `BattleSetup`; output: `BattleResult`.
- `ITargetPolicy` + 4 strategies (Strategy pattern).
- `BattleResult` / `UnitCombatStats` / `BattleEvent` — data only.
- `MonteCarloRunner` — N seeds → `BattleSummary` (win rate, percentiles).

### Editor

- `BattleSimulatorWindow` (EditorWindow + `[MenuItem("Tools/Empire At War/Game Design/Battle Simulator")]` + `[EditorToolInfo]`).
- `BattleSimulatorSetupView`, `BattleSimulatorResultsView`, `BattleSimulatorTimelineChart` (UI Toolkit `Painter2D`).
- Window state persisted (sides, quantities, settings) through `EditorPrefs`/`ScriptableSingleton` like `BalanceWindowState`.

### Editor Hub

- Hub discovers it automatically via `EditorToolCatalog.Discover()` (menu under `MENU_ROOT`).
- Add row to `Tools/EditorHub/README.md` inventory (`Game Design | Battle Simulator | Current`) and update entry count; extend `Tools/EditorHub/VerifyEditorHub.cs` if it asserts the catalog.

## Edge Cases

- Mount ↔ hardpoint mapping: verify `WeaponComponent.hardPoints` entries are the same objects as `HealthComponent` ShipUnits IDs; if not, map by explicit reference, never by name.
- Units with no weapons (carriers) → deal 0 damage without hangars; v1 shows warning "Hangar not simulated". v2: spawn squadrons from `HangarBays` with `HangarInitialDelay` / `HangarLaunchInterval`.
- Ship weapon range follows `ShipData.Range`, not `WeaponProfile.Range` (see Balance Editor edge cases).
- Both sides die on the same tick → `Draw (mutual)`.
- Overkill: damage on a destroyed target is wasted; count it as `overkill` in stats.
- Shield-piercing types skip shields but still hit hardpoints → hull × `HullDamageMultiplier`.
- Abilities (Power to Shields, Fleet Command, Sensor Jamming, ion pulses) not simulated in v1; label results "no abilities".
- Research/upgrade multipliers: default none; optional `CombatModifiers` hull/shield multipliers in v2.
- 1000 runs of large fleets: target ≤ 5 s on Editor; profile before optimizing.

## Files

- New: `Assets/Scripts/Editor/BattleSimulator/` (core + UI), `Assets/Scripts/Tests/Editor/BattleSimulator/`.
- Reused: `Assets/Scripts/Components/Health/HealthModel.cs`, `Components/Health/HardPoints/HardPointModel.cs`, `Components/Squadron/Health/SquadronHealthModel.cs`, `Components/CombatModifiers/CombatModifiers.cs`, `Components/Weapon/WeaponModel.cs`, `Components/AttackComponent/{WeaponProfile,WeaponsData,DamageMatrixData}.cs`.
- Reused Editor: `Editor/Balance/{BalanceRegistration,BalanceUnit,BalancePrefabBindings,BalanceWeaponPreview,BalanceDpsEstimator}.cs`.
- Hub: `Editor/EditorToolCatalog.cs`, `EditorToolInfoAttribute.cs`, `Tools/EditorHub/README.md`.
- Related: [[Done/Tooling/GameDesign_Balance_Editor]], [[Done/Refactoring/Weapon_Taxonomy_Balance_System]], [[Done/Features/AI_Composition_Counters]] (`UnitCombatProfileFactory` analytic DPS — compare against sim).

## TODO

1. [x] **Confirm scope with user** — v1 assumptions (all in range, no movement/arcs/abilities/hangars), default target policy, Live vs Draft default.
2. [x] **Data binding spike**
   - Build `SimUnit` for 3 ships + 1 squadron from `BalanceRegistration`; verify mount ↔ hardpoint ID mapping and `HardPointHealth` coverage for all ships.
   - Verify: EditMode test lists every roster ship with ≥1 hardpoint and resolved profiles; report units that fail.
3. [x] **Simulation core**
   - Tick loop, salvo cycle, hit roll, impact delay, shield regen, targeting strategies, end conditions.
   - Verify EditMode tests: deterministic seed; analytic check (1 weapon, 100% accuracy, no shields → TTK = hull / DPS ± 1 tick); shield regen; shield generator loss; piercing; point defense ignores capitals; destroyed weapon stops firing; draw/timeout.
4. [x] **Monte Carlo + summary** — percentiles, win rate, cancel, chunked execution. Verify 1000× 1v1 under target time.
5. [x] **Window UI + Editor Hub**
   - Setup/results/timeline/log views, persisted state, Balance draft toggle.
   - Verify: hub shows tool under Game Design and embeds the window; README inventory + count updated; compile clean; Console 0 errors.
6. [x] **Export** — Markdown copy and CSV to `Library/BattleSimulator/`.
7. **Deferred v2 (outside agreed scope; separate approval)** — hangar squadrons, range-closing, abilities, upgrade/research modifiers, class matrix grid.
8. [x] **Plan lifecycle** — update backlog, move to `Done/Tooling/` when agreed scope is verified.

## Implementation progress

- 2026-10-13: v1 authorized; live assets default, focus weakest, no movement/arcs/abilities/hangars. v2 remains outside this task.
- Verification in progress: bindings → core tests → batches → Editor window/Hub → export.
- Live source correction: `WeaponComponent.DAMAGE_TICK_INTERVAL` is `0.1 s`; simulation step remains `0.05 s`, beam damage uses runtime `0.1 s` granularity.

- Core stage committed: `1047f74d0`. EditMode filter `Battle`: 42 passed / 0 failed (4.46 s), including simulator core, roster audit, draft isolation and summary tests.
- Audit: 65 roster entries, 56 valid; 9 explicit weapon/health binding errors. Report: `Library/BattleSimulator/binding-audit.txt`. Simulator blocks affected rows; no combat assets changed.
- Snapshot optimization: capture weapon profiles once per factory; full roster audit 2.605 s. Full Balance registry hangar ambiguity bypassed only in combat-only registry path (hangars excluded from v1).

- Editor/export stage committed: `df09b6b80`. Final EditMode `Battle` group: **48 passed, 0 failed**, **4.19 s**. Compilation clean; no new Console errors after verification cursor 125 (historical unrelated errors preserved).
- **1000 Thranta 1v1 seeds: 2.275 s** (snapshot setup excluded), 511 A / 486 B / 3 draw; mean 33.854 s, median 33.7 s, p10 31.15 s, p90 36.35 s. Cancel and five-file export passed; `Library/BattleSimulator/verification.json`.
- Hub: 36 tools; simulator category/embedded window verified. Run-button smoke: 10 seeds, 200 px chart, 1400 px workspace, fleet columns contained.
- **Completed v1:** roster adapter, runtime-model simulation, deterministic timing, targeting, batch summaries/cancel, persisted UI, live/draft snapshots, timeline/log, CSV/Markdown exports. Usage and exact rules: `Tools/BattleSimulator/README.md`.
- **Data limitations (reported, not repaired):** missing explicit health bindings on MC75 Profundity, MC80 Independence, Victory I Advanced Loadout, Victory II Advanced Loadout, ISD III, Executor, Raider, AcclamatorAssault and Tector. Those 9 rows block Run; 56 other roster entries validated.
- **Additional explicit v1 assumptions:** no missile interception or fighter attack-run restrictions; upgrade level unlocks only (stat multipliers remain 1); arc factor rounds mount count in binding order; detailed results retained for first seed only. TTK excludes timeouts. Wasted-damage statistic counts already-destroyed targets/members.

### Review follow-up — 2026-10-13

- **Decision:** non-targetable weapons removed from the game (user request; suspected battle issue). Weapons outside `HealthComponent.ShipUnits` were never damaged and fired all battle.
- Gameplay data: **237** weapons added to health lists, `spawnDestroyedExplosion` on — Acclamator Assault 4, Executor 123, ISD III 19, Raider 10, Tector 15, Victory II 12, Victory I 10, MC75 Profundity 22, MC80 Independence 22. Raider `hardPointHealth` was empty → `Weapon: 160 × 1` (CR90/Thranta value).
- Simulator: in-flight impacts on a subsystem destroyed meanwhile are dropped while others live (runtime `AttackData.CanTarget`); ion pulses use prefab `flightDuration`; bearing mounts chosen by seeded shuffle, out-of-arc mounts replace destroyed ones; Level removed (ships/squadrons never upgrade; only stations); draft read from `Library/BalanceEditor/draft.json` (no open window needed); window one statement per line.
- Roster test now requires every unit to build: 65/65 (`RosterAudit_BuildsEveryUnit_WithEveryWeaponTargetable` passed).
- Tests: `Battle` 50/51 — only failure was the new fixed-flight test's expectation (0.9 → 0.85 s, exact tick); corrected, rerun blocked by an unrelated in-progress compile error in `FactionUnitUi.cs`. `Balance` 13 failures = existing Liberator hangar ambiguity. Full EditMode failures (18) are unrelated (AI production, wrecks, team colors, taxonomy, UI scale, height tiers, loadout order).
- Balance impact: affected ships now lose weapons under fire → expect lower sustained DPS for Executor/ISD III/Tector/Victory/MC75/MC80/Raider. Re-check matchups with the simulator.
