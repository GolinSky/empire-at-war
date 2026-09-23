# Ship Abilities — Implementation Plan (for Codex)

## Goal

Ships get 0..N abilities that the player activates from the ship UI, for one ship or for a whole selected group. Every ability has a duration and a recovery delay. Some abilities can be turned off early, and some need a target.

- One **catalog** (a ScriptableObject that works like a database) keyed by `ShipAbilityId`. It holds **all numbers**.
- `ShipData` lists which ability ids a ship has. Change the list to change the ship's abilities.
- **Each ability is its own class**, a feature with its own logic. It reaches ships through entity commands and uses higher-level services when it needs them.
- Ability logic lives **outside the ship**. `ShipAbilityService` checks the id, asks `ShipAbilityFactory` for a new ability object, starts it, owns its timers and stops it.
- The enemy AI uses the same service. Difficulty controls how quickly and how accurately it uses abilities.

**Readability comes first.** Classes are small and explicit. Patterns are used only for their main idea, with simple contracts (§2), never as full textbook implementations.

---

## 0. Before you start

- Read `AGENTS.md` and the vault notes `UI_UX_GUIDELINES` and `UI_CODE_BUILD_GUIDE` before you touch any UI.
- The working tree has an **uncommitted ShipUi refactor** (`ShipUiController`, `ShipUiModel`, `IShipUi`, `IShipGroupUi`, `ShipSelectionService`). Build on top of it. Do not revert it.
- Follow the constant naming rule (`UPPER_SNAKE_CASE`), one top-level type per file, no constructor null guards, and no `GetComponent` or `Find`.
- Do not run tests unless the user asks.

---

## 1. Architecture direction

This follows the main ideas of *Clean Architecture* (Robert C. Martin), simplified, plus a few GoF and Fowler patterns. Only ideas that prevent concrete problems in this feature were chosen.

### 1.1 Chosen

| Idea | Source | How it appears here | What it prevents |
|---|---|---|---|
| **Dependency Rule** (dependencies point inward) | Clean Architecture | Three layers, as rules and folders only (no new asmdefs). **Core rules:** `ShipAbilitySlot`, `CombatModifiers`, `CombatStatModifier`, catalog data. **Use case:** `ShipAbilityService` and the ability classes. **Outer:** UI, views, the enemy AI controller, installers, the factory. Inner code never references outer code. | UI or Unity details leaking into logic; logic that can't be tested |
| **One use case, one entry point** (input port) | Clean Architecture | The player UI and the enemy AI both go through the same `ShipAbilityService` methods. There is no second way to start an ability. | Player and AI behaving differently, and rules being duplicated in two places |
| **Humble Object** | Clean Architecture, ch. 23 | `ShipAbilityBarUi`, `ShipAbilityButtonUi` and `ProtonBeamView` only draw and forward input. Every decision is in pure C# that can be tested. | Logic hidden in MonoBehaviours, and bugs that can't be tested |
| **Plugins and the Main component** | Clean Architecture, ch. 17 & 26 | Abilities are plugins. `ShipAbilityFactory` is the **only** class that knows concrete ability types; it belongs to composition, next to the installers. The service depends only on `IShipAbility` and `IShipAbilityFactory`. | The service growing with every ability; a new ability forcing edits across the codebase |
| **Command with undo** | GoF Command | `IShipAbility.Start(...)` / `Stop()`. `Stop` undoes exactly what `Start` did. | Leftover buffs: speed or damage staying changed after the ability ends |
| **Simple Factory** | GoF, creational | `ShipAbilityFactory.Create(id)` is a `switch` that calls `DiContainer.Instantiate<T>()`. Each ability gets its own services through its constructor, with no DI binding per ability. | A service constructor that keeps growing with every ability's dependencies |
| **Value Object** | Fowler, PoEAA | `CombatStatModifier` is an immutable `readonly struct` (serialized fields, read-only properties). | A shared modifier being mutated by accident |
| **Service Layer / single owner** | Fowler, PoEAA | `ShipAbilityService` is the only class that changes slot state and calls `Start` and `Stop`. | State being changed from several places, and timers counted twice |
| **Observer** | GoF (already used in the project) | `ShipAbilitySlot.Changed` and `ShipAbilityService.TargetingChanged`. Plain C# events, no `UnityEvent`. | UI polling logic state everywhere |

