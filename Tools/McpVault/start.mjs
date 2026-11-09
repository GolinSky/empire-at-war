import { spawn } from 'node:child_process';
import { mkdirSync, openSync, closeSync, realpathSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const ENDPOINT = 'http://127.0.0.1:9123';
const ROOT = fileURLToPath(new URL('../../', import.meta.url));
const VAULT = realpathSync(fileURLToPath(new URL('../../EmpireAtWarDocumentation', import.meta.url)));

async function IsRunning() {
  let response;
  try {
    response = await fetch(`${ENDPOINT}/health`, { signal: AbortSignal.timeout(1000) });
  } catch (error) {
    if (error.cause?.code === 'ECONNREFUSED') return false;
    throw error;
  }
  const identity = await response.json();
  if (!response.ok || identity.service !== 'empire-vault' || identity.vault !== VAULT) {
    throw new Error(`Port 9123 belongs to another service or checkout. Expected vault: ${VAULT}`);
  }
  return true;
}

if (!await IsRunning()) {
  const directory = new URL('./.service/', import.meta.url);
  mkdirSync(directory, { recursive: true });
  const log = openSync(new URL('server.log', directory), 'a');
  const child = spawn(process.execPath, [fileURLToPath(new URL('./server.mjs', import.meta.url))], {
    cwd: ROOT, detached: true, windowsHide: true, stdio: ['ignore', log, log],
  });
  child.on('error', error => { throw error; });
  child.unref();
  closeSync(log);
  let ready = false;
  for (let attempt = 0; attempt < 100; attempt++) {
    await new Promise(resolve => setTimeout(resolve, 100));
    if (await IsRunning()) { ready = true; break; }
  }
  if (!ready) throw new Error('empire-vault did not start. Inspect Tools/McpVault/.service/server.log.');
}
console.log(`empire-vault ready at ${ENDPOINT}/mcp`);
