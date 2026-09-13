# Effective loading and confidence

## Configuration scenarios

| Scenario | Observed or inferred result |
|---|---|
| Current desktop task at repo root | Global and root project instruction text supplied in the task; named project roles available; current runtime explicitly says full filesystem access and approval `never` |
| CLI 0.153.0 at repo root | Merged MCP list includes enabled Unity/Serena/vault and disabled legacy Unity/Graphify |
| App binary 0.154.0-alpha.6.2 at repo root | Same MCP registry result |
| Both binaries at `Assets/Scripts` | Same MCP registry as root; no additional instruction/config files found in the inspected subtree |
| Both binaries at `/Users/golinsky` | Project services disappear; global legacy `unityMCP` is enabled |
| Working beneath vault `Rules` | Static discovery predicts global + root + `Rules/AGENTS.md`; not exercised by a fresh model turn |
| Windows, other clients, untrusted/profile fixtures | Not run; do not generalize the Mac result |

The observed global defaults are Astra/xhigh, live web search and `approvals_reviewer="user"`. Project settings supply Unity, Serena, vault, two disabled compatibility entries and agent defaults/limits. All eight role files explicitly choose their model/effort. No named profile or local model override was found. Desktop runtime permissions are an additional layer; `approvals_reviewer="user"` identifies a reviewer when approval is requested, and does not by itself enable approval requests in this `never` session.

The instruction and configuration precedence descriptions come from [official configuration documentation](https://learn.chatgpt.com/docs/config-file/config-basic) and [AGENTS.md discovery](https://learn.chatgpt.com/docs/agent-configuration/agents-md). Registry commands substantiate server enablement; they do not expose every effective desktop, cloud, permission or model setting.

## Instruction budget and routing

Global plus root source text totals **17,848 bytes** before separators/wrappers. Adding the vault `Rules/AGENTS.md` gives **23,002 bytes**. Both are below the documented default 32 KiB budget. No custom `project_doc_max_bytes` or fallback filenames were found. There is no evidence of an instruction-size failure and no reason to shorten files just to reach 100 lines.

The root already routes UI work to `UI_UX_GUIDELINES` and asset-folder organization to `PROJECT_ORGANIZATION` on demand. Preserve those triggers. Their MCP route currently depends on the broken Mac vault registration; this audit did not modify UI or asset folders and therefore did not need to load those complete reference notes.

Vault reference notes are not universally active instructions. `Rules/AGENTS.md` can become a narrower instruction file only for its actual subtree, or influence work if explicitly read through vault navigation. Omitted rules in the partial duplicate do not erase inherited root protections.

## Distinguish the loading surfaces

1. **Instructions:** global → repository → applicable nested file; selection of overrides/fallbacks matters.
2. **Skills:** metadata advertised first, body read on invocation. The personal Graphify metadata remains advertised while its project MCP server is disabled. These controls are separate.
3. **Roles:** eight standalone files are discovered. A filename using hyphens and a `name` using underscores is valid; the role name field is authoritative.
4. **MCP:** a server can be listed as enabled yet unavailable, as with the Windows vault entry. A disabled standalone service does not disable a separate hosted connector.
5. **Rules and permissions:** execution allowlists, filesystem permissions, model instructions and remote API authorization are distinct. No source establishes that a read-only role TOML automatically removes remote writes.
6. **Hooks:** absent `hooks.json` does not mean no hooks; installed plugins and `notify` contribute lifecycle actions.

The installed system/legacy skill paths were not migrated merely because the public guide emphasizes `.agents/skills`. Current session discovery provides stronger evidence of what is installed and advertised than a guessed cleanup rule.
