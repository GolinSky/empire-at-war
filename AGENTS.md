# Shared AI Agent Instructions

These instructions apply to every AI coding agent working in this repository, including Codex, Claude, Gemini, and Antigravity agents.

## Final Response

- End each task with a short, plain-language summary of what was done so the user can review it quickly. Include verification results or blockers only when relevant.

## Authority and Configuration Ownership

Apply project guidance in this order:

1. The active user request and system/developer instructions.
2. `AGENTS.md` for repository-wide policy and routing.
3. Live source, serialized assets, and current tool output.
4. Advisory vault notes and generated local artifacts.

- `.codex/config.toml` is the canonical Codex MCP and multi-agent defaults file.
- `.codex/agents-disabled/` stores inactive project-specific subagent roles. Built-in generic agents remain available.
- `.agents/mcp_config.json`, `.antigravity/mcp_config.json`, and `.gemini/mcp_config.json` mirror local MCP access for their respective agents.
- `graphify-out/` is generated local state and is never authoritative documentation.

## Penpot Project Scope

- Penpot work for this repository is restricted to the default project in the `EmpireAtWar` workspace. The verified workspace/project IDs and link are recorded in `Tools/Penpot/README.md` under `Allowed project`.
- Before reading or changing designs, verify that the connected file belongs to that exact workspace and project. Names, the default selection, and a working MCP token do not establish project membership.
- Match `penpot.currentFile.id` to the verified file before each mutation; reverify membership when the connected file changes. The Plugin API's current file name and ID alone do not identify its parent project.
- If the connection is missing, mismatched, or unverifiable, stop design operations and request a file from the allowed project. Never fall back to another project or workspace.
- Work in another Penpot project requires an explicit user instruction changing this scope.

## Obsidian Configuration Protection

- Never edit, rotate, redact, regenerate, untrack, ignore, delete, or otherwise change Obsidian configuration, credentials, certificates, ports, plugin state, or MCP authentication unless the user explicitly requests an Obsidian configuration change in the active request.
- Audits, reviews, documentation cleanup, MCP work, and general optimization requests do not grant permission to change Obsidian configuration. Report findings and wait for an explicit request.
- The local vault MCP server is `empire-vault`, backed by `EmpireAtWarDocumentation` over stdio. Do not start Obsidian or use the retired bearer-token REST bridge.

## Obsidian Writing Flow

Write Obsidian `.md` notes as engineering reference sheets: fast scanning, high information density, concise working knowledge.

### Content and Structure

- Prefer facts, decisions, rules, constraints, values, commands, paths, APIs, code, examples, edge cases, and TODOs.
- Remove introductions, conclusions, motivational text, filler, obvious statements, repeated explanations, unnecessary background, and long prose.
- Use a `# Topic` title. Include only useful sections: `Goal`, `Rules`, `Decision`, `Implementation`, `Important Values`, `Edge Cases`, `Files`, and `TODO`.
- Keep goals to 1–3 bullets; target 3–8 bullets per section and 1–2 lines per bullet. Split large sections. Usually limit heading depth to `###`.

### Writing Rules

- Use short sentences and one fact per bullet. Prefer bullets over paragraphs.
- Use concrete values with units, limits, current/target values, and costs where known. Preserve useful numbers; never replace them with vague warnings or invent missing values.
- Preserve exact filenames, class names, APIs, commands, and paths. Use inline code for identifiers.
- Prefer code examples over explanations of code. Add only necessary lifecycle, usage, or constraint context.
- Use `X → Y` for simple relationships, event chains, and edge-case outcomes.
- Use numbered steps for procedures.
- Show decisions explicitly: chosen option, `Why`, and `Avoid`. Separate facts/rules from reasons and solutions.

### Editing and Compression Pass

1. Preserve useful facts, important numbers, constraints, code, APIs, and file names.
2. Remove duplicate prose and compress long explanations.
3. Add missing edge cases supported by the source or verified behavior.
4. Ask of every sentence: “Does this contain information I may need later?” Delete it if not.
5. Convert paragraphs → bullets, sentences → values, explanations → code, and simple relationships → `A → B` where clearer.
6. Check for repetition and missing important numbers, filenames, classes, or APIs.
7. Keep short notes short; do not expand them into long documents.

When detailed research is necessary, separate it:

- `Topic.md` — concise working knowledge; keep it easy to scan.
- `Topic - Research.md` — detailed explanations and sources.

## Obsidian Plan Lifecycle