### 1.2 Considered and rejected (too heavy for the benefit)

| Rejected | Why |
|---|---|
| GoF **State** for slot Ready/Active/Recovering | Three states with one-line transitions. An `enum` plus a `switch` in one place is easier to read. |
| **Template Method** base class for the stat abilities | It saves a few lines but hides the logic inside a base class. Each ability should read from top to bottom in one file. |
| **Decorator** or pipeline chain for stats | A product of active multipliers does the same job with no object graph. |
| **Mediator**, event bus or messaging | Hidden control flow. Direct calls through entity commands can be traced. |
| Ability registry through **reflection or attributes** | Magic. An explicit factory `switch` is searchable and fails loudly. |
| One **asmdef per layer** | Real enforcement, but heavy for one feature. Keep the layers as rules plus a review checklist; revisit later if they are broken. |
| **Polymorphic per-ability settings** (`[SerializeReference]`) | More types and fragile serialization. A flat definition with `[Header]` groups is enough. |

### 1.3 Contracts (read these before writing any ability)

These rules are what protect against intermittent bugs.

1. **C1 One owner.** Only `ShipAbilityService` changes `ShipAbilitySlot` state and calls `IShipAbility.Start` and `Stop`. Abilities never touch slots.
2. **C2 Symmetry.** Everything an ability changes in `Start` it undoes in `Stop`. If it touched other ships, as `ConcentrateFire` does, it remembers exactly which ships and what it added.
3. **C3 `Stop` is called exactly once.** The service guarantees this in every case: the duration ends, the player cancels, the caster dies, or the scene is disposed (`ILateDisposable` stops everything still active).
4. **C4 A new instance every activation.** The factory creates a fresh object each time. Abilities have no static state and keep nothing between activations.
5. **C5 Abilities don't own time.** No coroutines, `Update` or internal timers in an ability. The service owns the duration. Views may animate visuals only.
6. **C6 Talk only through contracts.** Abilities reach entities only through entity commands (`IShipAbilityCommand`, `IAttackCommand`, `IHealthCommand`) and service interfaces (`IEntityLocator`, …). They never reference `Ship` or concrete components.
7. **C7 Numbers live in the catalog.** Ability classes contain behavior, not tuning values.
8. **C8 No validation code in the core.** Do not add checks for "is this id in the catalog", "are there duplicate ids", "is the icon missing", or `OnValidate` guards to production code. A missing catalog entry fails naturally through the dictionary lookup. Data correctness is covered by **tests** (§6).

   Gameplay rules are not validation and stay in the code: the slot is Ready, the target is alive, the target belongs to the opponent, the target is within range.
9. **C9 Components only read modifiers.** `WeaponComponent`, `HealthModel`, `HealthComponent` and `ShipMoveModel` read `CombatModifiers` and know nothing about abilities.

---

## 2. Abilities

