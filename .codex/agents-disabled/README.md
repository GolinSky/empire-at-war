# Inactive project-specific subagent roles

Codex loads project-specific roles from `.codex/agents/*.toml`. These role files are stored here so they are not loaded. The built-in generic agent remains available through `.codex/config.toml`.

The main agent uses Serena and Graphify MCP for codebase navigation. Automated tests may run only when the user explicitly requests them.
