"""Call the installed Blender MCP tool dispatcher directly, retaining its safe-mode validation."""
import asyncio
import json
import os
import sys
import tomllib
from pathlib import Path


async def Main():
    config = tomllib.loads(Path('.codex/config.toml').read_text(encoding='utf-8'))['mcp_servers']['blender']
    os.environ.update(config['env'])
    os.environ['BLENDER_PORT'] = '9897'
    from blender_mcp.server import mcp
    path = Path(sys.argv[1])
    payload = json.loads(path.read_text(encoding='utf-8-sig'))
    requests = [path.parent / (name + '.json') for name in payload] if isinstance(payload, list) else [path]
    for request_path in requests:
        request = json.loads(request_path.read_text(encoding='utf-8-sig'))
        result = await mcp.call_tool(request['tool'], request['arguments'])
        blocks = result[0] if isinstance(result, tuple) else result
        output = '\n'.join(block.text for block in blocks if block.type == 'text')
        request_path.with_suffix('.log').write_text(output, encoding='utf-8')
        if 'Error executing code:' in output or 'Rejected by safe mode' in output:
            raise RuntimeError(output)
        print('Completed ' + request_path.stem, flush=True)


asyncio.run(Main())