| Id / class | Example | Target | Can cancel | What `Start` does | What `Stop` does |
|---|---|---|---|---|---|
| `ProtonBeam` / `ProtonBeamAbility` | Venator (like Piett's ISD) | enemy | no | Applies `BeamDamage` once through the target's `IHealthCommand` and plays a `ProtonBeamView` | Releases the view |
| `Invulnerability` / `InvulnerabilityAbility` | Falcon-like | self | no | Adds the modifier (`DamageTakenMultiplier = 0`) | Removes the modifier |
| `BoostShieldPower` / `BoostShieldPowerAbility` | Nebulon-B / MC | self | **yes** | Adds the modifier (shield regen ↑, speed ↓, damage ↓) | Removes the modifier |
| `BoostEnginePower` / `BoostEnginePowerAbility` | Corvette | self | **yes** | Adds the modifier (speed ↑, damage ↓) | Removes the modifier |
| `BoostWeaponPower` / `BoostWeaponPowerAbility` | Acclamator / Victory | self | **yes** | Adds the modifier (damage ↑, speed ↓, damage taken ↑) | Removes the modifier |
| `Assault` / `AssaultAbility` | Admonitor-like | enemy | no | Adds the modifier (fire delay ×0.5, damage ×2) and orders the caster to attack the target through its `IAttackCommand` | Removes the modifier |
| `ConcentrateFire` / `ConcentrateFireAbility` | Home One | enemy | no | Finds friendly ships within `CommandRadius` through `IEntityLocator`, orders each to attack the target through its `IAttackCommand` and adds the modifier to each | Removes the modifier from exactly those ships |

Adding a new ability:
1. Append a value to the `ShipAbilityId` enum.
2. Write one class that implements `IShipAbility`.
3. Add one `case` to the factory.
4. Add one catalog entry.

Nothing else changes (Open/Closed in practice).

The four stat abilities start out almost identical, and that is intended. Each one can grow independently, for example a shield-boost VFX or invulnerability that only applies while shields are up.

---

## 3. Files to create (one type per file)

The project's type-first folder layout is kept (`Components` / `Entities` / `Services`).

### 3.1 Data and catalog in `Assets/Scripts/Services/ShipAbilities/`

| File | Purpose |
|---|---|
| `ShipAbilityId.cs` | `enum` with explicit int values that are only ever appended. `None = 0` is not allowed in data. |
| `ShipAbilityAiUse.cs` | `enum { Defensive, Escape, Offensive }`: when the enemy AI should fire the ability |
| `ShipAbilityDefinition.cs` | `[Serializable]` class holding all the numbers and assets for one ability (see below) |
| `ShipAbilityCatalog.cs` | `ScriptableObject : Mvc.Data` with `DictionaryWrapper<ShipAbilityId, ShipAbilityDefinition>` and `Get(id) => dictionary[id]`. There is no validation in it (C8). |

`ShipAbilityDefinition` fields are grouped with `[Header]`. The dictionary key is the id, so do **not** repeat the id inside the definition. Each ability reads only the groups it needs.

```csharp
[Header("Ui")]        Sprite Icon; string DisplayName;
[Header("Timing")]    float Duration; float RecoveryDelay; bool CanCancel;
[Header("Targeting")] bool RequiresEnemyTarget; float Range;
[Header("Ai")]        ShipAbilityAiUse AiUse;
[Header("Stats")]     CombatStatModifier StatModifier;      // stat abilities, Assault, ConcentrateFire
[Header("Beam")]      float BeamDamage; WeaponType BeamWeaponType; ProtonBeamView BeamViewPrefab;
[Header("Command")]   float CommandRadius;
```

`BeamWeaponType` reuses an existing `WeaponType`, such as `HeavyTurboLaser`, so `DamageCalculationData` already has an entry for it. **Do not add a new `WeaponType` value.**

### 3.2 Core rules in `Assets/Scripts/Components/CombatModifiers/`

These are pure C# classes with no ability knowledge.

| File | Purpose |
|---|---|
| `CombatStatModifier.cs` | Value Object, a `[Serializable] readonly`-style struct: private serialized fields with read-only properties for `DamageMultiplier`, `FireDelayMultiplier`, `SpeedMultiplier`, `ShieldRegenMultiplier` and `DamageTakenMultiplier`. The authored default for each is 1. |
| `CombatModifiers.cs` | One per entity. `Add(CombatStatModifier)` and `Remove(CombatStatModifier)` keep a list of active modifiers, and the five multipliers are **products** of that list, recalculated on each change. It raises `event Action Changed`. |

Storing a list and recalculating avoids the float drift of repeatedly multiplying and dividing, and it lets modifiers stack correctly. Equal modifiers are interchangeable, so removing by value is safe.

### 3.3 Per-ship runtime in `Assets/Scripts/Entities/Ship/Abilities/`

| File | Purpose |
|---|---|
| `ShipAbilityState.cs` | `enum { Ready, Active, Recovering }` |
| `ShipAbilitySlot.cs` | State of one ability on one ship: `Id`, `Definition`, `State`, `TimeLeft`, `Progress01` (for the UI fill), `event Action Changed`. Only the service mutates it (C1). It also stores the running `IShipAbility`. |
| `IShipAbilityCommand.cs` | `: IEntityCommand`. Exposes `IReadOnlyList<ShipAbilitySlot> Slots`, `CombatModifiers Modifiers`, `Vector3 WorldPosition`, `IEntity Entity`, `IHealthModelObserver Health`. |
| `ShipAbilityCommand.cs` | `Command<Ship>` implementation. Its constructor builds the slots from `ShipData.Abilities` and `ShipAbilityCatalog`. It is bound for **both** Player and Opponent in `ShipInstaller`. |

### 3.4 Use case in `Assets/Scripts/Services/ShipAbilities/`

| File | Purpose |
|---|---|
| `IShipAbility.cs` | `void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target); void Stop();` Add `Tick(float deltaTime)` only when the first ability actually needs it. |
| `IShipAbilityFactory.cs` | `IShipAbility Create(ShipAbilityId id);` This interface lets service tests pass a fake factory. |
| `ShipAbilityService.cs` | The single owner and entry point. It is `ITickable` and `ILateDisposable`, and it is bound in `SkirmishMainInstaller`. |

`ShipAbilityService` API (keep it this small):

```csharp
public event Action TargetingChanged;
public bool IsWaitingForTarget { get; }
public ShipAbilityId PendingAbilityId { get; }

public void Press(IReadOnlyList<IEntity> casters, ShipAbilityId id); // player UI (one ship or a group)
public void SubmitTarget(IEntity target);                            // from SelectionService
public void CancelTargeting();
public bool TryActivate(IShipAbilityCommand caster, ShipAbilityId id, IEntity target); // UI and enemy AI
public void Tick();
public void LateDispose();                                           // Stop() every active ability (C3)
```

How it behaves:

- **Press**
  - If any selected slot with this id is `Active` and `CanCancel`, cancel all of them. This is the deselect/disable toggle.
  - Otherwise, if `RequiresEnemyTarget`, store the pending id and casters and raise `TargetingChanged`.
  - Otherwise, call `TryActivate` on every caster whose slot is `Ready`.
- **TryActivate** applies the gameplay rules:
  - The slot must be `Ready` and the caster alive.
  - If a target is required, it must be alive, belong to the opponent and be within `Range`.
  - Then it calls `_factory.Create(id)` for a fresh instance (C4), calls `Start(...)`, sets the slot to `Active` and sets `TimeLeft = Duration`.
- **Tick** walks a `List<ShipAbilitySlot>` of slots that are `Active` or `Recovering`, and never scans every ship.
  - `Active` → `Recovering` when the time runs out: call `Stop()` and set `TimeLeft = RecoveryDelay`.
  - `Recovering` → `Ready` when the time runs out.
  - If the caster died, call `Stop()` and remove the slot from the list.
- **Cancel:** call `Stop()` and move the slot to `Recovering` with the full `RecoveryDelay`.

### 3.5 Abilities (plugins) in `Assets/Scripts/Services/ShipAbilities/Abilities/`

| File | Dependencies (constructor) |
|---|---|
| `InvulnerabilityAbility.cs` | none |
| `BoostShieldPowerAbility.cs` | none |
| `BoostEnginePowerAbility.cs` | none |
| `BoostWeaponPowerAbility.cs` | none |
| `AssaultAbility.cs` | none (uses the target's and caster's entity commands) |
| `ProtonBeamAbility.cs` | none (instantiates `definition.BeamViewPrefab`) |
| `ConcentrateFireAbility.cs` | `IEntityLocator` (or a fleet-level service later) |
| `ProtonBeamView.cs` | `MonoBehaviour`, Humble Object: `Play(Vector3 from, Transform to, float duration)`. Adapt the growth and hold logic from `Assets/Scripts/Components/LaserGun.cs` into it. Leave `LaserGun` untouched. |

Target shape of a stat ability (the whole class):

```csharp
public sealed class BoostEnginePowerAbility : IShipAbility
{
    private CombatModifiers _modifiers;
    private CombatStatModifier _modifier;

    public void Start(IShipAbilityCommand caster, ShipAbilityDefinition definition, IEntity target)
    {
        _modifiers = caster.Modifiers;
        _modifier = definition.StatModifier;
        _modifiers.Add(_modifier);
    }

    public void Stop() => _modifiers.Remove(_modifier);
}
```

### 3.6 Composition in `Assets/Scripts/Services/ShipAbilities/`

| File | Purpose |
|---|---|
| `ShipAbilityFactory.cs` | Implements `IShipAbilityFactory` and is the **only** class that knows concrete abilities. It takes a `DiContainer` in its constructor; the factory is part of composition, so it may depend on the container. |

```csharp
public IShipAbility Create(ShipAbilityId id) => id switch
{
    ShipAbilityId.ProtonBeam       => _container.Instantiate<ProtonBeamAbility>(),
    ShipAbilityId.Invulnerability  => _container.Instantiate<InvulnerabilityAbility>(),
    ShipAbilityId.BoostShieldPower => _container.Instantiate<BoostShieldPowerAbility>(),
    ShipAbilityId.BoostEnginePower => _container.Instantiate<BoostEnginePowerAbility>(),
    ShipAbilityId.BoostWeaponPower => _container.Instantiate<BoostWeaponPowerAbility>(),
    ShipAbilityId.Assault          => _container.Instantiate<AssaultAbility>(),
    ShipAbilityId.ConcentrateFire  => _container.Instantiate<ConcentrateFireAbility>(),
    _ => throw new ArgumentOutOfRangeException(nameof(id), id, null)
};
```

Keep the `throw` in the last arm. It is not validation; it is the required `switch` default and it fails loudly.

### 3.7 UI

These go in `Assets/Scripts/Entities/ShipUi/Ui/`. They are Humble Objects.

| File | Purpose |
|---|---|
| `ShipAbilityBarUi.cs` | Container on a serialized button prefab. `SetSlots(IReadOnlyList<ShipAbilitySlot> slots, Action<ShipAbilityId> onPressed)` groups the slots by `Id` and shows one button per id. It reuses buttons instead of destroying them every time the selection changes. |
| `ShipAbilityButtonUi.cs` | Icon, a radial cooldown fill, an active highlight and a "waiting for target" highlight. `Update` reads its bound slots. The fill shows the lowest `Progress01`; the active highlight shows when any slot is `Active`. The button is interactable when any slot is `Ready`, or when any slot is `Active` and `CanCancel`. |

Wiring into the existing ShipUi refactor:

- Add `[SerializeField] private ShipAbilityBarUi abilityBar;` to **both** `ShipUi` and `ShipGroupUi` prefabs and scripts.
- Add `void SetAbilitySlots(IReadOnlyList<ShipAbilitySlot> slots)` to `IShipUi` and `IShipGroupUi`.
- `IShipUiPresenter.PressAbility(ShipAbilityId id)` is handled by `ShipUiController`, which calls `_abilityService.Press(context.Entities, id)`.
- In `ShipUiController.RefreshSelection()`:
  - Collect the slots of every living selected entity that has an `IShipAbilityCommand` and pass them to the visible view.
  - Call `_abilityService.CancelTargeting()` whenever the selection changes.
- For the "waiting for target" highlight, `ShipUiController` subscribes to `_abilityService.TargetingChanged` and writes `PendingAbilityId` (nullable) into `ShipUiModel`. The bar reads it through `IShipUiModelObserver`.

### 3.8 Target input

These are small edits to existing files.

- `SelectionService.HandleActionInput`: if `_abilityService.IsWaitingForTarget` and the tapped entity belongs to the opponent, call `_abilityService.SubmitTarget(entity)` **instead of** `DispatchAttack`.
- `ShipSelectionService.HandleInput`: if `IsWaitingForTarget` and the tap hit empty space, call `CancelTargeting()` and return without issuing a move order.
- Neither service depends on the other, so the order in which they handle input does not matter.

### 3.9 Enemy AI in `Assets/Scripts/Services/Enemy/`

The enemy AI is another client of the same use case (§1.1, one entry point).

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

## 4. Edits to existing code (hook points)

| File | Change |
|---|---|
| `Entities/Ship/Data/ShipData.cs` | Add `[Header("Abilities")] [SerializeField] private List<ShipAbilityId> abilities;` and `public IReadOnlyList<ShipAbilityId> Abilities => abilities;` |
| `Components/Weapon/WeaponComponent.cs` | Inject `CombatModifiers`.<br>• Line ~424: `Model.GetDamage(...) * _modifiers.DamageMultiplier`<br>• Lines ~300 and ~307: `Model.DelayBetweenAttack * _modifiers.FireDelayMultiplier` |
| `Components/Health/HealthModel.cs` | Constructor gets `CombatModifiers`. In `ApplyDamage`, multiply the incoming damage by `DamageTakenMultiplier` and return early when it is `0`. |
| `Components/Health/HealthComponent.cs` | Multiply the shield regeneration amount by `ShieldRegenMultiplier` where regen is applied. |
| `Components/Ship/Movement/ShipMoveModel.cs` | `Speed => _shipMoveData.Speed * _speedCoefficient * _modifiers.SpeedMultiplier`. **Check first:** `ShipMoveComponent` passes `Model.Speed` into `_motion.PlayPath(...)` when a path starts (line ~272). If a speed change mid-path does not take effect, subscribe to `CombatModifiers.Changed` in `ShipMoveComponent` and push the new speed into the running motion. Do not re-plan the path. |
| `ShipInstaller`, `SpaceStationInstaller`, `DefendPlatformInstaller`, `MiningFacilityInstaller` | Add `Container.Bind<CombatModifiers>().AsSingle();`. All four bind `HealthModel` or `WeaponModel`, which now need it. |
| `ShipInstaller` | Add `Container.BindInterfacesExt<ShipAbilityCommand>();` for **both** `PlayerType` branches. |
| `SkirmishMainInstaller` | Bind `ShipAbilityCatalog` (`BindScriptableObject`, same as `ShipUiData`), `ShipAbilityFactory` (as `IShipAbilityFactory`), `ShipAbilityService` and `EnemyShipAbilityController`. Do **not** add a binding per ability. |
| `EnemyAiDifficultyProfile` | Add the three new fields (§3.9). |
| `SelectionService`, `ShipSelectionService` | Targeting hooks (§3.8). |
| `IShipUiPresenter`, `ShipUiController`, `IShipUi`, `ShipUi`, `IShipGroupUi`, `ShipGroupUi` | Ability bar wiring (§3.7). |

---

## 5. Assets

1. **Catalog asset:** `Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset`. Register it with Addressables and the Repository the same way `ShipUiData.asset` is registered. Do not change the structure of `AddressableAssetsData`. Fill in all seven ids.
2. **Icons:** generate 7 icons at 256×256 as PNG with transparency, in one consistent style: a flat, single-color holo-console glyph with a thin outer ring, readable at 64 px on mobile. Save them to `Assets/Art/Textures/Ui/Icons/ShipAbilities/<Id>Icon.png`. Import them as `Sprite (2D and UI)` using Unity tooling and assign them in the catalog.
3. **Beam VFX prefab:** `Assets/Prefabs/Vfx/ProtonBeamView.prefab` with a `LineRenderer`, a serialized material (reuse `laser-beam-effect-photoshop-free-overlay-texture.jpg`) and a hit effect.
4. **UI prefabs:** `ShipAbilityButtonUi.prefab` and a bar inside the `ShipUi` and `ShipGroupUi` prefabs. Follow `UI_UX_GUIDELINES` and MPUIKit.
5. **Ship configuration:**
   - Venator gets `ProtonBeam` and `BoostShieldPower`.
   - Assign one more ability to 2–3 other existing ship types so every ability can be tested (including `ConcentrateFire` on the largest ship).
   - Starting numbers: beam damage ≈ 3× the heaviest single shot; Duration 8–15 s; RecoveryDelay 30–60 s.
6. After every change to a serialized asset, follow the **Unity Asset Persistence** section of `AGENTS.md`.

---

## 6. Tests (EditMode, write them but do not run them unless asked)

Tests replace validation code in the core (C8).

- `CombatModifiersTests`: products stack correctly, Remove restores the values exactly, and an empty list gives 1.
- `ShipAbilityServiceTests`, using a fake `IShipAbilityFactory` that returns a recording `IShipAbility` and a fake `IShipAbilityCommand`:
  - Ready → Active → Recovering → Ready over the right durations.
  - **`Stop` is called exactly once** at the end of the duration, on cancel, on caster death and on `LateDispose` (C3).
  - A **new instance is created for every activation** (C4).
  - Cancel works only when `CanCancel`, and cancelling enters Recovering.
  - `Press` on a targeted ability puts the service into waiting for a target, and `SubmitTarget` activates it.
  - A target out of range is rejected and the slot stays Ready.
  - Group press activates only the Ready slots.
- **Symmetry tests for each ability** (C2): after `Start` followed by `Stop`, the caster's (and, for `ConcentrateFire`, the allies') `CombatModifiers` equal their values before `Start`.
- `ShipAbilityFactoryTests`: every `ShipAbilityId` except `None` creates an ability. This catches a missing `case`.
- `ShipAbilityCatalogTests`, on the real asset:
  - Every id except `None` has an entry and an icon.
  - Every `ShipData.Abilities` id exists in the catalog.
  - No ship lists the same id twice.
  - Targeted abilities have `Range > 0`.
