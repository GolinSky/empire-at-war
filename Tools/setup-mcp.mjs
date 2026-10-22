import { existsSync, readFileSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const ROOT = fileURLToPath(new URL('../', import.meta.url));
const WINDOWS = process.platform === 'win32';
const CHECK_ONLY = process.argv.includes('--check');

function Run(command, args) {
  // npm is a .cmd shim on Windows. Only fixed npm arguments are passed here.
  const result = command === 'npm' && WINDOWS
    ? spawnSync('cmd.exe', ['/d', '/s', '/c', 'npm', ...args], { cwd: ROOT, stdio: 'inherit' })
    : spawnSync(command, args, { cwd: ROOT, stdio: 'inherit' });
  if (result.error) throw new Error(`${command}: ${result.error.message}`);
  if (result.status !== 0) throw new Error(`${command} failed (exit ${result.status}, signal ${result.signal}).`);
}

try {
  if (process.argv.slice(2).some(arg => arg !== '--check')) {
    throw new Error('Usage: node Tools/setup-mcp.mjs [--check]');
  }
  if (!CHECK_ONLY && process.env.EAW_MCP_BOOTSTRAPPED !== '1') {
    // The launcher can install/relaunch Node and pass its refreshed PATH to us.
    if (WINDOWS) {
      Run('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', fileURLToPath(new URL('./setup-mcp.ps1', import.meta.url))]);
    } else if (process.platform === 'darwin') {
      Run('/bin/bash', [fileURLToPath(new URL('./setup-mcp.sh', import.meta.url))]);
    } else {
      throw new Error('Automatic runtime installation supports Windows and macOS only.');
    }
    process.exit(0);
  }
  if (Number(process.versions.node.split('.')[0]) < 20) {
    throw new Error('Install Node.js 20 or newer, then rerun setup.');
  }

  console.log(`Checking MCP prerequisites for ${ROOT}`);
  console.log(`Node ${process.versions.node}`);
  for (const command of ['npm', 'uvx', 'unity']) {
    console.log(`Checking ${command} on PATH...`);
    Run(command, ['--version']);
  }

  if (!CHECK_ONLY) {
    for (const directory of ['Tools/McpVault', 'Tools/Penpot']) {
      console.log(`Installing locked dependencies: ${directory}`);
      Run('npm', ['ci', '--prefix', directory, '--ignore-scripts', '--no-fund', '--no-audit']);
    }

    // Read the shared mirror so package pins remain in the MCP configuration.
    const servers = JSON.parse(readFileSync(new URL('../.mcp.json', import.meta.url), 'utf8')).mcpServers;
    for (const [name, requirement] of [
      ['Blender', servers.blender.args[0]],
      ['Graphify', servers.graphify.args[servers.graphify.args.indexOf('--from') + 1]],
    ]) {
      console.log(`Caching ${name}: ${requirement}`);
      Run('uvx', ['--from', requirement, 'python', '-c', 'import sys; print("Python dependencies ready:", sys.version.split()[0])']);
    }
    console.log('MCP package installation complete.');
  }

  const urlFile = new URL('./Penpot/.local/server-url.txt', import.meta.url);
  const hasPenpotUrl = existsSync(urlFile) && readFileSync(urlFile, 'utf8').trim().length > 0;
  console.log(hasPenpotUrl
    ? 'Penpot local URL file is present (connection not tested).'
    : 'Penpot needs Tools/Penpot/.local/server-url.txt containing your private MCP URL.');
  console.log('Open this project in Unity and enable the Blender MCP add-on when using those servers.');
  console.log('Serena requires its separately managed local backend; see Tools/Serena/README.md.');
  console.log('Restart MCP connections with the repository root as the working directory.');
} catch (error) {
  console.error(`MCP setup failed: ${error.message}`);
  console.error('To install missing runtimes, run Tools/setup-mcp.ps1 on Windows or bash Tools/setup-mcp.sh on macOS.');
  process.exitCode = 1;
}
