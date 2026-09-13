# Inventory and ownership

Path aliases in this report: `HOME=/Users/golinsky`, `REPO=/Users/golinsky/Projects/empire-at-war`, `SYNC=/Users/golinsky/.local/share/chezmoi`. Exact paths and hashes are in `inventory.json`.

## Instructions, settings and discovery

| Surface | Present / observed | Owner and interpretation |
|---|---|---|
| `HOME/.codex/config.toml` | 171 lines; parses; both binaries list merged MCP declarations | User + desktop-managed runtime state coexist; also managed by chezmoi |
| `HOME/.codex/AGENTS.md` | 98 lines, 4,052 bytes | General preferences; identical bytes to sync source |
| Global override | No `AGENTS.override.md` | Ordinary global instructions selected |
| `REPO/AGENTS.md` | 173 lines, 13,796 bytes | Repository policy and on-demand vault routing |
| `REPO/.codex/config.toml` | 65 lines; parses | Project MCP and agent defaults |
| `REPO/.codex/agents/*.toml` | Eight files; all parse and have required role fields | Custom project roles; named roles exposed to current session |
| Global agent directory | Absent | No personal role files found |
| Profiles / project overrides | No personal profile files or additional config/instruction files in inspected `Assets/Scripts` subtree | No profile migration justified |
| Root `CLAUDE.md` / `GEMINI.md` | Absent | No Codex fallback names configured; do not invent adapters |
| `ObsidianDocumentation/Rules/AGENTS.md` | 61 lines, 5,154 bytes | Partial duplicate plus actual nested instruction candidate for work beneath `Rules` |
| `ObsidianDocumentation/00_Home.md` and `Rules/README.md` | Read narrowly for routing/integration claims | Advisory documentation; stale integration statements found |
| `.agents`, `.antigravity`, `.gemini` MCP JSON | Three parseable tracked mirrors | Other-client loading not verified; these are not native Codex TOML layers |
| `~/.codex/rules/default.rules` | Seven explicit allow rules | User execution approvals; source template differs |
| Standalone hooks | No root, project `.codex`, or global `hooks.json` found | Plugin hooks and global `notify` still exist |

No accessible `/etc/codex/config.toml`, `/etc/codex/requirements.toml`, `/etc/codex/managed_config.toml`, checked global requirements/managed file, or checked Codex preference plist was found. Account-level constraints and launch-time overrides are not ruled out by file absence.

## Skill inventory

- Exactly one personal skill under `~/.agents/skills`: `graphify`. It is also managed by chezmoi at `SYNC/home/dot_agents/skills/graphify/SKILL.md`. Its advertised catchall codebase trigger conflicts with the project prohibition. Only its metadata was considered for this finding; Graphify was not invoked.
- Six installed system skill directories under `~/.codex/skills/.system`: `imagegen`, `openai-docs`, `plugin-creator`, `review-agent`, `skill-creator`, `skill-installer`. `review-agent` is installed but was not advertised in the parent's current skill catalog; existence does not establish selection.
- No `REPO/.agents/skills` directory and no `/etc/codex/skills` directory found.
- Current catalog also advertises agy (three workflows), Google Drive (five workflows), documents, PDF, presentations, spreadsheets/live Excel, Sites (three workflows), template creation, visualization, plugin management, deep research and a Jotform document-upload skill.
- Five system skills plus the other entries above make 27 advertised skills in the parent's supplied catalog. Large vendor descriptions are an optional review topic; no warning or failed selection demonstrated budget truncation here.
- No same-name personal-skill collision was found. Multiple cached plugin versions are installation artifacts, not evidence of duplicate active instruction loading. No cache deletion proposed.
- The OpenAI Docs and Google Drive skills were used in this audit. Other workflow bodies/scripts were not executed or exhaustively audited. No evidence justifies rewriting all vendor skills.

## MCP and connector matrix

`codex mcp list --json` was read with both installed binaries at project root, nested `Assets/Scripts`, and outside the repository. Listed means configured, not connected.