- `EnemyShipAbilityControllerTests`, in the existing `EnemyUnitCommanderTests` style: a Defensive ability fires under low shields when `AbilityUseChance = 1`.

---

## 7. Implementation order (verify each step before moving on)

1. **Data:** enums, definition, catalog, `ShipData.Abilities`, catalog asset with placeholder values. → Compiles.
2. **CombatModifiers and hooks:** struct, class, the four installer bindings, and the weapon, health, move and regen hooks. → Compiles, and ships behave exactly as before because all multipliers are 1.
3. **Use case:** slot, command, `IShipAbility`, factory interface and factory, service, `BoostEnginePowerAbility`. → Activate it from a temporary Editor menu call on the selected ship. Speed changes, then reverts when the duration ends.
4. **Remaining stat abilities:** Invulnerability, BoostShieldPower, BoostWeaponPower, Assault.
5. **UI:** bar and button, wiring into ShipUi and ShipGroupUi, and the toggle. → Buttons appear for single and group selection, show the cooldown fill, and can be toggled off.
6. **Targeting, ProtonBeam and ConcentrateFire:** SelectionService hooks, the beam ability and view, the concentrate-fire ability. → The Venator beam hits the tapped enemy; tapping empty space cancels targeting.
7. **Enemy AI:** difficulty fields and the controller.
8. **Assets:** icons, prefabs, ship assignments, and the asset persistence check.
9. **Tests:** write them. Remove the temporary Editor menu call from step 3.

