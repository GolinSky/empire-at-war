# Shared AI Agent Instructions

These instructions apply to every AI coding agent working in this repository, including Codex, Claude, Gemini, and Antigravity agents.

## Mandatory First Step

Before inspecting project files, proposing a plan, writing code, or changing any asset, read [[../Architecture/PROJECT_ORGANIZATION|PROJECT_ORGANIZATION.md]] in full. Treat its type-first asset structure, folder definitions, and placement rules as mandatory. If these instructions conflict with the organization guide, stop and ask the user which rule should take precedence.

## 1. Think Before Coding

Do not silently assume requirements or hide uncertainty. Surface assumptions, ambiguity, and meaningful tradeoffs before implementation.

- State important assumptions explicitly.
- If multiple reasonable interpretations would produce materially different results, present them instead of choosing silently.
- Mention a simpler approach when one exists and push back when complexity is not justified.
- If a critical requirement is unclear and cannot be resolved from the repository, stop and ask.
- For multi-step work, state a brief plan with a verification check for each step.

For trivial tasks, use judgment and keep this lightweight.

## 2. Goal-Driven Execution

Convert each request into verifiable success criteria and continue until they are satisfied.

- Bug fix: reproduce the problem with a test or reliable check, implement the fix, then verify it.
- Validation change: cover invalid inputs, implement validation, then run the relevant checks.
- Refactor: establish that behavior or tests pass before and after the change.
- Feature: define observable expected behavior and verify it in the narrowest reliable way.

## 3. Simplicity and Scope

Write the minimum code necessary to solve the requested problem.

- Do not add unrequested features.
- Do not introduce abstractions for a single use.
- Do not add speculative flexibility or configuration.
- Do not handle scenarios that are impossible under established project invariants.
- If a solution is substantially longer or more complex than necessary, simplify it.
- Every changed line must trace directly to the user's request or be required to keep that change correct.

## 4. Surgical Changes

Touch only what the task requires and clean up only issues introduced by the current change.

- Do not improve unrelated code, comments, or formatting.
- Do not refactor unrelated working code.
- Match the existing local style.
- Mention unrelated dead code or problems rather than deleting or fixing them without authorization.
- Remove imports, variables, functions, files, or other artifacts made unused by the current change.
- Do not remove pre-existing unused code unless requested.

## 5. Architecture: Model-View-Presenter

Gameplay features must follow Model-View-Presenter (MVP) to keep game logic decoupled from Unity APIs.

- **Model:** Pure C# classes with no `UnityEngine` references. Own data, state, and business rules.
- **View:** `MonoBehaviour` implementations responsible only for rendering state and capturing input. Views contain no business logic.
- **Presenter:** Coordinates Model and View, subscribes to Model events, updates the View, handles user actions, and owns their lifecycle coordination.

Before implementing a new feature, provide a brief class diagram or a concise responsibility list for its Model, View, and Presenter.

## 6. SOLID and GRASP Standards

- **Single Responsibility:** Views handle UI and effects; Presenters handle flow; Models handle data and rules.
- **Dependency Inversion:** Presenters depend on interfaces such as `IWeaponView`, not concrete View implementations.
- **High Cohesion:** Keep code belonging to one system, such as Inventory, together in its own namespace and appropriate project folders.
- **Explicit Dependencies:** Use constructor injection for pure C# classes. Use explicit `[SerializeField]` fields assigned through the Inspector for Unity references. Avoid using `GetComponent`, `GetComponentsInChildren`, `GetComponentInParent`, or related Unity lookup APIs because they create implicit, hidden dependencies.
- Avoid god objects and oversized manager classes. Prefer focused services and ScriptableObjects for global configuration or shared data.
- If a class exceeds 200 lines, evaluate and suggest a focused refactor; do not refactor outside the requested scope without approval.

## 7. Preferred Design Patterns

Use patterns only when their complexity is justified:

- **Strategy:** Interchangeable behavior, such as distinct AI movement policies.
- **Observer:** Model-to-Presenter communication through C# events or `System.Action`; use asynchronous primitives such as UniTask only when the workflow is genuinely asynchronous. Do not use `UnityEvent` in Models.
- **Factory:** Entity, prefab, or VFX creation when instantiation details should be encapsulated.

## 8. Unity Constraints & Error Handling

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

## 9. Project Placement Summary

The complete and authoritative placement rules are in [[../Architecture/PROJECT_ORGANIZATION|PROJECT_ORGANIZATION.md]]. At minimum:

- Follow a type-first layout under `Assets`.
- Put visual source assets in `Assets/Art`.
- Put reusable GameObject configurations in `Assets/Prefabs`.
- Put project-owned C# in `Assets/Scripts`, preserving the `Components`, `Entities`, `Services`, `Editor`, and `Tests` separation.
- Put configuration and ScriptableObject data in `Assets/Settings`.
- Put major core packages in `Assets/Plugins` and other external assets in `Assets/ThirdParty`.
- Use `Assets/Sandbox` for temporary prototypes and technical tests.
