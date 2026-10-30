---
category: Features
status: done
created: 2026-10-30
updated: 2026-10-30
completed: 2026-10-30
---
# Auto Power Abilities — Research

## Goal

- Source evidence for [[Done/Features/Auto_Power_Abilities_Plan]]; inspected 2026-10-30.
- Initial inspection below records the pre-implementation code. Execution evidence follows; implementation and verification are complete. User acceptance remains pending in the plan.
- Latest user clarification: auto simply starts the ability like a manual button press. Special refund, origin tracking and override proposals below were rejected as unnecessary; normal lifecycle/cancellation applies.

## Execution Evidence

- New files: `AutoPowerRules`, `AutoPowerMonitor`, `AutoPowerTuning`, `AutoPowerEligibility`, `AutoPowerCombatSnapshot`, `PlayerAutoPowerService`, `IShipAutoPowerFacade`, `ShipAutoPowerFacade`, `IAutoPowerWeaponObserver`.
- `WeaponComponent.CanEngageMainWeapon` checks current live non-strikecraft-only mounts, target class and actual mount range.
- `ShipAutoPowerFacade` observes health suppression/max/restore revisions, current regeneration and saved preference; `Ship` binds its weapon observer.
- `PlayerAutoPowerService.PostTick` runs after UI input, batches four casters/frame, snapshots spatial cells per cycle and calls ordinary `TryActivate`.
- `UnitSave.autoPowerDisabled` is captured/restored only through `UnitSaveService`; legacy missing bool defaults to enabled.
- `AbilitySlotSave.Id` remaps the seven old shield integers before validation/restoration; validator rejects merged-slot collisions.
- `SettingsData.Gameplay` defaults on. Consumers read `ISettingsService.Saved.Gameplay.AutoPowerAbilities`; `SavedChanged` fires after successful commit only.
- Gameplay settings/A controls are serialized in the existing prefabs; no Addressables structure changes.
- Live profile calibration: minimum engaged hull/piercing DPS vs Corvette/Frigate/Cruiser/Capital/HeavyCapital = `32.4 / 17.1 / 12.285 / 9 / 7.2`. One TIE fighter squadron = `21 / 11.822 / 7.583 / 4.667 / 3.111`; lone strikecraft is also explicitly excluded.
- Shield recovery is a hard eligibility gate; a multiplier cannot bypass recovery, suppression or destroyed generators.
- Final Abilities: `142/142` passed. Six real runtime integration cases passed. Full EditMode: `2,542/2,550` passed; eight unrelated baseline balance/AI failures. Complete cached result tree inspected after domain reload; plan records names and evidence.
- Canonical working rules → [[GameDesign/Abilities_And_Superweapons#Automatic player ship powers]].

## Implementation

### Activation, interruption and recovery

- `Services/ShipAbilities/ShipAbilityService.cs:Press` cancels one active cancellable slot for single selection; otherwise finds ready slots and activates them. No activation-source information is passed.
- `TryActivate` requires a usable ready slot, starts its effect, calls `slot.Activate`, and adds the slot to `_running`.
- `Stop` calls `slot.Recover()` and then `ability.Stop()`. Ordinary cancellation therefore starts the full recovery delay.
- `Advance` removes Ready slots from `_running`; its source comment explicitly warns that reactivation before removal adds the same slot twice.
- Investigated refund path would require effect cleanup and running-list removal before reactivation. Not needed after user clarification; keep ordinary recovery.
- `Abilities/StatModifierAbility.cs` stores one modifier handle; `Stop` removes that handle. Ordinary cancellation removes only the interrupted effect, preserving unrelated modifiers.
- `Entities/Ship/Abilities/ShipAbilitySlot.cs` has Ready/Active/Recovering, TimeLeft, RunningAbility, Changed, CanActivate, CanPress and Progress01; no origin/manual priority state.
- `CanPress` currently requires IsAvailable and either Ready or Active + CanCancel. No distinct auto-interrupt rule is planned after clarification.
- Catalog id 38 Overcharge Weapons has `canCancel: 0`. Under the clarified normal lifecycle this remains non-cancellable, regardless of who starts it.

### UI

- `Entities/ShipUi/ShipUiController.cs`: single-selection ability click uses ordinary `Press`; `AddSelectionGroups` passes `allowCancel: false`.
- A group ability button will therefore not cancel an auto-started ability without an explicit group interrupt path.
- `ShipAbilityButtonUi` already consumes slot state for active highlight, progress/time, and clickability. Automatic activation should flow through these same states.
- Existing persistent active highlight/time/progress must display auto-started effects too. A separate AUTO provenance badge is unnecessary after clarification.
- `ShipSelectionGroupUi` owns a separate ability bar. Single and group layouts both need automation state and interrupt behavior.

### Save compatibility

- `Services/Saves/AbilitySlotSave.cs:From` converts Active → Recovering with remaining active time + full recovery delay; active effects do not resume after load.
- `UnitSaveService.RestoreAbilities` requires exact slot count and positional ability IDs; `BattleSaveValidator.ValidateAbilities` also validates IDs.
- Legacy shield-id conversion must occur before validation and restoration. Remapping only inside the runtime catalog is insufficient.
- Do not shorten existing saved recovery when remapping to id 3. Slot `Restore` already supports a saved cooldown longer than the new definition delay.
- No refund/origin serialization is planned. Preserve existing active→recovering save behavior and cooldowns.

### Decision sampling and range

- `Services/Enemy/EnemyShipAbilityController.cs` processes `CASTERS_PER_TICK = 4`; snapshots casters/hostiles before activation, then rechecks entity registration and death.
- Its target eligibility rejects cloaked/invisible enemies, but self-use searches radar range. Reuse batching structure, not its trigger/range rules.
- `IShipAbilityFacade` exposes Slots, Health, Modifiers, WorldPosition, Entity and RadarRange; it does not expose live weapon engagement capability.
- `Components/Weapon/WeaponComponent.cs` constructs `WeaponModel` ranges from profiles during initialization. `AttackDistance` uses cached maximum range; `GetEngagementRange` uses cached class range × VisionMultiplier.
- Cached range is insufficient for the plan's alive-main-weapons requirement. Existing local hardpoint iteration checks `IsDestroyed`, `StrikecraftOnly`, weapon position and `MaxAttackDistance`; expose a narrow read-only query through the entity boundary.
- The auto service must not reach into another entity's components or use GetComponent. Ship wires its own component query into a facade.
- `Services/Enemy/ForceCompositionBuilder.cs` builds combat composition using current hull/shields and an offense scale; `GetOffenseScale` approximates offense by all surviving hardpoints, including systems.
- `Entities/EnemyFaction/Models/Combat/CombatMatchup.cs:Advantage` compares time-to-destroy in both directions and returns a force ratio. Reuse only after selecting visible, nearby candidates; do not import remembered enemy intel or rescan all entities per caster.
- Existing force estimates are suitable candidates for last-stand evaluation, not proof of live main-weapon range or exact firepower.

### Shields and catalog

- `HealthModel.ShieldPercentage` returns zero for MaxShields <= 0; zero percentage alone does not establish shield capability.
- `Components/Health/HealthComponent.cs` blocks regeneration during shield suppression, shield recovery and loss of all shield generators. Multiplying regeneration cannot bypass these gates.
- `HealthModel.SetShieldSuppressed` can set shields to zero without damage. A sample drop must not automatically count as combat damage.
- Sampling only old shields minus current shields can hide damage behind regeneration; proposed bounded sum of positive sample losses is still an approximation, not exact damage accounting.
- Catalog id 3: 15 s duration, 40 s recovery, cancellable, BoostShieldPowerSettings.
- Catalog id 26 is actually BoostWeaponPowerSettings despite its Power to Shields name; confirm migration before selecting automatic abilities by settings type.
- Id 20 is serialized as `14000000` in TartanShipData, RecalcitrantShipData and TIEAvengerSquadronData. Filter caster entity kind as well as settings type.
- `Editor/Balance/BalanceAbilityAdapter.cs` handles BoostShieldPowerSettings through type-based statModifier mapping; no removed shield-id names found in this file during inspection. Verify catalog enumeration and exports rather than assuming an id-specific edit is necessary.

## TODO

- UI/settings/save integration details incorporated in the execution plan.
- Refund question resolved by simplification: auto acts like an ordinary ability start; no special refund path.
- Treat thresholds and proposed UI wording as design candidates until implementation/balance acceptance.

## Files

- Settings path: `SettingsData` → `SettingsDraftEditor`/`SettingsModel` → `ISettingsPresenter`/`SettingsRouteUiController` → `SettingsUi` rows; preserve InitializeRows/DisposeRows/Render symmetry.
- `SettingsData.FromJson` constructs defaults then uses JsonUtility.FromJsonOverwrite; Clone round-trips JSON; Matches is field-by-field.
- `SettingsService.Apply` can wait for display confirmation before Commit; gameplay toggle must follow the chosen Apply/Discard/rollback contract.
- `BattleSave.FromJson` uses JsonUtility.FromJson instead; verify new per-ship boolean missing-field behavior separately from settings.
- `UnitSaveService.CaptureUnits` already captures ability slots. Prefer this save ownership for automation preference; avoid duplicate ShipSaveFacade state.
- `ShipAbilityBarUi` groups slots by ability ID; `ShipAbilityTooltipContent` summarizes ready/active slots. Preserve truthful mixed-state display.
- UI prefabs: `Assets/Prefabs/Ui/Ship/ShipUi.prefab`, `ShipGroupUi.prefab`, `ShipAbilityButtonUi.prefab`; `Assets/Prefabs/Ui/MainMenu/SettingsUi.prefab`.
- UI guidance was read by the research pass: UI_UX_GUIDELINES and UI_CODE_BUILD_GUIDE. Preserve explicit bindings and existing style; no UI mutations made.
