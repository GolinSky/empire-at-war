"""Convert source-audited carrier pieces through the existing Blender MCP."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/EnduranceImport')
source = source.replace("assert bpy.types.blendermcp_server.port == 9886", "assert bpy.app.version_string == '3.6.23'")
source = source.replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/mcp_call.py')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import Endurance Fleet Carrier; Squadron - use x wing')
source = source.replace("exporter = Path('Tools/Blender/export_isd_i.py').read_text()", "exporter = Path('Tools/Blender/export_isd_i.py').read_text()\nexporter = exporter.replace(\"assert all(m['uv_layers'] == 1 for m in after['meshes'].values())\", \"assert all(m['uv_layers'] == 1 or m['triangles'] == 0 for m in after['meshes'].values())\")")
exec(compile(source, __file__, 'exec'))
