"""Use the configured MCP SDK with this import's isolated Blender port."""
import asyncio
import base64
import json
import os
import sys
import tomllib
from datetime import timedelta
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client


async def Main():
    request_path = Path(sys.argv[1])
    request = json.loads(request_path.read_text(encoding='utf-8-sig'))
    config = tomllib.loads(Path('.codex/config.toml').read_text(encoding='utf-8'))['mcp_servers']['blender']
    environment = dict(os.environ, **config['env'])
    environment['BLENDER_PORT'] = '9890'
    parameters = StdioServerParameters(command=config['command'], args=config['args'], env=environment)
    async with stdio_client(parameters) as (read, write):
        async with ClientSession(read, write, read_timeout_seconds=timedelta(seconds=180)) as session:
            await session.initialize()
            if request['tool'] == 'list_tools':
                result = await session.list_tools()
                print(json.dumps([{'name': t.name, 'schema': t.inputSchema} for t in result.tools]))
                return
            arguments = request.get('arguments', {})
            if 'code_file' in request:
                arguments['code'] = Path(request['code_file']).read_text(encoding='utf-8-sig')
            result = await session.call_tool(request['tool'], arguments)
            for index, item in enumerate(result.content):
                if item.type == 'image':
                    path = request_path.with_suffix(f'.{index}.png')
                    path.write_bytes(base64.b64decode(item.data))
                    print(path)
                elif item.type == 'text':
                    print(item.text)
                    if item.text.startswith(('Error executing code:', 'Rejected by safe mode')):
                        raise RuntimeError('Blender operation failed')
            if result.isError:
                raise RuntimeError('MCP call failed')


asyncio.run(Main())
