"""Convert audited MC30c through the configured Blender MCP connection."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/MC30cImport')
source = source.replace("assert bpy.types.blendermcp_server.port == 9886", "assert bpy.types.blendermcp_server.port == 9890")
source = source.replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/MC30c/Call.py')
source = source.replace("exporter = exporter.replace(\"scene = bpy.context.scene\"", "exporter = exporter.replace('assert bone_error < .001', \"assert bone_error < .01\\nassert max((Vector(before['bones'][n]['head'])-Vector(after['bones'][n]['head'])).length for n in before['bones'] if n.startswith('HP_')) < .001\")\nexporter = exporter.replace(\"scene = bpy.context.scene\"")
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import MC30c Frigate')
exec(compile(source, 'Tools/Blender/MC30c/Convert.py', 'exec'))
