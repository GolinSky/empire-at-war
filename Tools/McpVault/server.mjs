import { createServer as createHttpServer } from 'node:http';
import { realpathSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { createServer } from '@bitbonsai/mcpvault';
import { createMcpHandler } from '@modelcontextprotocol/server';
import { toNodeHandler } from '@modelcontextprotocol/node';

const HOST = '127.0.0.1';
const PORT = 9123;
const VAULT = realpathSync(fileURLToPath(new URL('../../EmpireAtWarDocumentation', import.meta.url)));
const handler = createMcpHandler(() => createServer(VAULT, { version: '0.16.0' }), {
  onerror: error => console.error(error),
});
const handleMcp = toNodeHandler(handler, { onerror: error => console.error(error) });

const server = createHttpServer((request, response) => {
  // Only local clients may access the vault. Reject browser origins and DNS rebinding.
  if (request.headers.host !== `${HOST}:${PORT}` || request.headers.origin !== undefined) {
    response.writeHead(403).end();
    return;
  }
  if (request.method === 'GET' && request.url === '/health') {
    response.writeHead(200, { 'Content-Type': 'application/json' });
    response.end(JSON.stringify({ service: 'empire-vault', vault: VAULT }));
    return;
  }
  if (request.url !== '/mcp') {
    response.writeHead(404).end();
    return;
  }
  handleMcp(request, response);
});
server.on('error', error => {
  console.error(error);
  process.exitCode = 1;
});
server.listen(PORT, HOST, () => console.log(`empire-vault listening on http://${HOST}:${PORT}/mcp`));
for (const signal of ['SIGTERM', 'SIGINT']) {
  process.on(signal, async () => {
    await handler.close();
    server.close();
    server.closeAllConnections();
  });
}
