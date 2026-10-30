---
category: Features
status: done
created: 2026-10-30
updated: 2026-10-30
completed: 2026-10-30
acceptance: pending-user
---
# Auto Power Abilities

## Goal

- Automatically press eligible player-ship power abilities when useful.
- Keep the existing ability lifecycle and ship UI; add global and per-ship automation toggles.
- Merge Power to Shields variants into shared id 3.

## Decision

- Latest user clarification, 2026-10-30: auto mode acts like the player pressing the ability button. Lifecycle stays start → duration → end → cooldown.
- No separate refund, cooldown-reset, takeover, or auto-only cancellation system. This supersedes the earlier override/refund proposal.
- Manual control uses existing activation/cancellation rules; automation never cancels or replaces a running ability.
- Auto only chooses Ready slots. Active and Recovering slots remain untouched; disabling automation never stops an existing effect.
- Player ships only; eligible settings types: `BoostShieldPowerSettings`, `BoostWeaponPowerSettings`.
- Exclude squadrons, Concentrate Fire, Hunt/Pursuit, beams, id 24 Power to Main Batteries and id 35 Intensify Firepower.
- Id 20 is shared by Tartan, Recalcitrant and TIE Avenger squadron. Include the ships; exclude the squadron by entity kind.
- Enemy AI activation rules remain unchanged. Shared shield balance changes affect every ship using the merged definition.
- Research and current-code findings → [[Done/Features/Auto_Power_Abilities_Plan - Research]].

### Player controls

- Gameplay setting: “Ships use power abilities automatically”; proposed default on.
- Per-ship A toggle: default on, persisted per ship; shown only for eligible ships.
- Global off → A greyed/not clickable with “Disabled in Settings”; retain each ship's preference.
- Exactly one selected unit and it is this ship → suspend its automatic activation. Multi-selection → automation continues.
- Selecting a ship does not cancel an already running ability. Show “Automatic use paused while individually selected”.
- Group A click: among eligible owned ships, any off → all on; otherwise all off. Show mixed state; ignore ineligible ships.
- A controls future automation only. The ability button controls activation/cancellation; keep these actions separate.

## Power to Shields Merge

Shared numbers = current id 3:

| Duration | Cooldown | Shield regen | Damage | Speed | Cancel |
|---|---|---|---|---|---|
| 15 s | 40 s | ×4 | ×0.75 | ×0.75 | yes |

- Effect is shield regen, not damage reduction → still useful at 0 shields.
- Variants to remove and remap to 3:

| Id | Name | Now | Result |
|---|---|---|---|
| 13 | `NebulonBBoostShieldStrength` | regen ×20, 60 s cd | much weaker |
| 15 | `MonCalCruiserPowerToShields` | regen ×20 | much weaker |
| 17 | `HomeOnePowerToShields` | regen ×20, 20 s cd, fire delay ×3 | much weaker, no fire penalty |
| 26 | `MC80IndependencePowerToShields` | regen ×16, 30/60 s, fire delay ×2; wrongly typed `BoostWeaponPowerSettings` | much weaker; type bug fixed |
| 34 | `CorellianBattlecruiserPowerToShields` | same as 3 | none |
| 45 | `AssaultFrigateMkIIPowerToShields` | regen ×3.2, 8/35 s | slightly stronger |
| 53 | `WavecrestPowerToShields` | regen ×10, 20/60 s | weaker |

- If ×4 is too weak after testing, tune the one shared value.


## Implementation

### 1. Shared shield ability and compatibility

1. Inventory serialized catalog entries, ship ability arrays, SFX entries, source references and tests for ids 13, 15, 17, 26, 34, 45, 53.
2. Replace each ship reference with id 3, preserve slot order and unrelated abilities; remove obsolete catalog variants and stale enum names without renumbering remaining ids.
3. Centralize legacy integer-id remap; execute before save validation and restoration, not only at runtime lookup.
4. Preserve saved remaining cooldowns, even when longer than id 3's recovery. Do not reset cooldown on load.
5. Deduplicate remapped SFX keys and select id 3's shared cue; verify no duplicate-key/catalog-reference failures.
6. Check `BalanceAbilityAdapter` type-based mapping and generated ability rows. Change code only if migration actually requires it.
7. Update affected assertions and migration coverage; inspect imported/saved assets and Unity diagnostics.

