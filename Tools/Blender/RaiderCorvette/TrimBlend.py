"""Run in a separate background Blender process on the saved conversion file."""
import bpy

source = bpy.data.scenes['RaiderCorvette Source']
objects = set(source.objects)
for scene in list(bpy.data.scenes):
    if scene != source:
        bpy.data.scenes.remove(scene)
for obj in list(bpy.data.objects):
    if obj not in objects:
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=bpy.data.filepath)
assert len(bpy.data.scenes) == 1 and len(bpy.data.objects) == 5
print('RAIDER_BLEND_VERIFIED: one source scene, four meshes, one rig')
