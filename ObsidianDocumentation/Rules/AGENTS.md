# Shared AI Agent Instructions

These instructions apply to every AI coding agent working in this repository, including Codex, Claude, Gemini, and Antigravity agents.


## Architecture: Model-View-Presenter

Gameplay features must follow Model-View-Presenter (MVP) to keep game logic decoupled from Unity APIs.

- **Model:** Pure C# classes with no `UnityEngine` references. Own data, state, and business rules.
- **View:** `MonoBehaviour` implementations responsible only for rendering state and capturing input. Views contain no business logic.
- **Presenter:** Coordinates Model and View, subscribes to Model events, updates the View, handles user actions, and owns their lifecycle coordination.

Before implementing a new feature, provide a brief class diagram or a concise responsibility list for its Model, View, and Presenter.

## SOLID and GRASP Standards

- **Single Responsibility:** Views handle UI and effects; Presenters handle flow; Models handle data and rules.
- **Dependency Inversion:** Presenters depend on interfaces such as `IWeaponView`, not concrete View implementations.
- **High Cohesion:** Keep code belonging to one system, such as Inventory, together in its own namespace and appropriate project folders.
- **Explicit Dependencies:** Use constructor injection for pure C# classes. Use explicit `[SerializeField]` fields assigned through the Inspector for Unity references. Avoid using `GetComponent`, `GetComponentsInChildren`, `GetComponentInParent`, or related Unity lookup APIs because they create implicit, hidden dependencies.
- Avoid god objects and oversized manager classes. Prefer focused services and ScriptableObjects for global configuration or shared data.
- If a class exceeds 200 lines, evaluate and suggest a focused refactor; do not refactor outside the requested scope without approval.

## Preferred Design Patterns

Use patterns only when their complexity is justified:

- **Strategy:** Interchangeable behavior, such as distinct AI movement policies.
- **Observer:** Model-to-Presenter communication through C# events or `System.Action`; use asynchronous primitives such as UniTask only when the workflow is genuinely asynchronous. Do not use `UnityEvent` in Models.
- **Factory:** Entity, prefab, or VFX creation when instantiation details should be encapsulated.

## Unity Constraints & Error Handling

- Use PascalCase for types, methods, properties, and public members.
- Use `_camelCase` for private fields.
- **Constant Field Naming Style:** All `const` fields MUST use `UPPER_SNAKE_CASE` (e.g. `const string PREFAB_FOLDER = "...";`, `const float TWEEN_DURATION = 0.1f;` — all uppercase with underscores between words). Never use `PascalCase` or `camelCase` for `const` fields.
- **Explicit Component Binding:** Avoid using `GetComponent`, `GetComponentsInChildren`, `GetComponentInParent`, `Find`, or `FindObjectOfType`. Use explicit `[SerializeField]` fields assigned through the Inspector or explicit dependency injection instead of implicit component searching.
- **Avoid Silent Null References (Fail-Fast Principle):** Never swallow null references, return silent dummy fallbacks, or mask missing mandatory dependencies using null-conditional operators (`?.`). Throw explicit exceptions (e.g. `ArgumentNullException`, `InvalidOperationException`) or use assertions immediately during initialization (`Awake` / constructor) to fail fast when required references or dependencies are null.
- Resolve and cache Unity references during initialization, such as `Awake` or `Start`, or inject them explicitly.
- Use ScriptableObjects for shared configuration and data containers when appropriate.
- Respect Unity asset metadata: move or rename assets through Unity-aware tooling so their `.meta` files and references remain valid.
- Do not modify the structure of `Assets/AddressableAssetsData`.
- Keep `Assets/Resources` minimal and limited to bootstrap needs.

## Project Placement Summary

The complete and authoritative placement rules are in the **`PROJECT_ORGANIZATION`** note in the Obsidian Vault. AI agents must read it on demand via the **Obsidian MCP** tool (`vault_read` or `read_note` / `search_notes` for note `"PROJECT_ORGANIZATION"`) before creating, moving, or reorganizing project asset folders. At minimum:

- Follow a type-first layout under `Assets`.
- Put visual source assets in `Assets/Art`.
- Put reusable GameObject configurations in `Assets/Prefabs`.
- Put project-owned C# in `Assets/Scripts`, preserving the `Components`, `Entities`, `Services`, `Editor`, and `Tests` separation.
- Put configuration and ScriptableObject data in `Assets/Settings`.
- Put major core packages in `Assets/Plugins` and other external assets in `Assets/ThirdParty`.
- Use `Assets/Sandbox` for temporary prototypes and technical tests.

## UI/UX Recipe Manual & MPUIKit Standards

When creating, modifying, or refactoring any UI prefab, component, or view:
- AI agents must read the **`UI_UX_GUIDELINES`** note in the Obsidian Vault in full on demand via the **Obsidian MCP** tool (`vault_read` or `read_note` / `search_notes` for note `"UI_UX_GUIDELINES"`) before touching UI code or prefabs.
