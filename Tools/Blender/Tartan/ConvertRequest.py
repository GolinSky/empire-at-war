"""Send audited source data and conversion code to the isolated Blender MCP."""
import asyncio
import json
import os
import tomllib
from datetime import timedelta
from pathlib import Path
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

async def Main():
    config = tomllib.loads(Path('.codex/config.toml').read_text())['mcp_servers']['blender']
    environment = dict(os.environ, **config['env'])
    environment['BLENDER_PORT'] = '9885'
    parameters = StdioServerParameters(command=config['command'], args=config['args'], env=environment)
    async with stdio_client(parameters) as (read, write):
        async with ClientSession(read, write, read_timeout_seconds=timedelta(seconds=180)) as session:
            await session.initialize()
            for name in ('get_addon_status', 'get_scene_info'):
                result = await session.call_tool(name, {'user_prompt': 'import Tartan-class Patrol Cruiser; run test; commit after'})
                print(result)
            audit = Path('Temp/TartanImport/SourceAudit.json').read_text()
            code = 'import json\naudit = json.loads(' + repr(audit) + ')\n' + Path('Tools/Blender/Tartan/Convert.py').read_text()
            result = await session.call_tool('execute_blender_code', {'code': code, 'user_prompt': 'import Tartan-class Patrol Cruiser; hangar squadrons left disabled; use power to weapons ship ability; run test - fix relate to this unit issues; commit after'})
            for item in result.content:
                if item.type != 'text':
                    continue
                print(item.text)
                if 'CONVERSION_JSON ' in item.text:
                    report = item.text.split('CONVERSION_JSON ', 1)[1].split('\n', 1)[0]
                    Path('Temp/TartanImport/ConversionReport.json').write_text(json.dumps(json.loads(report), indent=2))
            if not Path('Temp/TartanImport/ConversionReport.json').exists():
                raise RuntimeError('Conversion did not finish')

asyncio.run(Main())