- Id 26's incorrectly typed weapon settings must be gone before automatic type-based eligibility is enabled.
- Old saves must pass both `BattleSaveValidator.ValidateAbilities` and `UnitSaveService.RestoreAbilities`, which compare slot IDs/order.
- Audit migration collisions explicitly; do not silently discard two distinct saved slots if an unexpected old record contains multiple merged ids.

### 2. Pure decision rules and tuning data

- Proposed new pure C# rule model under `Services/ShipAbilities`: snapshot in → None/Shields/Weapons out; no Unity types.
- Separate designer tuning from the user's global toggle. Store all decision thresholds in one settings owner.
- Initial values below are proposed tuning defaults, not measured balance results.

| Parameter | Initial proposal |
|---|---|
| Decision interval | 0.5 s simulation time |
| Casters per frame | 4, matching existing enemy controller batching |
| Shield-loss window | 3 s |
| Heavy shield loss | ≥25% current maximum shields within window |
| Shield trigger level | <60% |
| Last-stand hull | <35% |
| Shields down | current shields ≤0 |
| Confirmation | 2 consecutive completed checks |
| Post-manual-action delay | 2 s before another automatic start |

- Shield decision: positive MaxShields, recoverable shield system, heavy recent loss and low shields.
- Skip shield boost when generators are lost, shields are suppressed, or base regeneration is zero; multiplier alone cannot repair those conditions.
- Zero current shields remains eligible when regeneration can resume. Inspect shield-recovery timing before spending a 15 s boost during a recovery lockout.
- Maintain a bounded timestamped sample buffer; sum positive drops as the initial sampling approximation. Do not claim exact damage measurement.
- Reset samples when maximum shields change, suppression begins/ends, ship spawns/loads, or monitoring resumes after a long gap. Suppression/initialization is not damage.
- Weapons good-attack: visible living uncloaked hostile within actual range of a live applicable main weapon; meaningful enemy force, not one fighter.
- Weapons last-stand: low hull, shields down, meaningful visible enemy force locally stronger than nearby allied force; caster counts among friendlies.
- Priority: last-stand Weapons → Shields → normal Weapons → None. Heavy shield loss blocks normal Weapons.
- No auto-start while any ability on this ship is active; this conservatively prevents overlap with manual effects without tracking activation origin.
- If the chosen slot is unavailable/cooling down, do nothing. Do not fall through into a conflicting lower-priority power ability.
- Before implementing strength thresholds, define their unit and reuse suitable combat-profile estimates. Proposed comparison: local hostile/own force ratio ≥1.5 for last stand; minimum force threshold requires calibration against fighter/corvette profiles.
- Keep optional attack-order gating out of the first implementation unless needed after observation; actual engagement eligibility is the primary signal.

### 3. Runtime service and entity integration

1. Add a player-owned auto-power service, registered in the battle lifetime scope with explicit dependencies.
2. Resolve only living eligible ships owned by the local human player; do not infer ships from ability settings alone.
3. Snapshot entities and hostile positions once per decision cycle; process a bounded caster batch each frame.
4. Finish the current batch before replacing it; use timestamps so a large fleet cannot starve later casters or distort the shield window.
5. Recheck registration, ownership, death, visibility, current range, settings, selection, A flag and slot readiness immediately before activation.
6. Call `ShipAbilityService.TryActivate(caster, id, null)` for automatic start. Do not call `Press` as a blind toggle, because Press can cancel an active slot.
7. Observe explicit manual ability intent to suppress same-frame auto start and apply the proposed short delay; no cooldown changes.
8. Remove per-ship samples/confirmation state on death/despawn; clear on battle disposal/load. Respect pause, battle start/end and simulation ability settings.

