"""Run the source-audited Quasar exporter through the existing Blender MCP."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/QuasarFireImport')
source = source.replace("assert bpy.types.blendermcp_server.port == 9886", "assert bpy.app.version_string == '3.6.23'")
source = source.replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/mcp_call.py')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import Quasar Fire')
exec(compile(source, __file__, 'exec'))