Plan maintenance is part of the task. When creating, executing, or completing a vault plan, update its note and backlog entry through `empire-vault` before the final response; do not wait for a separate cleanup request.

### Categories and Locations

- Use one primary category: `Features`, `Bugs`, `Refactoring`, `Optimization`, `Tooling`, or `Documentation`. Choose by the plan's main outcome; preserve its filename.
- Active plans live in `TODOs/<Category>/`; completed plans live in `Done/<Category>/`. Example: `TODOs/Bugs/Ship_Order_Fix.md` → `Done/Bugs/Ship_Order_Fix.md`.
- Keep `<Topic> - Research.md` and any plan-specific supporting notes beside their plan. Leave shared reference notes in their existing locations.
- `TODOs/TODOs.md` remains the backlog index. Group active entries by category under `Active Tasks`; group completed entries by category under `Done`. Active sections contain only unfinished work.

### Working Flow

1. Read the plan, its research companion, and its backlog entry before implementation. Verify recorded progress against live source; vault snapshots and checked boxes alone do not prove completion.
2. When creating or touching a plan, categorize it and place it in the matching active or completed folder. Use frontmatter `category` matching the folder and `status`: `todo`, `in-progress`, `blocked`, or `done`; preserve unrelated metadata.
3. During work, check off completed steps and record remaining work, blockers, and relevant verification evidence. Keep partially completed or unverified plans active; do not mark an entire plan done because one phase is finished.
4. Mark a plan `done` only when all agreed scope is complete and applicable verification is satisfied. Record `completed` as `YYYY-MM-DD` using the user's local date, plus a concise outcome and verification result. Follow the existing prohibition on automated tests unless explicitly requested; never claim unrun tests passed.
5. Before the final response, move the completed plan and its companions to `Done/<Category>/` using the vault MCP move tool. Move the entire backlog entry, including nested details, from the active section to `Done`; retain its checked checkbox and update its plan/research links. Do not leave an active duplicate or delete the completed record.
6. Search for links to moved paths and repair affected backlog, dashboard, and note links; do not assume a move rewrites links automatically. Read back the moved notes and index, confirm the old paths are absent, and verify affected links resolve.

### Edge Cases

- If work is reopened, move the plan and companions back to `TODOs/<Category>/`, restore an unchecked active backlog entry, update status, and remove the current `completed` field; preserve useful completion history in the note.
- Cancelled or superseded work is not completed work. Record that disposition explicitly and remove it from active tasks without claiming success.
- If completion is uncertain, keep the plan active and record what must be verified. Only reconcile plans related to the current task unless the user requests a broader backlog cleanup.
- If a vault write or move fails, report the exact unfinished maintenance in the final response. Never claim the plan was archived when only its checkbox or status changed.

## Unity Tooling

- Use Unity's official `unity` CLI command; do not call `unity-cli` or `unity-mcp-cli`.
- Use direct `unity` commands for project, Editor, build, package, and diagnostic operations.
- For interactive Editor automation through MCP, use the official Unity CLI bridge (`unity mcp`) backed by `com.unity.pipeline`; do not use the legacy Coplay/mcp-for-unity server.

## Code Navigation: Serena

- Serena MCP is configured only for this repository in `.codex/config.toml`. Never install or register Serena in user/global Codex configuration.
- Use Serena for live C# symbol work: symbol/file overviews, definitions, callers and references, implementations, diagnostics, symbol-aware renames, and surgical symbol-body edits.
- Use built-in search/read/patch tools for non-code files, exact text searches, and small line-oriented edits. Use Unity tooling, not Serena, for scenes, prefabs, assets, Editor state, imports, serialization, and play/build operations.
- Serena uses one shared HTTP backend at `http://127.0.0.1:9122/mcp` for this exact checkout. `Tools/Serena/Start-Serena.ps1` starts it idempotently and activates `empire-at-war`; setup installs a per-user sign-in shortcut. If unavailable, run that launcher and inspect `.serena/service/` logs. Never switch the shared backend to another project or worktree; use a separate backend and endpoint for another checkout. See `Tools/Serena/README.md`.
- Do not run Serena onboarding or write Serena memories automatically. `AGENTS.md` is the source of durable agent instructions; use Serena memories only when the user explicitly requests them.

## Code Navigation and Custom Roles