- Use `IEntity`/`IEntityLocator` and narrow facades across entity boundaries. Ship alone wires its own components.
- Add a read-only live-weapon eligibility query through a facade; do not query components externally or use GetComponent.
- Existing cached `WeaponModel.MaxWeaponRange` and class ranges describe the initialized loadout, not surviving mounts.
- Query live main mounts, target-class compatibility, current modifiers and actual weapon-to-target distance; exclude destroyed/strikecraft-only mounts where inappropriate.
- Reuse local combat profiles where suitable; do not copy enemy strategic remembered-intel behavior or run whole-world force scans per caster.

### 4. Global settings

1. Add `GameplaySettingsData` and integrate it into `SettingsData` construction, sanitize, equality, clone and JSON behavior.
2. Old settings without gameplay data → default on; explicit false remains false.
3. Wire the existing settings draft/editor/presenter/model route and add a Gameplay tab with one toggle.
4. Apply changes through existing Apply/Discard semantics, effective without restart. Do not let an uncommitted draft change gameplay.
5. Handle combined display-confirmation rollback consistently; preserve the setting through repository save/load.
6. Global off blocks new starts immediately after applying; running abilities and per-ship preferences remain unchanged.

### 5. Per-ship preference and saves

- Store one canonical per-ship preference; expose it through an entity facade and initialize it to on.
- Capture with the unit/ability save owner; avoid duplicate storage in both ShipSaveFacade and UnitSaveService.
- Use a backward-compatible serialized representation, e.g. `autoPowerDisabled` defaults false when absent; verify actual serializer behavior.
- Restore preference before auto ticks; restart sampling with no pre-load history.
- Keep existing `AbilitySlotSave` behavior: an active effect saves as Recovering for remaining active duration + full cooldown; no active effect resumes.
- No activation-origin field or refund eligibility is needed under the clarified normal lifecycle.
- Round-trip on/off, old missing-field defaults, shield-id migration, ready/recovering slots and mixed ships/squadrons.

### 6. Ship UI and interruption

- Automatic start must update the existing ability icon immediately: active highlight, remaining duration, progress and subsequent cooldown.
- Selecting a ship after auto activation must display its current slot state without waiting for another start event.
- A brief activation flash is optional polish; it cannot replace the persistent active/timer display.
- Keep the ability button's ordinary cancellation behavior: cancellable active abilities can be stopped; normal recovery then starts.
- Existing non-cancellable definitions remain non-cancellable; e.g. Overcharge Weapons id 38 currently has CanCancel=false. Do not silently introduce auto-only cancellation exceptions.
- Single-selection UI is the established interruption path. Group buttons currently pass `allowCancel: false`; preserve that behavior unless a separate group-cancel change is explicitly chosen.
- Add A controls to single and grouped ship layouts, including mixed state, global-disabled explanation and individual-selection suspension.
- No mandatory AUTO provenance model/badge: A displays automation preference; existing icon state displays the running ability.
- Global off must not disable ordinary ability buttons or hide their running/cooldown state.
- Read `UI_UX_GUIDELINES` and `UI_CODE_BUILD_GUIDE` before UI edits; use serialized references, existing button styles and tooltips.

## Files

| Area | Existing integration points |
|---|---|
| Lifecycle | `Assets/Scripts/Services/ShipAbilities/ShipAbilityService.cs`; `Assets/Scripts/Entities/Ship/Abilities/ShipAbilitySlot.cs` |
| Batching reference | `Assets/Scripts/Services/Enemy/EnemyShipAbilityController.cs` |
| Weapon/health observations | `Assets/Scripts/Components/Weapon/WeaponComponent.cs`; `Assets/Scripts/Components/Health/HealthComponent.cs`; `HealthModel.cs` |
| Local force estimates | `Assets/Scripts/Services/Enemy/ForceCompositionBuilder.cs`; `UnitCombatProfileCatalog.cs`; `Assets/Scripts/Entities/EnemyFaction/Models/Combat/CombatMatchup.cs` |
| Settings | `Assets/Scripts/Services/Settings/SettingsData.cs`; `SettingsService.cs`; settings UI draft/editor/presenter route |
| Save | `Assets/Scripts/Services/Saves/UnitSave.cs`; `UnitSaveService.cs`; `AbilitySlotSave.cs`; `BattleSaveValidator.cs`; `BattleSave.cs` |
| Ship UI | `Assets/Scripts/Entities/ShipUi/ShipUiController.cs`; `ShipAbilityBarUi.cs`; `ShipAbilityButtonUi.cs`; `ShipSelectionGroupUi.cs` |
| Tooltip | `Assets/Scripts/Entities/Tooltip/ShipAbilityTooltipContent.cs` |
| Prefabs | `Assets/Prefabs/Ui/Ship/ShipUi.prefab`; `ShipGroupUi.prefab`; `ShipAbilityButtonUi.prefab`; `Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab` |
| Catalog/audio/balance | `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset`; `Assets/Settings/Data/Models/Audio/ShipSfxData.asset`; `Assets/Scripts/Editor/Balance/BalanceAbilityAdapter.cs` |

