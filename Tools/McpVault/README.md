# Portable MCP setup

- Shared configs: `.codex/config.toml`, `.mcp.json`, `.agents/mcp_config.json`, `.antigravity/mcp_config.json`, `.gemini/mcp_config.json`.
- Open this repository as the client workspace. Start MCP processes with the repository root as their working directory; relative arguments resolve from that directory, not the config directory.
- Commands use `node`, `uvx`, and the official `unity` CLI from PATH on Windows and macOS. Node must be version 20 or newer. The desktop client's environment must include these commands, including when launched from Finder on macOS.
- No `$project` substitution is used. If a client starts outside the repository, set its local working-directory option to this checkout; do not commit machine-specific absolute paths.

## Setup on each computer

1. From the repository root, run the launcher for your computer. Node, uv, and Unity CLI do not need to be installed first.

   Windows PowerShell:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File Tools/setup-mcp.ps1
   ```

   macOS Terminal:

   ```sh
   bash Tools/setup-mcp.sh
   ```

2. If Node is already installed, the existing command also runs the appropriate bootstrap launcher:

   ```sh
   node Tools/setup-mcp.mjs
   ```

   Setup installs missing runtimes, installs both locked Node dependency sets, and caches the configured Blender and Graphify packages. It resolves the checkout from its own location, so an absolute script path also works from another directory. Rerunning preserves working runtimes; `npm ci` replaces only the two generated `node_modules` directories. Failures stop setup with a nonzero exit code.

   Use `-Check` with the PowerShell launcher, `--check` with the macOS launcher, or `node Tools/setup-mcp.mjs --check` to check runtime availability without installing packages or changing persistent PATH settings. This does not test MCP connections or verify installed dependencies.

3. Configure the ignored local Penpot URL file as described in [Penpot setup](../Penpot/README.md). It does not travel with Git. The script reports whether the file is present without printing its contents.
4. Open this project in Unity with its Pipeline package, and start the Blender MCP add-on on `127.0.0.1:9876` when needed.
5. Fully restart the desktop client after runtime installation so it inherits PATH changes. Setup requires network access. Clearing the uv cache causes Blender and Graphify packages to download again on next use.

## Runtime installation

- A working Node 20+ with npm is reused. Otherwise, setup downloads the latest Node 24 LTS archive from `nodejs.org` and verifies it against the official SHA-256 checksums. The new runtime is installed per user; other Node installations are not removed.
- Missing uv/uvx and Unity CLI use the official Astral and Unity installers. Setup installs the CLI only; it does not install Unity Editors or Blender, start services, or configure authentication.
- Windows adds installed runtime directories to the user PATH. macOS adds the required export lines to shell profiles. The current setup process also receives the new paths immediately.
- Homebrew, WinGet, and administrator access are not prerequisites. The official Unity CLI currently requires macOS 14+ or Windows 10 21H1+.
- Sources: [Node downloads](https://nodejs.org/en/download), [uv installation](https://docs.astral.sh/uv/getting-started/installation/), [Unity CLI installation](https://docs.unity.com/en-us/unity-cli/use-unity-cli).

## Vault

- `@bitbonsai/mcpvault` stays pinned at `0.16.0`; its dependency tree is recorded in `package-lock.json`.
- Codex connects to `http://127.0.0.1:9123/mcp`; one independent Node backend serves `EmpireAtWarDocumentation` using the package's unchanged tool handlers.
- `node Tools/McpVault/start.mjs` starts the backend idempotently from any working directory. It verifies the existing listener's vault identity and refuses another checkout on port 9123. Logs: ignored `Tools/McpVault/.service/server.log`.
- Windows setup installs `empire-at-war Vault.lnk` in the current user's Startup folder. To install/start just this service: `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/McpVault/Setup-Service.ps1`.
- On macOS, run `node Tools/McpVault/start.mjs` after sign-in, before opening MCP connections. Portable setup also starts it immediately.
- The HTTP listener binds only to `127.0.0.1`, validates the Host header, and rejects browser Origin headers. No network-facing listener or Obsidian plugin is involved.
- Other clients retain their existing stdio configuration. The HTTP change targets Codex on Windows.
- No global npm installation, Obsidian application, or retired REST bridge is required.

### Startup stalls diagnosed 2026-10-29

- Direct stdio diagnostics: 12 launches completed initialization and listed 18 tools in 174–329 ms. Startup does not index vault contents.
- Codex failures coincided with unrelated MCP timeouts and `config/read` taking 300,197 ms. One actual 11:18:13 UTC vault timeout was displayed at 11:25:23 UTC.
- This matches the Windows per-chat stdio blocking-pool exhaustion described in [Codex issue #50923](https://github.com/openai/codex/issues/50923). The historical logs establish client-wide stalls; they do not contain a native thread dump proving pool exhaustion in this process.
- Shared HTTP removes empire-vault's per-chat child processes and blocking stdout/stderr pipes from Codex. Startup timeout is unchanged. Other stdio servers can still trigger Codex's broader issue.

## Other local services

- Serena still connects to `http://127.0.0.1:9122/mcp`. A backend for this checkout must run on each computer; the existing Windows launcher does not provision a macOS service.
- Graphify derives its repository root from its launcher location; its Windows scheduled watcher is optional for MCP freshness checks.
