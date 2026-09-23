# Ship Abilities — Implementation Plan (for Codex)

## Goal

Ships get 0..N abilities that the player activates from the ship UI, for one ship or for a whole selected group. Every ability has a duration and a recovery delay. Some abilities can be turned off early, and some need a target.

- One **catalog** (a ScriptableObject that works like a database) keyed by `ShipAbilityId`.
- `ShipData` lists which ability ids a ship has. Change the list to change the ship's abilities.
- Ability logic lives **outside the ship**. `ShipAbilityService` looks up the id in the catalog, creates the matching effect object and runs it.
- The enemy AI uses the same service. Difficulty controls how quickly and how accurately it uses abilities.

**Readability comes first.** Keep the number of classes small. Build the abilities in the examples from **three reusable effect classes** configured by data, not one class per ability.

---

## 0. Before you start

- Read `AGENTS.md` and the vault notes `UI_UX_GUIDELINES` and `UI_CODE_BUILD_GUIDE` before you touch any UI.
- The working tree has an **uncommitted ShipUi refactor** (`ShipUiController`, `ShipUiModel`, `IShipUi`, `IShipGroupUi`, `ShipSelectionService`). Build on top of it. Do not revert it.
- Follow the constant naming rule (`UPPER_SNAKE_CASE`), one top-level type per file, no constructor null guards, and no `GetComponent` or `Find`.
- Do not run tests unless the user asks.

---

## 1. Abilities and the effect type behind each