---

## 8. Responsibilities

| Who | Does | Does not |
|---|---|---|
| `ShipAbilityCatalog` / `ShipAbilityDefinition` | Hold all numbers and assets | Contain logic or validation |
| `ShipAbilitySlot` | Hold the state of one ability on one ship | Change its own state |
| `CombatModifiers` | Combine the active stat multipliers | Know about abilities |
| `ShipAbilityService` | Activate, cancel and target abilities; run timers; own the ability lifecycle | Know concrete ability types |
| `ShipAbilityFactory` | Map id → new ability instance | Hold state |
| `XxxAbility` classes | Do one feature's logic in `Start` and undo it in `Stop` | Own time, touch slots or reference concrete ship components |
| `ShipUiController` | Turn the selection into slots, and a button press into `Press` | Contain ability rules |
| `EnemyShipAbilityController` | Decide when the AI uses abilities, based on difficulty | Bypass `TryActivate` |
| `ShipAbilityBarUi` / `ShipAbilityButtonUi` / `ProtonBeamView` | Draw and forward input | Make decisions |

## 9. Things not to do

- Do not merge several abilities into one generic class, even when their code is the same today.
- Do not add a DI binding per ability, a reflection or attribute registry, an event bus or a base class hierarchy for abilities.
- Do not put ability logic or timers inside `Ship`, `WeaponComponent` or `HealthComponent`. They only read `CombatModifiers`.
- Do not put tuning numbers inside ability classes.
- Do not add validation code (catalog checks, `OnValidate`, guard helpers) to core code. Write a test instead.
- Do not add a new `WeaponType` for the beam.
- Do not use `UnityEvent`, `GetComponent`, `?.` on required dependencies, or constructor null guards.

## 10. Open questions (defaults are used unless the user decides otherwise)

- **Q1: beam target out of range.** Default: the activation is rejected for those ships, targeting stays active, and nothing is consumed. The alternative is to order the ship to approach and fire once it is in range.
- **Q2: cancelling a toggleable ability.** Default: full `RecoveryDelay`. The alternative is a recovery delay proportional to the time the ability was active.
- **Q3: group press on a targeted ability.** Default: every selected ship with a Ready slot fires at the same target.
- **Q4: hardpoint targeting for the beam.** Default v1: the first living hardpoint of the target. Letting the player tap a specific hardpoint is a follow-up.
