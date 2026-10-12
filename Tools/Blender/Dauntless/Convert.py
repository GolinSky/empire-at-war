"""Convert audited Dauntless models through the shared safe-mode Blender MCP."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/DauntlessImport')
source = source.replace("assert bpy.types.blendermcp_server.port == 9886", "assert bpy.app.version_string == '3.6.23'")
source = source.replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/mcp_call.py')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import Dauntless Heavy Cruiser')
# Preserve existing Blender scenes. ALAMO scans all objects, so borrow them only during import.
source = source.replace('repair =', '''exporter = exporter.replace("for obj in list(bpy.data.objects):\\n    bpy.data.objects.remove(obj, do_unlink=True)\\nscene = bpy.context.scene", "scene = bpy.data.scenes.new(VARIANT + ' Source')\\nbpy.context.window.scene = scene")
exporter = exporter.replace("bpy.ops.import_mesh.alo(filepath=SOURCE, importAnimations=False)", "borrowed = [o for o in bpy.data.objects if o.name not in scene.objects]\\nvisibility = [(o, o.hide_render, o.hide_viewport) for o in borrowed]\\nfor obj in borrowed: scene.collection.objects.link(obj)\\nbpy.ops.import_mesh.alo(filepath=SOURCE, importAnimations=False)\\nfor obj, render, viewport in visibility:\\n    scene.collection.objects.unlink(obj)\\n    obj.hide_render, obj.hide_viewport = render, viewport")
repair =''')
# Identify meshes by importer creation order: other scenes may already own Hull/Shield names.
source = source.replace("obj = next(o for o in scene.objects if o.type == 'MESH' and o.name == imported_name)", "obj = [o for o in scene.objects if o.type == 'MESH'][list(AUDIT['material_audit']['meshes']).index(record)]\n    obj['EaWName'] = imported_name")
source = source.replace('exporter = exporter.replace("rig =', '''exporter = exporter.replace("obj['EaWName'], obj['EaWHidden'] = obj.name, obj.hide_render", "obj['EaWName'], obj['EaWHidden'] = obj.get('EaWName', obj.name), obj.hide_render")
exporter = exporter.replace("rig =''')
exec(compile(source, 'Tools/Blender/Dauntless/Convert.py', 'exec'))
