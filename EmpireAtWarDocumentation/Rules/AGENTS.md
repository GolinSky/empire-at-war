# Shared AI Agent Instructions

- Applies to Codex, Claude, Gemini, and Antigravity agents in the repository.

## Architecture: Model-View-Presenter

- MVP is optional: use it where it fits (stateful interactive UI).
- Do not force it on entity components, hardpoints, services, or small features.

- **Model:** Pure C# classes with no `UnityEngine` references.
  - Own data, state, and business rules.
- **View:** `MonoBehaviour` implementations responsible only for rendering state and capturing input.
  - Views contain no business logic.
- **Presenter:** Coordinates Model and View, subscribes to Model events, updates the View, handles user actions, and owns their lifecycle coordination.

## SOLID and GRASP Standards

- **Single Responsibility:** Views handle UI and effects; Presenters handle flow; Models handle data and rules.
- **Dependency Inversion:** Presenters depend on interfaces such as `IWeaponView`, not concrete View implementations.
- **High Cohesion:** Keep code belonging to one system, such as Inventory, together in its own namespace and appropriate project folders.
- **Explicit Dependencies:** Use constructor injection for pure C# classes.
  - Use explicit `[SerializeField]` fields assigned through the Inspector for Unity references.
  - Avoid using `GetComponent`, `GetComponentsInChildren`, `GetComponentInParent`, or related Unity lookup APIs because they create implicit, hidden dependencies.
- Avoid god objects and oversized manager classes.
  - Prefer focused services and ScriptableObjects for global configuration or shared data.
- If a class exceeds 200 lines, evaluate and suggest a focused refactor; do not refactor outside the requested scope without approval.

## Preferred Design Patterns

- Use patterns only when justified by the feature.

- **Strategy:** Interchangeable behavior, such as distinct AI movement policies.
- **Observer:** Model-to-Presenter communication through C# events or `System.Action`; use asynchronous primitives such as UniTask only when the workflow is genuinely asynchronous.
  - Do not use `UnityEvent` in Models.
- **Factory:** Entity, prefab, or VFX creation when instantiation details should be encapsulated.

## Unity Constraints & Error Handling

- Use PascalCase for types, methods, properties, and public members.
- Use `_camelCase` for private fields.
- **Constant Field Naming Style:** All `const` fields MUST use `UPPER_SNAKE_CASE` (e.g. `const string PREFAB_FOLDER = "...";`, `const float TWEEN_DURATION = 0.1f;` — all uppercase with underscores between words).
  - Never use `PascalCase` or `camelCase` for `const` fields.
- **Explicit Component Binding:** Avoid using `GetComponent`, `GetComponentsInChildren`, `GetComponentInParent`, `Find`, or `FindObjectOfType`.
  - Use explicit `[SerializeField]` fields assigned through the Inspector or explicit dependency injection instead of implicit component searching.
- **Avoid Silent Null References:** Never swallow null references, return silent dummy fallbacks, or mask missing mandatory dependencies using null-conditional operators (`?.`).
  - Verify required UI setup and Inspector references in tests instead of adding routine null guards to production UI initialization.
- Resolve and cache Unity references during initialization, such as `Awake` or `Start`, or inject them explicitly.
- Use ScriptableObjects for shared configuration and data containers when appropriate.
- Respect Unity asset metadata: move or rename assets through Unity-aware tooling so their `.meta` files and references remain valid.
- Do not modify the structure of `Assets/AddressableAssetsData`.
- Keep `Assets/Resources` minimal and limited to bootstrap needs.

## Project Placement Summary

- Authoritative placement note: **`PROJECT_ORGANIZATION`**.
- Before creating, moving, or reorganizing asset folders: read the full note via **Obsidian MCP** (`vault_read` or `read_note` / `search_notes` for `"PROJECT_ORGANIZATION"`).

- Follow a type-first layout under `Assets`.
- Put visual source assets in `Assets/Art`.
- Put reusable GameObject configurations in `Assets/Prefabs`.
- Put project-owned C# in `Assets/Scripts`, preserving the `Components`, `Entities`, `Services`, `Editor`, and `Tests` separation.
- Put configuration and ScriptableObject data in `Assets/Settings`.
- Put major core packages in `Assets/Plugins` and other external assets in `Assets/ThirdParty`.
- Use `Assets/Sandbox` for temporary prototypes and technical tests.

## UI/UX Recipe Manual

- `Assets/ThirdParty/MPUIKit` is optional; use it when needed. No mandatory conversion to MPUIKit.

- For new UI features, read [[Rules/UI_CODE_BUILD_GUIDE]] before writing code or creating prefabs.

### UI Changes

- Before creating, modifying, or refactoring UI prefabs/components/views: read **`UI_UX_GUIDELINES`** in full via Obsidian MCP (`vault_read` or `read_note` / `search_notes` for `"UI_UX_GUIDELINES"`).

## Obsidian Writing

- Canonical policy: repository root `AGENTS.md` → Obsidian Writing Flow.
- Write concise references: one fact per bullet, concrete values, exact APIs/files, numbered procedures.
- Preserve constraints, code, and useful facts; remove duplicate prose.
- Perform a second compression pass; keep detailed sources in `Topic - Research.md`.
