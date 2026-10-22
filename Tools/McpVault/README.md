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
- Node runs `Tools/McpVault/node_modules/@bitbonsai/mcpvault/dist/server.js` with `EmpireAtWarDocumentation` as the vault path.
- No global npm installation, Obsidian application, or retired REST bridge is required.

## Other local services

- Serena still connects to `http://127.0.0.1:9122/mcp`. A backend for this checkout must run on each computer; the existing Windows launcher does not provision a macOS service.
- Graphify derives its repository root from its launcher location; its Windows scheduled watcher is optional for MCP freshness checks.
