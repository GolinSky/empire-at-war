# Implementation Plan - Obsidian Vault Integration & MCP Config

Set up an Obsidian Vault in the project (`f:\Private\empire-at-war\ObsidianDocumentation`), construct an organized note index & vault settings, and configure the local Obsidian MCP server for both **Antigravity** and **Codex**.

## User Review Required

> [!NOTE]
> - The Obsidian Vault is initialized in `f:\Private\empire-at-war\ObsidianDocumentation`.
> - The Obsidian Local REST API plugin (`obsidian-local-rest-api`) is used by `@oleksandrkucherenko/mcp-obsidian` to allow AI assistants to interact with your vault while Obsidian is running on HTTPS port 27124.
> - The API key used across Codex and Antigravity: `1c49d8b5597a92eb7b3001058807174c50fab29ebf60feaf509126440e02a9b2`.

## Proposed Changes

### 1. Vault Categorization
- `TODOs/`: Project backlog & UI Service refactoring plan
- `Architecture/`: Placement rules, MVP refactoring playbook, selection system, graphify guide
- `Rules/`: Shared AI agent rules, coding standards, project overview
- `Artifacts/`: AI implementation plans and walkthroughs

---

## Antigravity & Codex MCP Configuration

### Antigravity mcp_config.json
```json
"obsidian": {
  "command": "npx",
  "args": [
    "-y",
    "@oleksandrkucherenko/mcp-obsidian"
  ],
  "env": {
    "API_KEY": "1c49d8b5597a92eb7b3001058807174c50fab29ebf60feaf509126440e02a9b2",
    "API_URLS": "[\"https://127.0.0.1:27124/\"]"
  }
}
```