### Execution evidence — 2026-10-30

- Force metric: live profile hull/piercing DPS against caster class; meaningful engaged force ≥ half a Corellian Corvette. A lone strikecraft entity is excluded explicitly.
- Verified reference thresholds (DPS): Corvette `32.4`, Frigate `17.1`, Cruiser `12.285`, Capital `9`, HeavyCapital `7.2`. One TIE fighter squadron rates `21 / 11.822 / 7.583 / 4.667 / 3.111` respectively.
- Local candidate grid: `512`-unit cells; snapshot once per cycle, query nearby cells per caster. Actual registration/position/visibility/range rechecked before use.
- Player automation runs after UI input through `IPostTickable`; manual intent wins within the frame. BattleSimulation omits this service.
- Shield recovery lockout blocks shield power; health/suppression revisions reset samples even when suppression begins and ends between checks.
- Prefab builders saved Gameplay and A controls through Unity APIs; nine migrated data assets refreshed, reserialized and saved. Catalog tuning uses camelCase serialized fields.
- Gameplay preview: `1920×1080`, no tab overlap, toggle label one line/no overflow. In-game acceptance remains user-owned.
- Final Abilities category: `142/142` passed. Initial focused passes: Core `128`, Ui `232`, Ships `138`, GameFlow `156`, Audio `137`, BattleInput `207`; six real runtime cases passed.
- Final full EditMode run: `2,550` tests, `2,542` passed, `8` failed. Complete cached root tree confirmed all `2,550` leaf results; short CLI output omitted the early enemy-AI failure after reload.
- All feature regressions pass. Final full run includes the updated Balance shared/aliased-settings fixture, real runtime tests, post-input decisions, spatial snapshots and immediate lifecycle-only UI notifications.
- Eight unrelated failures: OriginalValuesRebalanceTests Arquitens hull (`2,900` expected / `2,500` current), Recusant shields (`5,000` / `3,000`); five WeaponTaxonomyTests Executor baseline-damage cases; EnemyDefenseStrategyTests Capital-heavy choice (`MassDriverSatellite` expected / `MissileSatellite` current). Relevant source/data match unchanged HEAD.
- Earlier EditMode UI test cleanup failures were corrected by explicit lifecycle invocation; no production null fallback was added. Full run repeated after the fix.
- Unity assets are imported/saved; no feature compile/import/serialization errors. MainMenuScene remains open, saved and clean. Full-suite expected error-path logs remain in Console.
- Do not change unrelated balance/AI data to make the eight pre-existing assertions pass as part of this feature.
- Reusable guidance updated → [[GameDesign/Abilities_And_Superweapons#Automatic player ship powers]].

### Independent source verification — 2026-10-30

- Outcome: no actionable implementation defect found in the current working tree; retain `done` / `acceptance: pending-user`.
- Runtime: reviewed rule priority, confirmation/sampling resets, four-caster batching, manual delay, individual-selection pause, normal `TryActivate`, live weapon range/modifiers, shield regeneration gates and battle-scope registration.
- Settings/UI: verified committed-settings ownership, defaults/clone/equality, Apply/Discard/display rollback, single/group A logic, lifecycle notifications and serialized prefab references.
- Migration/save: verified all seven legacy IDs, collision rejection, slot order, unchanged cooldowns, per-ship defaults/restoration, 36 unique catalog keys and 35 unique SFX keys.
- Non-test checks: Serena reports no errors/warnings for `PlayerAutoPowerService`, `AutoPowerCombatSnapshot`, `AutoPowerMonitor`; scoped `git diff --check` passed.
- Tests, Play Mode, fresh Unity compilation/import checks and visual acceptance were not run in this review. Earlier test counts above remain historical evidence; the current checkout test override applies.
- Existing battle/balance/UI gates remain in [[TODOs/User_Acceptance_Queue]]. No new reusable guidance: this review confirms the existing [[GameDesign/Abilities_And_Superweapons#Automatic player ship powers]] rules.

## Edge Cases

- An auto activation consumes the normal ability lifecycle; a subsequent manual click does not bypass cooldown.
- A manual cancellation must not cause immediate automatic reactivation; readiness plus the short manual-action delay govern new starts.
- An enemy visible at snapshot time can become hidden/die before its caster batch; revalidate before spending the ability.
- All useful main weapons destroyed → no weapon boost even if cached range remains large.
- Shield suppression or maximum-shield changes → reset sampling, not heavy-damage trigger.
- Large fleets → bounded batches, one shared candidate snapshot per cycle, no per-frame full-world scans.
- No special refund/cancellation APIs, automatic replacement of running powers, expanded enemy AI or new squadron automation in this scope.

## TODO

- [x] Read plan and inspect live lifecycle, UI, settings, saves, batching, health and weapon range.
- [x] Resolve id 20 ship/squadron scope; leave ids 24/35 excluded.
- [x] Record latest user clarification: ordinary ability lifecycle, no special refund system.
- [x] Finalize meaningful-force metric/calibration and shield-recovery eligibility before implementing decision rules.
- [x] Merge shield definitions, assets, SFX and old-save migration.
- [x] Implement pure rules, bounded sampling, runtime service and narrow entity observations.
- [x] Implement Gameplay settings and per-ship save preference.
- [x] Implement A controls and verify ordinary ability UI feedback/interruption.
- [x] Add focused regression coverage; source/compile/import verification.
- User battle/balance/UI acceptance → [[TODOs/User_Acceptance_Queue]].

### Verification

- Planned categories: Abilities, Ships, Ui, GameFlow, Balance; Audio for SFX assertions, Core if battle-scope registration changes.
- Rules: exact thresholds, confirmation reset, last-stand priority, no-shield/generator loss/suppression, no visible enemies, one fighter, destroyed mounts, paused/manual/selected states.
- Lifecycle: automatic start follows ordinary duration/recovery; manual cancellation follows ordinary cooldown; cooling-down/active slots are never auto-started.
- UI: active icon/time appears on automatic start and later selection; cancellable single-ship ability can be interrupted; group/mixed A toggles preserve ineligible ships.
- Settings/save: old defaults, explicit off persistence, Apply/Discard, all seven legacy IDs, unchanged saved cooldowns and active→recovering conversion.
- Catalog: no stale removed-id references, unique SFX keys, surviving catalog types, id 26 no longer classified as weapon boost.
- Existing fixtures to review: ProcursatorShipTests, TartanTests, FactionAudioAssetTests, ShipSfxDataCoverageTests, ShipAbilitySymmetryTests, SettingsDataTests, SettingsServiceTests, SettingsDraftEditorTests, SettingsUiPrefabTests, JsonSettingsRepositoryTests.
- User authorized relevant tests and subagents for execution on 2026-10-30. Focused and full-suite runs are recorded above; do not interpret unrelated baseline failures as a passing full suite.
- After serialized asset edits: Refresh, reserialize only changed paths, SaveAssets and inspect Console. No asset changes performed during this planning task.
- Closed 2026-10-30: implementation, source/import verification and relevant regression coverage complete. User-only battle/balance/UI gates → [[TODOs/User_Acceptance_Queue]].