| Service | Project effective configuration | Harmless result / limitation |
|---|---|---|
| `unity` | Project `unity mcp`, enabled | Correct project/Editor ready; scene inventory succeeded; 149 Unity tools advertised in parent |
| `serena` | Project `uvx -p 3.13 --from serena-agent==1.7.0 ... --project-from-cwd`, enabled | `1.7.0`, active `empire-at-war`, language server ready; 23 tools advertised |
| `empire-vault` | Enabled, Windows executable/package/vault paths | Executable absent on Mac; no vault tools advertised; not launched |
| `graphify` | Project `enabled=false` | Not used. Tool disablement does not suppress personal skill metadata |
| `unityMCP` | Global legacy 10.0.0 entry overridden to disabled by project tombstone | Disabled at root/nested; enabled outside repository. This scoped override works |
| `context7` | Global `npx -y @upstash/context7-mcp`, API key forwarded by environment-variable name | Resolve and documentation query succeeded; no credential value inspected |
| `context-mode` | Global `context-mode` | Read-only stats responded, version `1.0.169`; no content ingestion performed |
| `deepwiki` | Global remote HTTP | Three tools advertised; no health call made |
| `node_repl` | Global app-bundled executable and runtime environment | Executable exists; three tools advertised; no browser action needed |
| `cua_repl` | Plugin-provided, enabled in both CLI registries | Direct UI tool surface also present; no UI action needed |
| `github` standalone server | Explicitly disabled | Connector-backed GitHub tools still advertised; separate integration |
| `computer-use` standalone | Disabled; relative command does not resolve from project | Dormant compatibility entry; not a current startup failure |
| `codex_app` | Registry reports disabled in standalone CLI environment | Current desktop task exposes working app tools; conversation was read successfully. Client environment matters |

The parent also received 266 hosted connector tools through `mcp__codex_apps` and 31 app coordination tools. Those are not fully explained by the user TOML. Actual provider grant enforcement was not audited by making writes. Plain configuration has no per-role allowlists for these surfaces.

## Custom role matrix

Each role sets model/effort/sandbox on lines 3–5 and `[agents] enabled=false` near its end. None contains MCP allow/deny lists. The eight names do not shadow the built-in `default`, `worker`, or `explorer` names.

| Name / file stem | Model | Effort | Declared filesystem default | Responsibility |
|---|---|---|---|---|
| `context_curator` / `context-curator` | gpt-5.6-luna | low | read-only | Narrow historical/context handoff |
| `code_explorer` / `code-explorer` | gpt-5.6-luna | medium | read-only | Execution path and impact mapping |
| `unity_architect` / `unity-architect` | gpt-5.6-sol | high | read-only | High-risk/ambiguous design |
| `csharp_worker` / `csharp-worker` | gpt-5.6-terra | high | workspace-write | One bounded C# implementation |
| `unity_operator` / `unity-operator` | gpt-5.6-terra | medium | workspace-write | Approved Editor mutation |
| `unity_profiler` / `unity-profiler` | gpt-5.6-terra | high | read-only | Performance evidence |
| `unity_reviewer` / `unity-reviewer` | gpt-5.6-terra | high | read-only | Independent correctness review |
| `unity_test_runner` / `unity-test-runner` | gpt-5.6-luna | low | read-only | Explicitly requested automated validation only |

The project permits four concurrent children, excluding the parent. The current five-slot tool environment is consistent with that. `[agents]` defaults are Terra/high; named role settings take precedence. The single audit child received unrestricted filesystem/no-escalation runtime metadata despite its read-only file default. It followed the assigned read-only scope; no boundary was probed with a write.

## Sync, hooks and package ownership

Chezmoi's managed-file inventory includes global config, global AGENTS, execution rules and the personal Graphify skill. The project `.codex` directory is separately tracked in the game repository. Guides are ordinary files beside the `home` source root.

The source template contains both durable preferences and ephemeral app-specific paths, versions, hook trust state, runtime bindings, and project trust entries. Rendering it with `chezmoi cat` proved drift without applying it. The actual Mac config contains app version `26.908.40834`; the rendered template targets `26.831.21537`.

Inspected bundled `browser` and `chrome` manifests declare lifecycle callbacks to `node_repl.turn_ended`; `unified-computer-use` declares equivalents to `cua_repl.turn_ended`. `computer-use` and `codex-app-tools` manifests declare MCP files. Global `notify` points to an existing app client. No destructive hook command was found in these inspected declarations. Repeated callback declarations may be intentional compatibility behavior; duplicate dispatch and cost were not measured. Keep plugin-owned hooks/cache untouched.

`Packages/manifest.json` contains both official `com.unity.pipeline` and legacy `com.coplaydev.unity-mcp`. Installed package presence does not prove that the retired server is active. Package removal is outside this audit's repair proposals.
