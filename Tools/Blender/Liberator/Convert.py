"""Convert one source model at a time in this import's isolated Blender MCP."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/LiberatorImport')
source = source.replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/Liberator/Call.py')
source = source.replace('port == 9886', 'port == 9893')
source = source.replace('for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials):', 'for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials, bpy.data.images):')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import Liberator Cruiser')
exec(compile(source, __file__, 'exec'))
