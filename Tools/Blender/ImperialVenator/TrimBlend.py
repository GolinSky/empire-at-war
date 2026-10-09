"""Run only on task-generated blends in a separate background Blender process."""
import bpy
import hashlib
import struct
import sys

name, output = sys.argv[sys.argv.index('--') + 1:]
source = bpy.data.scenes[name + ' Source']
objects = set(source.objects)


def Fingerprint():
    digest = hashlib.sha256()
    for obj in sorted(objects, key=lambda o: o.name):
        digest.update(str((obj.name, obj.type, obj.parent.name if obj.parent else None, obj.parent_bone, obj.hide_render, list(obj.matrix_world))).encode())
        if obj.type == 'MESH':
            for vertex in obj.data.vertices:
                digest.update(struct.pack('fff', *vertex.co))
            for polygon in obj.data.polygons:
                digest.update(str((list(polygon.vertices), polygon.material_index)).encode())
            for layer in obj.data.uv_layers:
                for uv in layer.data:
                    digest.update(struct.pack('ff', *uv.uv))
            digest.update(str([slot.material.name for slot in obj.material_slots]).encode())
        if obj.type == 'ARMATURE':
            digest.update(str([(b.name, b.parent.name if b.parent else None, list(b.matrix_local)) for b in obj.data.bones]).encode())
    return digest.hexdigest()


before = Fingerprint()
for scene in list(bpy.data.scenes):
    if scene != source:
        bpy.data.scenes.remove(scene)
for obj in list(bpy.data.objects):
    if obj not in objects:
        bpy.data.objects.remove(obj, do_unlink=True)
bpy.ops.outliner.orphans_purge(do_recursive=True)
assert before == Fingerprint()
assert len(bpy.data.scenes) == 1 and set(bpy.data.objects) == objects
bpy.ops.wm.save_as_mainfile(filepath=output, compress=True)
print('TRIM_VERIFIED', name, before, len(objects), 'source objects retained including helpers')
