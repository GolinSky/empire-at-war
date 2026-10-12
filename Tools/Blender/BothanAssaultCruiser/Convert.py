"""Reuse the audited exporter in a fresh scene, preserving existing Blender work."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/BothanAssaultCruiserImport')
source = source.replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/mcp_call.py')
source = source.replace("assert bpy.types.blendermcp_server.port == 9886", "assert bpy.app.version_string == '3.6.23'")
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import Bothan Assault Cruiser')
insertion = '''
exporter = exporter.replace("for obj in list(bpy.data.objects):\\n    bpy.data.objects.remove(obj, do_unlink=True)\\nscene = bpy.context.scene", "previous_scene = bpy.context.scene\\nscene = bpy.data.scenes.new(VARIANT + ' Source')\\nbpy.context.window.scene = scene")
exporter = exporter.replace("bpy.ops.import_mesh.alo(filepath=SOURCE, importAnimations=False)", "borrowed = [(o, o.name, o.hide_render, o.hide_viewport) for o in list(bpy.data.objects)]\\nfor obj, name, hidden, viewport in borrowed:\\n    obj.name = '__BothanBorrowed_' + name\\n    scene.collection.objects.link(obj)\\ntry:\\n    bpy.ops.import_mesh.alo(filepath=SOURCE, importAnimations=False)\\nfinally:\\n    for obj, name, hidden, viewport in borrowed:\\n        scene.collection.objects.unlink(obj)\\n        obj.hide_render, obj.hide_viewport = hidden, viewport")
exporter = exporter.replace("bpy.ops.wm.save_as_mainfile(filepath=OUTPUT + '/' + VARIANT + '.blend')", "bpy.ops.wm.save_as_mainfile(filepath=OUTPUT + '/' + VARIANT + '.blend', copy=True)")
exporter += "\\nfor obj, name, hidden, viewport in borrowed:\\n    obj.name = name\\nbpy.context.window.scene = scene\\n"
'''
source = source.replace('for name, audit in audits.items():', insertion + '\nfor name, audit in audits.items():')
exec(compile(source, __file__, 'exec'))
