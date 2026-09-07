# Shared AI Agent Instructions

These instructions apply to every AI coding agent working in this repository, including Codex, Claude, Gemini, and Antigravity agents.

## Authority and Configuration Ownership

Apply project guidance in this order:

1. The active user request and system/developer instructions.
2. `AGENTS.md` for repository-wide policy and routing.
3. `.codex/agents/*.toml` for one custom role's operating boundary.
4. Live source, serialized assets, and current tool output.
5. Advisory vault notes and generated local artifacts.

- `.codex/config.toml` is the canonical Codex MCP and multi-agent defaults file.
- `.codex/agents/` owns role-specific model, sandbox, and operating restrictions.
- `.agents/mcp_config.json`, `.antigravity/mcp_config.json`, and `.gemini/mcp_config.json` mirror local MCP access for their respective agents.
- `graphify-out/` is generated local state and is never authoritative documentation.

## Obsidian Configuration Protection

- Never edit, rotate, redact, regenerate, untrack, ignore, delete, or otherwise change Obsidian configuration, credentials, certificates, ports, plugin state, or MCP authentication unless the user explicitly requests an Obsidian configuration change in the active request.
- Audits, reviews, documentation cleanup, MCP work, and general optimization requests do not grant permission to change Obsidian configuration. Report findings and wait for an explicit request.
- The local vault MCP server is `empire-vault`, backed by `ObsidianDocumentation` over stdio. Do not start Obsidian or use the retired bearer-token REST bridge.

## Unity Tooling

- Use Unity's official `unity` CLI command; do not call `unity-cli` or `unity-mcp-cli`.
- Use direct `unity` commands for project, Editor, build, package, and diagnostic operations.
- For interactive Editor automation through MCP, use the official Unity CLI bridge (`unity mcp`) backed by `com.unity.pipeline`; do not use the legacy Coplay/mcp-for-unity server.

## Code Navigation: Serena

- Serena MCP is configured only for this repository in `.codex/config.toml`. Never install or register Serena in user/global Codex configuration.
- Use Serena for live C# symbol work: symbol/file overviews, definitions, callers and references, implementations, diagnostics, symbol-aware renames, and surgical symbol-body edits.
- Use built-in search/read/patch tools for non-code files, exact text searches, and small line-oriented edits. Use Unity tooling, not Serena, for scenes, prefabs, assets, Editor state, imports, serialization, and play/build operations.
- The local MCP launch auto-activates the project from the current working directory. If Serena reports that no project is active, activate `F:\Private\empire-at-war`.
- Do not run Serena onboarding or write Serena memories automatically. `AGENTS.md` is the source of durable agent instructions; use Serena memories only when the user explicitly requests them.

## Subagent Orchestration

Use the named project agents for non-trivial work when their scopes can remain independent:

1. Trivial or isolated change: the parent works directly.
2. Investigation: use `code_explorer` and, when useful, `context_curator`.
3. Architecture: use `unity_architect` only for high-risk or ambiguous design.
4. Implementation: assign exactly one writer—`csharp_worker` or `unity_operator`—for overlapping scope.
5. Review: use `unity_reviewer` after implementation when an independent review materially improves confidence.
6. Performance work: use `unity_profiler` before proposing optimization.
7. Validation: use `unity_test_runner` only when the user explicitly requested automated test execution.
8. The parent synthesizes results, resolves conflicts, and makes the final decision.

Use two to four children only when work is genuinely independent. Give every child a narrow objective, exact files or symbols, constraints, and required output. Never run overlapping writers or spawn every agent by default. Child agents must not spawn their own subagents.


## Architecture: Model-View-Presenter

Gameplay features must follow Model-View-Presenter (MVP) to keep game logic decoupled from Unity APIs.

- **Model:** Pure C# classes with no `UnityEngine` references. Own data, state, and business rules.
- **View:** `MonoBehaviour` implementations responsible only for rendering state and capturing input. Views contain no business logic.
- **Presenter:** Coordinates Model and View, subscribes to Model events, updates the View, handles user actions, and owns their lifecycle coordination.

Before implementing a new feature, provide a brief class diagram or a concise responsibility list for its Model, View, and Presenter.

##  SOLID and GRASP Standards

- **Single Responsibility:** Views handle UI and effects; Presenters handle flow; Models handle data and rules.
- **Dependency Inversion:** Presenters depend on interfaces such as `IWeaponView`, not concrete View implementations.
- **High Cohesion:** Keep code belonging to one system, such as Inventory, together in its own namespace and appropriate project folders.
- **Explicit Dependencies:** Use constructor injection for pure C# classes. Use explicit `[SerializeField]` fields assigned through the Inspector for Unity references. Avoid using `GetComponent`, `GetComponentsInChildren`, `GetComponentInParent`, or related Unity lookup APIs because they create implicit, hidden dependencies.
- Avoid god objects and oversized manager classes. Prefer focused services and ScriptableObjects for global configuration or shared data.
- If a class exceeds 200 lines, evaluate and suggest a focused refactor; do not refactor outside the requested scope without approval.

