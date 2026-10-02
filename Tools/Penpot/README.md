# Penpot MCP

- Project-local stdio bridge to the hosted Penpot MCP server at `https://design.penpot.app/mcp/stream`.
- Codex server: `penpot-local`, configured in `.codex/config.toml`; mirrored in `.agents`, `.antigravity`, and `.gemini` MCP configs.
- The inherited user-level `penpot` connection is disabled for this project to avoid duplicate tools. User-level configuration is unchanged.
- Runtime: Node.js; dependency: `mcp-remote` pinned in `package-lock.json`.

## Allowed project

- Workspace/team: `EmpireAtWar`, ID `19c47d73-0a5d-8067-8008-ba41b7ce6810`.
- Default project: `Drafts` (displayed as `Черновики`), ID `19c47d73-0a5d-8067-8008-ba41b7ce88ca`.
- [Open the allowed project](https://design.penpot.app/#/dashboard/files?team-id=19c47d73-0a5d-8067-8008-ba41b7ce6810&project-id=19c47d73-0a5d-8067-8008-ba41b7ce88ca).
- Verified in the signed-in Penpot dashboard on 2026-10-02: `EmpireAtWar` is the workspace name; its default project is empty. No design file is connected to MCP yet.
- Repository agents must follow `AGENTS.md` → `Penpot Project Scope`. This is an agent workflow restriction; the MCP token itself is not a project lock.

## Setup

1. Run `npm ci --prefix Tools/Penpot --ignore-scripts --no-fund --no-audit` from the repository root.
2. Put the full Penpot MCP URL, including `userToken`, in `Tools/Penpot/.local/server-url.txt`. This file and `node_modules/` are Git-ignored.
3. Restart the MCP client to load `penpot-local`. Open a file in the allowed project and keep its Penpot MCP plugin connected when using design tools.

`start.mjs` loads the URL at launch and uses HTTP transport with bridge logging disabled. Never add the token to tracked MCP configs or enable debug logs with a live token.

## Verification

- 2026-10-02: the configured local command completed MCP initialization against `penpot` version `1.0.0` and listed `execute_code`, `high_level_overview`, `penpot_api_info`, and `export_shape`.
- Connection verification did not modify any design file. Editing still requires a connected Penpot plugin.

References: [Codex MCP configuration](https://learn.chatgpt.com/docs/extend/mcp?surface=cli), [Penpot MCP](https://github.com/penpot/penpot-mcp), [MCP Remote](https://github.com/geelen/mcp-remote).