- Use Serena MCP for live C# symbol work and Graphify MCP for graph-based codebase exploration. Verify graph findings against live source before making changes.
- Graphify updates automatically through the per-user `empire-at-war Graphify` task. Use the configured Graphify MCP tools: they verify freshness before reading the graph and report update failures instead of serving stale results. Setup, status, scope, and recovery are documented in `Tools/Graphify/README.md`.
- Project-specific custom subagent roles are disabled.


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

## Entity Communication

- **Between entities:** use only `IEntity`, `IEntityLocator`, and entity facades (`IEntityFacade`, resolved with `TryGetFacade` / `GetFacade`). `IEntity` holds no Unity types. Another entity's Transform comes from `IEntityTransformFacade`. A unit's own Transform is injected with `EntityBindType.ViewTransform`.
- **Inside an entity:** the entity class (for example `Ship` or `Squadron`) is the only object that connects its components.
  - The entity calls methods on its components' interfaces.
  - Components report to the entity through C# events or read-only observer interfaces.
  - A component may read a sibling through a narrow read-only interface, such as `IWeaponFacing` or `IRadarModelObserver`, but never commands it.
  - Components never use `SetMediator`-style callbacks and never inject their owning entity.
- **Ship state:** only `ShipOrderRunner` changes ship state. States report `IsComplete` and never switch states themselves.

## Unity Constraints & Error Handling

- Use PascalCase for types, methods, properties, and public members.
- Use `_camelCase` for private fields.
- **Constant Field Naming Style:** All `const` fields MUST use `UPPER_SNAKE_CASE` (e.g. `const string PREFAB_FOLDER = "...";`, `const float TWEEN_DURATION = 0.1f;` — all uppercase with underscores between words). Never use `PascalCase` or `camelCase` for `const` fields.
- **Explicit Component Binding:** Avoid using `GetComponent`, `GetComponentsInChildren`, `GetComponentInParent`, `Find`, or `FindObjectOfType`. Use explicit `[SerializeField]` fields assigned through the Inspector or explicit dependency injection instead of implicit component searching.
- **Avoid Silent Null References:** Never swallow null references, return silent dummy fallbacks, or mask missing mandatory dependencies using null-conditional operators (`?.`). Verify required UI setup and Inspector references in tests instead of adding routine null guards to production UI initialization.
- **No Constructor Null Guards:** Assign injected constructor dependencies directly (e.g. `_gameModel = gameModel;`). Never add `throw new ArgumentNullException` in constructors, including `?? throw` assignments or explicit `if` checks. Do not substitute `ArgumentNullException.ThrowIfNull`, guard helpers, assertions, or other exceptions for these constructor null guards.
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

## UI/UX Recipe Manual

- `Assets/ThirdParty/MPUIKit` is optional; use it when needed. No mandatory conversion to MPUIKit.

When creating, modifying, or refactoring any UI prefab, component, or view:
- AI agents must read the **`UI_UX_GUIDELINES`** note in the Obsidian Vault in full on demand via the **Obsidian MCP** tool (`vault_read` or `read_note` / `search_notes` for note `"UI_UX_GUIDELINES"`) before touching UI code or prefabs.
- For new UI features, AI agents must also read the **`UI_CODE_BUILD_GUIDE`** note in the Obsidian Vault before writing code or creating prefabs.

## Code Simplicity

- Keep one source of truth for identifiers. Do not pass duplicate string names alongside typed, hashed, or otherwise canonical identifiers solely for validation or error messages.
- Do not wrap direct framework calls in helpers that only pre-check state and throw. Call the framework API directly and let required-state failures surface naturally.
- Add conditions and validation only when they change required behavior or are explicitly requested; do not add routine defensive checks around straightforward code.

## Serialized Field Naming

- Unity `[SerializeField]` fields use unprefixed `camelCase` so serialized property names remain stable.
- Preserve `[Header]` Inspector groups and their member order, including serialized auto-properties. Header grouping takes precedence over type-based field ordering.

## C# File Organization

- Define one top-level type per C# script and name the file exactly after that type. Do not group multiple classes or interfaces in a differently named `*Contracts.cs` file.

## Unity Test Safety

These rules apply only when the user explicitly requests automated test execution.

1. Run `unity command list_open_scenes --json` or `unity command assert_test_ready --json` before any Unity test command.
2. Inspect every open scene. Continue only when all scenes have `isDirty=false`.
3. If a dirty scene has a non-empty asset path, save it with `save_scene` without asking (a test-run request permits this), then inspect again.
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

- **No Automated Test Execution:** Do NOT run tests (unit tests, automated test runners, etc.) unless the user explicitly requests it.