##  Preferred Design Patterns

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

## Code Simplicity

- Keep one source of truth for identifiers. Do not pass duplicate string names alongside typed, hashed, or otherwise canonical identifiers solely for validation or error messages.
- Do not wrap direct framework calls in helpers that only pre-check state and throw. Call the framework API directly and let required-state failures surface naturally.
- Add conditions and validation only when they change required behavior or are explicitly requested; do not add routine defensive checks around straightforward code.

## Serialized Field Naming

- Unity `[SerializeField]` fields use unprefixed `camelCase` so serialized property names remain stable.

## C# File Organization

- Define one top-level type per C# script and name the file exactly after that type. Do not group multiple classes or interfaces in a differently named `*Contracts.cs` file.

## Unity Test Safety

These rules apply only when the user explicitly requests automated test execution.

1. Run `unity command list_open_scenes --json` or `unity command assert_test_ready --json` before any Unity test command.
2. Inspect every open scene. Continue only when all scenes have `isDirty=false`.
3. If a dirty scene has a non-empty asset path and the task explicitly permits saving it, save it with `save_scene`, then inspect again.
4. If a dirty scene has an empty path or is untitled, stop with `BLOCKED_DIRTY_UNTITLED_SCENE`.
5. Never call `run_tests`, `open_scene`, `save_all`, enter Play Mode, or close/reload scenes while scene state is unknown or dirty.
6. Never invoke dialog-producing APIs such as `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo`, `EditorSceneManager.EnsureUntitledSceneHasBeenSaved`, `EditorUtility.DisplayDialog`, or `DisplayDialogComplex` in agent automation.
7. Start tests with `async_tests=true` and poll `test_status` until completion, failure, cancellation, or timeout.
8. If a test command times out, do not retry immediately. Inspect `test_status`, Editor status, Console, and `Editor.log`, then confirm no previous test remains active.
9. Never use `open_scene` to escape a dirty-scene condition because it may discard unsaved changes.

## Unity Asset Persistence

Unity assets must be fully imported and saved after every agent mutation. Never require the user to focus Unity, open an asset, press Ctrl+S, or manually save the project.

When a serialized Unity asset is modified directly on disk, including `.prefab`, `.unity`, `.asset`, `.mat`, `.controller`, `.anim`, or `.overrideController`:

1. Run `unity command eval --code 'UnityEditor.AssetDatabase.Refresh();'`.
2. Re-serialize only the changed asset paths with `UnityEditor.AssetDatabase.ForceReserializeAssets(...)`, then call `UnityEditor.AssetDatabase.SaveAssets()`.
3. Check the Unity console for serialization or import errors.

Do not call `ForceReserializeAssets()` without an explicit asset-path collection unless project-wide reserialization is explicitly required.

For Unity API mutations:

- Mark modified ScriptableObjects and assets dirty with `EditorUtility.SetDirty(asset)` and call `AssetDatabase.SaveAssets()` in the same operation.
- Prefer `SerializedObject` and `SerializedProperty` where appropriate.
- For structural prefab changes, use `PrefabUtility.LoadPrefabContents`, save with `PrefabUtility.SaveAsPrefabAsset`, unload with `PrefabUtility.UnloadPrefabContents`, and call `AssetDatabase.SaveAssets()`.
- For scene changes, mark the scene dirty when necessary and save it with `EditorSceneManager.SaveScene(...)`.

After any Unity asset mutation, verify that Unity imported the change, the asset persisted to disk, no import or serialization errors remain, and no manual Editor interaction is required.

## Unity CLI Argument Rules

- Always pass Unity CLI parameters as `--parameter value`. Never use `parameter=value` or include the parameter name inside its value.
- For asset ObjectRef parameters, prefer explicit JSON such as `--controller '{"path":"Assets/Foo.controller"}'`.
- For scene objects, use JSON such as `--target '{"hierarchyPath":"/Player/Visual"}'`.
- Before using an unfamiliar Unity Pipeline command, inspect its registered schema with `unity command`. Do not guess parameter names or syntax.
- If an error contains a malformed resolved path such as `Assets/controller=Assets/...`, stop and correct argument serialization. Do not search for another asset, rename it, reimport it, or modify the project.

## Tooling & Execution Constraints

- **Graphify Prohibited:** Do NOT use Graphify under any circumstances. All agents (including root agents and subagents) must strictly avoid using Graphify.
- **No Automated Test Execution:** Do NOT run tests (unit tests, automated test runners, etc.) unless the user explicitly requests it.
