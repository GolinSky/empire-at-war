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


async def main():
    request = json.loads(Path(sys.argv[1]).read_text(encoding='utf-8-sig'))
    config_path = Path(__file__).resolve().parents[2] / '.codex' / 'config.toml'
    config = tomllib.loads(config_path.read_text(encoding='utf-8'))['mcp_servers']['blender']
    environment = dict(os.environ, **config['env'])
    parameters = StdioServerParameters(command=config['command'], args=config['args'], env=environment)
    async with stdio_client(parameters) as (read, write):
        async with ClientSession(read, write, read_timeout_seconds=timedelta(seconds=180)) as session:
            await session.initialize()
            if request['tool'] == 'list_tools':
                result = await session.list_tools()
                print(json.dumps([{'name': tool.name, 'schema': tool.inputSchema} for tool in result.tools]))
                return
            arguments = request.get('arguments', {})
            if 'code_file' in request:
                arguments['code'] = Path(request['code_file']).read_text(encoding='utf-8-sig')
            result = await session.call_tool(request['tool'], arguments)
            for index, item in enumerate(result.content):
                if item.type == 'image':
                    path = Path(sys.argv[1]).with_suffix(f'.{index}.png')
                    path.write_bytes(base64.b64decode(item.data))
                    print(path)
                elif item.type == 'text':
                    print(item.text)
                    if item.text.startswith(('Error executing code:', 'Rejected by safe mode')):
                        raise RuntimeError('Blender rejected or failed the requested operation')
            if result.isError:
                raise RuntimeError('MCP call failed')


asyncio.run(main())