| Id | Example | Effect type | Target | Can cancel | How it works |
|---|---|---|---|---|---|
| `ProtonBeam` | Venator (like Piett's ISD) | `Beam` | enemy entity | no | One burst of damage on the target plus a beam VFX for `Duration` |
| `Invulnerability` | Falcon-like | `StatModifier` | self | no | `DamageTakenMultiplier = 0` |
| `BoostShieldPower` | Nebulon-B / MC | `StatModifier` | self | **yes** | Shield regen ↑, speed ↓, damage ↓ |
| `BoostEnginePower` | Corvette | `StatModifier` | self | **yes** | Speed ↑, damage ↓ |
| `BoostWeaponPower` | Acclamator / Victory | `StatModifier` | self | **yes** | Damage ↑, speed ↓, damage taken ↑ |
| `Assault` | Admonitor-like | `StatModifier` | enemy entity | no | Fire delay ×0.5, damage ×2, and the ship is ordered to attack the target |
| `ConcentrateFire` | Home One | `CommandFocusFire` | enemy entity | no | Friendly ships in `CommandRadius` are ordered to attack the target and get the damage modifier |

Adding a new ability usually means one new enum value plus one catalog entry and no new code. Write a new effect class only when the behavior itself is new, for example healing or spawning fighters.

---

## 2. Files to create (20 in total, one type per file; 6 of them are small enums, structs or interfaces)

### 2.1 Data and catalog in `Assets/Scripts/Services/ShipAbilities/`

| File | Purpose |
|---|---|
| `ShipAbilityId.cs` | `enum`, with explicit int values that are only ever appended. `None = 0` is not allowed in data. |
| `ShipAbilityEffectType.cs` | `enum { StatModifier, Beam, CommandFocusFire }` |
| `ShipAbilityAiUse.cs` | `enum { Defensive, Escape, Offensive }`: when the enemy AI should fire the ability |
| `ShipAbilityDefinition.cs` | `[Serializable]` class holding all the data for one ability (see below) |
| `ShipAbilityCatalog.cs` | `ScriptableObject : Mvc.Data` with `DictionaryWrapper<ShipAbilityId, ShipAbilityDefinition>` and `Get(id)`. It follows the same pattern as `ShipUiData` and `ShipsData`. |

`ShipAbilityDefinition` fields are grouped with `[Header]`. The dictionary key is the id, so do **not** repeat the id inside the definition.

```csharp
[Header("Ui")]        Sprite Icon; string DisplayName;
[Header("Timing")]    float Duration; float RecoveryDelay; bool CanCancel;
[Header("Targeting")] bool RequiresEnemyTarget; float Range;
[Header("Logic")]     ShipAbilityEffectType EffectType; ShipAbilityAiUse AiUse;
[Header("Stats")]     CombatStatModifier StatModifier;      // used by StatModifier and CommandFocusFire
[Header("Beam")]      float BeamDamage; WeaponType BeamWeaponType; ProtonBeamView BeamViewPrefab;
[Header("Command")]   float CommandRadius;
```

`BeamWeaponType` reuses an existing `WeaponType`, such as `HeavyTurboLaser`, so `DamageCalculationData` already has an entry for it. **Do not add a new `WeaponType` value.**

### 2.2 Combat modifiers in `Assets/Scripts/Components/CombatModifiers/`

| File | Purpose |
|---|---|
| `CombatStatModifier.cs` | `[Serializable] struct`: `DamageMultiplier`, `FireDelayMultiplier`, `SpeedMultiplier`, `ShieldRegenMultiplier`, `DamageTakenMultiplier`. All default to 1. |
| `CombatModifiers.cs` | Pure C# class with no Unity references, one per entity. It has `Add(CombatStatModifier)` and `Remove(CombatStatModifier)`, keeps a list of active modifiers, and exposes the five multipliers as **products** of that list, recalculated on Add and Remove. It raises `event Action Changed`. |

Storing a list and recalculating avoids the float drift you get from repeatedly multiplying and dividing, and it lets several effects stack. For example, an active Shield Boost and a Concentrate Fire bonus combine correctly.

### 2.3 Per-ship runtime in `Assets/Scripts/Entities/Ship/Abilities/`

| File | Purpose |
|---|---|
| `ShipAbilityState.cs` | `enum { Ready, Active, Recovering }` |
| `ShipAbilitySlot.cs` | Runtime state of one ability on one ship: `Id`, `Definition`, `State`, `TimeLeft`, `Progress01` (for the UI fill), `event Action Changed`. The service also stores the running `IShipAbilityEffect` here. |
| `IShipAbilityCommand.cs` | `: IEntityCommand`. Exposes `IReadOnlyList<ShipAbilitySlot> Slots`, `CombatModifiers Modifiers`, `Vector3 WorldPosition`, `IEntity Entity`, `IHealthModelObserver Health`. |
| `ShipAbilityCommand.cs` | `Command<Ship>` implementation. Its constructor builds the slots from `ShipData.Abilities` and `ShipAbilityCatalog`. It is bound for **both** Player and Opponent in `ShipInstaller`. |

This follows the existing entity-command pattern (`TryGetCommand<IShipAbilityCommand>`), so the service, UI and AI reach abilities without touching `Ship` internals.

### 2.4 Execution in `Assets/Scripts/Services/ShipAbilities/`

| File | Purpose |
|---|---|
| `ShipAbilityService.cs` | The intermediate executor. It is scene-level, `ITickable`, and bound in `SkirmishMainInstaller`. |
| `IShipAbilityEffect.cs` | `void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target); void Stop();` |
| `StatModifierEffect.cs` | `Start` adds `definition.StatModifier` to the caster's modifiers. If there is a target, it also calls `IAttackCommand.Attack(target, Vector3.zero)`. `Stop` removes the modifier. |
| `ProtonBeamEffect.cs` | `Start` takes the first non-destroyed hardpoint of the target, calls `IHealthCommand.ApplyDamage(BeamDamage, BeamWeaponType, hardPointId)` and instantiates `BeamViewPrefab` from caster to target. `Stop` releases the view. |
| `ProtonBeamView.cs` | `MonoBehaviour` that only draws the beam: `Play(Vector3 from, Transform to, float duration)`. Adapt the growth and hold logic from the existing `Assets/Scripts/Components/LaserGun.cs` into this view. Leave `LaserGun` untouched. |
| `ConcentrateFireEffect.cs` | Uses `IEntityLocator` to find friendly entities within `CommandRadius` of the caster. On each one it calls `IAttackCommand.Attack(target, offset)` and adds `StatModifier` to its `CombatModifiers`. It remembers the affected ships and removes the modifier from them in `Stop`. |

`ShipAbilityService` API (keep it this small):

```csharp
public event Action TargetingChanged;
public bool IsWaitingForTarget { get; }
public ShipAbilityId PendingAbilityId { get; }

public void Press(IReadOnlyList<IEntity> casters, ShipAbilityId id); // from the UI (one ship or a group)
public void SubmitTarget(IEntity target);                            // from SelectionService
public void CancelTargeting();
public bool TryActivate(IShipAbilityCommand caster, ShipAbilityId id, IEntity target); // UI and enemy AI
public void Tick();
```

How it behaves:

- **Press**
  - If any selected slot with this id is `Active` and `CanCancel`, cancel all of them. This is the deselect/disable toggle.
  - Otherwise, if `RequiresEnemyTarget`, store the pending id and casters and raise `TargetingChanged`.
  - Otherwise, call `TryActivate` on every caster whose slot is `Ready`.
- **TryActivate**
  - Requires the slot to be `Ready` and the caster to be alive.
  - If a target is required, it must be alive, belong to the opponent and be within `Range`.
  - Then it creates the effect, calls `effect.Start(...)`, sets the slot to `Active` and sets `TimeLeft = Duration`.
- **Creating the effect** is the only mapping from logic to data. Keep it as one private `switch` and do not add a factory class:

  ```csharp
  private IShipAbilityEffect CreateEffect(ShipAbilityEffectType type) => type switch
  {
      ShipAbilityEffectType.StatModifier     => new StatModifierEffect(),
      ShipAbilityEffectType.Beam             => new ProtonBeamEffect(),
      ShipAbilityEffectType.CommandFocusFire => new ConcentrateFireEffect(_entityLocator),
      _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
  };
  ```

- **Tick** walks a `List<ShipAbilitySlot>` of slots that are `Active` or `Recovering`, and never scans every ship.
  - `Active` → `Recovering` when the time runs out: call `Stop()` and set `TimeLeft = RecoveryDelay`.
  - `Recovering` → `Ready` when the time runs out.
  - If the caster died, call `Stop()` and remove the slot from the list.
- **Cancel:** call `Stop()` and move the slot to `Recovering` with the full `RecoveryDelay`. The default is a full recovery delay; see open question Q2.

### 2.5 UI

These go in `Assets/Scripts/Entities/ShipUi/Ui/`. The prefabs are variants and the art goes under `Assets/Art/Textures/Ui/Icons/ShipAbilities/`.

| File | Purpose |
|---|---|
| `ShipAbilityBarUi.cs` | Container on a serialized button prefab. `SetSlots(IReadOnlyList<ShipAbilitySlot> slots, Action<ShipAbilityId> onPressed)` groups the slots by `Id` and shows one button per id. It reuses buttons instead of destroying them every time the selection changes. |
| `ShipAbilityButtonUi.cs` | Icon, a radial cooldown fill, an active highlight and a "waiting for target" highlight. `Update` reads its bound slots. The fill shows the lowest `Progress01`; the active highlight shows when any slot is `Active`. The button is interactable when any slot is `Ready`, or when any slot is `Active` and `CanCancel`. |

Wiring into the existing ShipUi refactor:

- Add `[SerializeField] private ShipAbilityBarUi abilityBar;` to **both** `ShipUi` and `ShipGroupUi` prefabs and scripts, so single and group selection share the same bar.
- Add `void SetAbilitySlots(IReadOnlyList<ShipAbilitySlot> slots)` to `IShipUi` and `IShipGroupUi`.
- `IShipUiPresenter.PressAbility(ShipAbilityId id)` is handled by `ShipUiController`, which calls `_abilityService.Press(context.Entities, id)`.
- In `ShipUiController.RefreshSelection()`:
  - Collect the slots of every living selected entity that has an `IShipAbilityCommand` and pass them to whichever view is visible.
  - Call `_abilityService.CancelTargeting()` whenever the selection changes.
- For the "waiting for target" highlight, `ShipUiController` subscribes to `_abilityService.TargetingChanged` and writes `PendingAbilityId` (nullable) into `ShipUiModel`. The bar reads it through `IShipUiModelObserver`. No new interface is needed.

### 2.6 Target input

These are small edits to existing files.

- `SelectionService.HandleActionInput`: if `_abilityService.IsWaitingForTarget` and the tapped entity belongs to the opponent, call `_abilityService.SubmitTarget(entity)` **instead of** `DispatchAttack`.
- `ShipSelectionService.HandleInput`: if `IsWaitingForTarget` and the tap hit empty space, call `CancelTargeting()` and return without issuing a move order.
- Neither service depends on the other, so the order in which they handle input does not matter.

### 2.7 Enemy AI in `Assets/Scripts/Services/Enemy/`

| File | Purpose |
|---|---|
| `EnemyShipAbilityController.cs` | `ITickable`, bound next to `EnemyUnitCommander`. Every `AbilityDecisionInterval` seconds it goes through opponent entities (from `IEntityLocator`) that have an `IShipAbilityCommand`, and checks each `Ready` slot. |

Rules by `AiUse`:

| AiUse | When the AI uses it |
|---|---|
| `Defensive` | `ShieldPercentage < RetreatShieldThreshold` and a player entity is within the ship's radar distance |
| `Escape` | The strategic state is Retreat, read from `IEnemyAiStateProvider.CurrentState`. Check the real enum member name. |
| `Offensive` | A player entity is within `Range` |

- The ability fires only if `Random.value < AbilityUseChance`.
- **How it picks a target:** with probability `AbilityTargetPrecision`, it takes the *best* target in range (lowest `ArmorPercentage`, preferring ships over structures). Otherwise it takes a random target in range.
- Then it calls `_abilityService.TryActivate(...)`. The AI never cancels toggleable abilities in v1.

Add three fields to `EnemyAiDifficultyProfile` and extend its constructor and all four presets:

| | Easy | Medium | Hard | UltraHard |
|---|---|---|---|---|
| `AbilityDecisionInterval` | 3.0 | 2.0 | 1.0 | 0.5 |
| `AbilityUseChance` | 0.35 | 0.6 | 0.85 | 1.0 |
| `AbilityTargetPrecision` | 0.2 | 0.5 | 0.8 | 1.0 |

---

## 3. Edits to existing code (hook points)

| File | Change |
|---|---|
| `Entities/Ship/Data/ShipData.cs` | Add `[Header("Abilities")] [SerializeField] private List<ShipAbilityId> abilities;` and `public IReadOnlyList<ShipAbilityId> Abilities => abilities;` |
| `Components/Weapon/WeaponComponent.cs` | Inject `CombatModifiers`.<br>• Line ~424: `Model.GetDamage(...) * _modifiers.DamageMultiplier`<br>• Lines ~300 and ~307: `Model.DelayBetweenAttack * _modifiers.FireDelayMultiplier` |
| `Components/Health/HealthModel.cs` | Constructor gets `CombatModifiers`. In `ApplyDamage`, multiply the incoming damage by `DamageTakenMultiplier` and return early when it is `0`. |
| `Components/Health/HealthComponent.cs` | Multiply the shield regeneration amount by `ShieldRegenMultiplier` where regen is applied. |
| `Components/Ship/Movement/ShipMoveModel.cs` | `Speed => _shipMoveData.Speed * _speedCoefficient * _modifiers.SpeedMultiplier`. **Check first:** `ShipMoveComponent` passes `Model.Speed` into `_motion.PlayPath(...)` when a path starts (line ~272). If a speed change mid-path does not take effect, subscribe to `CombatModifiers.Changed` in `ShipMoveComponent` and push the new speed into the running motion. Do not re-plan the path. |
| `ShipInstaller`, `SpaceStationInstaller`, `DefendPlatformInstaller`, `MiningFacilityInstaller` | Add `Container.Bind<CombatModifiers>().AsSingle();`. All four bind `HealthModel` or `WeaponModel`, which now need it. Stations simply keep neutral modifiers. |
| `ShipInstaller` | Add `Container.BindInterfacesExt<ShipAbilityCommand>();` for **both** `PlayerType` branches. |
| `SkirmishMainInstaller` | Bind `ShipAbilityCatalog` (`BindScriptableObject`, same as `ShipUiData`), `ShipAbilityService` and `EnemyShipAbilityController`. |
| `EnemyAiDifficultyProfile` | Add the three new fields (§2.7). |
| `SelectionService`, `ShipSelectionService` | Targeting hooks (§2.6). |
| `IShipUiPresenter`, `ShipUiController`, `IShipUi`, `ShipUi`, `IShipGroupUi`, `ShipGroupUi` | Ability bar wiring (§2.5). |

---

## 4. Assets

1. **Catalog asset:** `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset`. Register it with Addressables and the Repository the same way `ShipUiData.asset` is registered. Do not change the structure of `AddressableAssetsData`. Fill in all seven ids.
2. **Icons:** generate 7 icons at 256×256 as PNG with transparency, in one consistent style: a flat, single-color holo-console glyph with a thin outer ring, readable at 64 px on mobile. Save them to `Assets/Art/Textures/Ui/Icons/ShipAbilities/<Id>Icon.png`. Import them as `Sprite (2D and UI)` using Unity tooling and assign them in the catalog.
3. **Beam VFX prefab:** `Assets/Prefabs/Vfx/ProtonBeamView.prefab` with a `LineRenderer`, a serialized material (reuse the existing beam texture `laser-beam-effect-photoshop-free-overlay-texture.jpg`) and a hit effect.
4. **UI prefabs:** `ShipAbilityButtonUi.prefab` and a bar inside the `ShipUi` and `ShipGroupUi` prefabs. Follow `UI_UX_GUIDELINES` and MPUIKit. Minimum touch size and spacing come from that note.
5. **Ship configuration:**
   - Venator gets `ProtonBeam` and `BoostShieldPower`.
   - Assign one more ability to 2–3 other existing ship types so every effect type can be tested (including `ConcentrateFire` on the largest ship).
   - Pick sensible starting numbers, for example: beam damage ≈ 3× the heaviest single shot; Duration 8–15 s; RecoveryDelay 30–60 s.
6. After every change to a serialized asset, follow the **Unity Asset Persistence** section of `AGENTS.md`: refresh, reserialize only the changed paths, save, and check the console.

---

## 5. Tests (EditMode, write them but do not run them unless asked)

- `CombatModifiersTests`: products stack correctly, Remove restores the values exactly, and an empty list gives 1.
- `ShipAbilityServiceTests`, using a fake `IShipAbilityCommand`:
  - Ready → Active → Recovering → Ready over the right durations.
  - Cancel works only when `CanCancel`, and cancelling enters Recovering.
  - `Press` on a targeted ability puts the service into waiting for a target, and `SubmitTarget` activates it.
  - A target out of range is rejected and the slot stays Ready.
  - A dead caster stops its effect.
  - Group press activates only the Ready slots.
- `ShipAbilityCatalogTests`, on the real asset:
  - Every `ShipAbilityId` except `None` has an entry and an icon.
  - Every `ShipData.Abilities` id exists in the catalog.
  - No ship lists the same id twice.
- Extend the existing `EnemyUnitCommanderTests` style for `EnemyShipAbilityController`: a Defensive ability fires under low shields with `AbilityUseChance = 1`.

---

## 6. Implementation order (verify each step before moving on)

1. **Data:** enums, definition, catalog, `ShipData.Abilities`, catalog asset with placeholder values. → Compiles, and the catalog test passes when asked.
2. **CombatModifiers and hooks:** struct, class, the four installer bindings, and the weapon, health, move and regen hooks. → Compiles, and ships behave exactly as before because all multipliers are 1.
3. **Runtime:** slot, command, service, `StatModifierEffect`. → Bind a temporary debug key or Editor menu call to activate an ability on the selected ship. Check that speed and damage change and then revert when the duration ends.
4. **UI:** bar and button, wiring into ShipUi and ShipGroupUi, and the toggle. → Buttons appear for single and group selection, show the cooldown fill, and can be toggled off.
5. **Targeting and the other two effects:** SelectionService hooks, `ProtonBeamEffect` with its view, `ConcentrateFireEffect`. → A Venator beam hits the tapped enemy; tapping empty space cancels targeting.
6. **Enemy AI:** difficulty fields and the controller. → On UltraHard the enemy uses abilities quickly and on the weakest target; on Easy it rarely uses them.
7. **Assets:** icons, prefabs, ship assignments, and the asset persistence check.
8. **Tests:** write them.

---

## 7. Model / View / Presenter responsibilities

- **Model** (pure C#): `ShipAbilityDefinition` and `ShipAbilityCatalog` (data), `ShipAbilitySlot` (state), `CombatModifiers` (combat multipliers).
- **Presenter and flow:** `ShipAbilityService` (activation, timers, effects), `ShipUiController` (selection → slots, button press → service), `EnemyShipAbilityController` (AI decisions).
- **View:** `ShipAbilityBarUi`, `ShipAbilityButtonUi`, `ProtonBeamView`. They only render and forward input.
- **Logic units:** `StatModifierEffect`, `ProtonBeamEffect`, `ConcentrateFireEffect`. These are plain objects created by the service. `Ship` does not know about any of them.

## 8. Things not to do

- Do not create one class per ability. Stat-based abilities are data.
- Do not add a factory class, registry or DI binding per effect. The single `switch` in the service is the whole mapping.
- Do not put ability logic or timers inside `Ship`, `WeaponComponent` or `HealthComponent`. They only read `CombatModifiers`.
- Do not add a new `WeaponType` for the beam.
- Do not use `UnityEvent`, `GetComponent`, `?.` on required dependencies, or constructor null guards.

## 9. Open questions (defaults are used unless the user decides otherwise)

- **Q1: beam target out of range.** Default: the activation is rejected for those ships, targeting stays active, and nothing is consumed. The alternative is to order the ship to approach and fire once it is in range.
- **Q2: cancelling a toggleable ability.** Default: full `RecoveryDelay`. The alternative is a recovery delay proportional to the time the ability was active.
- **Q3: group press on a targeted ability.** Default: every selected ship with a Ready slot fires at the same target.
- **Q4: hardpoint targeting for the beam.** Default v1: the first living hardpoint of the target. Letting the player tap a specific hardpoint is a follow-up.
