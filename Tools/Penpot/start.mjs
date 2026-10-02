import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

const serverUrl = readFileSync(new URL('./.local/server-url.txt', import.meta.url), 'utf8').trim();
const proxyPath = fileURLToPath(new URL('./node_modules/mcp-remote/dist/proxy.js', import.meta.url));

process.argv = [process.execPath, proxyPath, serverUrl, '--transport', 'http-only', '--silent'];
await import('./node_modules/mcp-remote/dist/proxy.js');
