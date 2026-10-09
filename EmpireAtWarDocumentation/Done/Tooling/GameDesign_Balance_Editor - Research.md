---
category: Tooling
status: done
created: 2026-10-08
updated: 2026-10-09
tags:
  - editor
  - balance
  - research
completed: 2026-10-09
---
# Game Design Balance Editor — Research

## Goal

- Retain verified data ownership and implementation constraints from the 2026-10-08 feasibility pass.
- Supporting evidence for [[Done/Tooling/GameDesign_Balance_Editor]]; reverify against live source before implementation.

## Decision

- User permits ScriptableObjects and existing prefab data fields; presets apply in the Editor.
- User requires explicit shared-across-factions labels and stat editing while comparing ships from the same or different factions.
- Hard-coded AI configuration and role-specific multipliers are deferred; they are not prerequisites for the data editor.
- [Interactive HTML design](file:///C:/Users/golin/.agent/diagrams/game-balance-workbench-2026-10-08.html) is a local layout prototype, not an installed extension or runtime source.
- Prototype verification covered filtering, pinned comparison, draft edits, preset save/load/simulated apply, alias warnings and narrow/desktop layout.
- No Unity implementation, gameplay tests or battle simulation was performed during the design pass.

## Important Values

- Inspected Unity version: `6000.4.7f1`.
- Snapshot: `36` ship roster entries/`36` mapped ShipData entries; no duplicate ship IDs in the inspected faction rosters.
- Factions: Republic `11` ships/`5` squadrons; Separatist `9/3`; Empire `10/6`; Rebellion `6/3`.
- Shared catalogs: `58` weapon profiles and `29` ability definitions.
- Laser Beam and Proton Beam share one serialized settings object; their definition-level duration/recovery fields are separate.
- Counts and values are a dated disk snapshot, not fixed roster limits or evidence that every referenced prefab has been runtime-tested.

## Implementation

### Data ownership

- `ShipData`: health, movement, radar/fire range, ability IDs, hangar settings and hardpoint health; `ShipsData` maps `ShipType` to asset GUID.
- `SquadronData`: per-member health, flight, attack runs, countermeasures, vision, separate weapon range and ability IDs.
- `FactionDefinition`: ship/squadron entries with `FactionData` price, build time, capacity, limit and unlock level; research is faction-owned.
- `WeaponsData`: global `WeaponProfile` values by `WeaponType`; `DamageMatrixData`: class accuracy/damage, shield multiplier and piercing.
- `ShipAbilityCatalog`: definitions by `ShipAbilityId`; each definition has timing/targeting and a polymorphic `SerializeReference` settings object.
- `ShipClassMatchupData`: serialized counter-production preferences; distinct from combat damage matrix and tooltip matchup descriptions.
- Faction membership does not equal ally/enemy relationship; current AI players share `AiPlayerInstaller` and read the same canonical unit/faction data.

### Runtime constraints to preserve

- `WeaponComponent.Initialize` sets range from `IWeaponRangeData.WeaponRange × VisionMultiplier`; it passes that range to hardpoints.
- `ShipData.WeaponRange => Range`. Editing shared `WeaponProfile.Range` does not change ordinary ship/squadron fire range through that path.
- `HealthModel.InitializeHardPoints` selects health by `HardPointType`; all installed mounts of that type use the ship's matching entry.
- `WeaponHardPoint`: serialized weapon ID, `yAxisRange`, `mainBattery`; base `HardPoint`: `unlockLevel`.
- `MissileInterceptorHardPoint`: serialized `range`, `reload`, `interceptChance`.
- Prefabs include nested overrides; inspect effective source/override ownership before writing. Runtime-assigned hardpoint IDs are not preset identities.
- Ability assignment requires the existing subtype's runtime capabilities; tuning parameters does not create the required facades or view components.

### Derived data and estimates

- `WeaponLoadoutBaker` derives `weaponLoadout` from view prefabs and `SquadronData.MemberCount` from fighters.
- Current `BakeAll` writes all ship/squadron data and calls `ApplyModifiedPropertiesWithoutUndo`; reuse derivation with targeted persistence.
- `ShipData.HullBottom/HullTop` are baked geometry values; keep them outside hand-tuned balance fields.
- `UnitCombatProfileFactory` estimates DPS: mount count × damage × shots per salvo ÷ reload, then accuracy/class/shield modifiers.
- That estimate does not account for full firing-sequence timing, movement, arcs, interception, target switching or all ability effects; never label it measured DPS/win rate.

### UI Toolkit

- Editable-card follow-up (2026-10-09): immediate result-click addition with retained search; typed speed/cost/time/range controls; `ShipHeightTier` enum; existing hardpoint weapon assignments only; DPS recalculates read-only.
- Startup bottleneck: repeated `BalancePrefabBindings.Topology` per data field. One computation per unit reduces measured inventory load from 10.605 s to 1.794 s.
- Apply returns its verified inventory, removing the redundant window rebuild. Full preset save: 407 ms; temporary one-field Apply/read-back: 4,273 ms.

- Compare redesign (2026-10-09): full-width workspace, searchable Add unit picker, four cards per row with unlimited rows, individual stat backgrounds and editable health/shield slider/value pairs.
- Baseline/delta labels and detailed editing foldout were removed at the user’s request. Earlier comparison verification below records superseded layouts.
- Current grid verification: 2/9 units at 924/1244/2524 px content widths, fifth-unit addition/removal and draft slider/input changes; `Library/BalanceEditor/grid-check.json`.

- Installed layout now uses `BalanceEditor.uxml` + `BalanceEditor.uss`, a 32 px fixed-height roster and responsive field-section grids.
- Navigation: Units/Compare/Combat/Changes; unit tabs Stats/Weapons/Abilities/Hardpoints; global data and full ability catalog under Tools.
- Draft overview: Hull, Shields, Speed and base DPS estimate (`mounts × damage × shots / reload`); no TTK/simulation implementation.
- `BalanceScrollPosition` persists offsets per view. Restore after virtualized row measurement; ignore unlaid-out trees when capturing offsets.
- UI refinement verified at 1400 × 850 and 960 × 550 px; rerun Balance EditMode 24/24. Report: `Library/BalanceEditor/ui-redesign-tests.json`.

- `EditorWindow.CreateGUI` owns the window tree; use built-in multi-column lists/trees, numeric/enum fields, foldouts and split panes.
- Current `SubclassSelectorDrawer` implements IMGUI `OnGUI`; the new editor needs native UI Toolkit settings controls.
- Draft controls bind to draft state. `SerializedObject` adapters apply the approved change set to live assets.
- [Unity 6.4 Editor windows](https://docs.unity3d.com/6000.4/Documentation/Manual/UIE-HowTo-CreateEditorWindow.html).
- [Unity multi-column lists and trees](https://docs.unity3d.com/Manual/UIE-ListView-TreeView.html).
- [Unity 6.4 SerializedObject binding](https://docs.unity3d.com/6000.4/Documentation/Manual/UIE-Binding.html).

### Implementation evidence — 2026-10-08

- Implemented workbench: `Assets/Scripts/Editor/Balance/`; acceptance tests: `Assets/Scripts/Tests/Editor/Balance/`; menu `Tools/Game Design/Balance Editor`.
- Live inventory: 77 roster entries, including 36 ships and 17 squadrons; 8,907 allowlisted fields. GUID/GlobalObjectId + canonical entry/field keys resolve current serialized paths.
- Stored yaw limits wider than ±180° are valid: runtime compares normalized target yaw against the stored bounds. Validate finite values and minimum ≤ maximum; preserve current limits.
- Unlock level 0 is valid; runtime availability uses current level ≥ required level. Unused generic nested mount enum sentinels with no inheriting consumers are outside the canonical assignment allowlist.
- Shared-source drafts update inheriting cells/loadout previews. Full presets capture effective inherited values; matching owning values do not create redundant prefab overrides.
- Balance tests 24/24; full EditMode 1,279/1,284. Existing failures: AI priority production, two engine fixture cases, Acclamator team-color/helper meshes.
- Default Republic/Separatist battle startup and running passed; Apply blocked in Play Mode. Shutdown exposed existing null-parent errors in economy/core UI route disposal; no runtime changes made for them.
- Persistence verified on temporary copied/synthetic assets: narrow writes, nested-source ownership, managed aliases, derived loadouts, preset round trips, conflict rejection and rollback/restore.

### Review findings resolved — 2026-10-09

- Presets previously persisted mutable prefab inheritance/consumer state in compatibility schema; local Apply invalidated their own snapshots. `PresetSchema` now keeps structural compatibility separate from `Schema` used for live conflicts.
- `WeaponType` IDs `16`, `17`, `18` lack registered profiles. Assignment choices exclude them; recovered drafts report the missing profile without throwing during DPS rendering.
- Live-only consumer indexing hid newly assigned weapon/ability fields. `BalanceDraftUsage` overlays staged assignment values without mutating registry ownership or conflict baselines.
- Compare previously exposed shared nested-source fields but omitted owning mount fields. Owning fields now use `mount/<canonical field key>`; unrelated mounts display N/A across ships.
- Comparison row context and shrinking roster names resolve ambiguous profile labels and clipped pin controls.

## Files

### Source

- `Assets/Scripts/Entities/Ship/Data/{ShipData,ShipsData}.cs`.
- `Assets/Scripts/Entities/Squadron/Data/SquadronData.cs`.
- `Assets/Scripts/Entities/Faction/Model/{FactionDefinition,FactionData,StationLevelData}.cs`; `Research/`.
- `Assets/Scripts/Components/AttackComponent/{WeaponsData,WeaponProfile,DamageMatrixData,DamageTypeProfile}.cs`.
- `Assets/Scripts/Services/ShipAbilities/{ShipAbilityCatalog,ShipAbilityDefinition,ShipAbilityId}.cs`; `Abilities/*/*Settings.cs`.
- `Assets/Scripts/Components/Health/{HardPointHealth,HealthModel}.cs`; `Assets/Scripts/Components/Weapon/WeaponComponent.cs`.
- `Assets/Scripts/Components/ViewComponents/Health/{HardPoint,WeaponHardPoint,MissileInterceptorHardPoint}.cs`.
- `Assets/Scripts/Editor/AI/WeaponLoadoutBaker.cs`; `Assets/Scripts/Editor/SubclassSelectorDrawer.cs`.
- `Assets/Scripts/Services/Enemy/{UnitCombatProfileCatalog,UnitCombatProfileFactory}.cs`.
- Deferred: `Assets/Scripts/Entities/EnemyFaction/Models/EnemyAiDifficultyProfile.cs`.

### Assets

- `Assets/Settings/Data/Ship/`; `Assets/Settings/Data/Squadron/`.
- `Assets/Settings/Data/Factions/{Republic,Separatist,Empire,Rebellion,Shared}/`.
- `Assets/Settings/Data/Models/Weapon/{WeaponsData,DamageMatrixData}.asset`.
- `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset`.
- `Assets/Settings/Data/AI/ShipClassMatchupData.asset`.
- `Assets/Prefabs/Models/Ships/`; resolve other consumers through current project mappings.
