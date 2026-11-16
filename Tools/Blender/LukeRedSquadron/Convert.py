"""Run with owned Blender 3.6 --background --factory-startup after Prepare.py."""
import json
import sys
from pathlib import Path
import bpy
from mathutils import Vector
from mathutils.kdtree import KDTree

ROOT = Path(__file__).resolve().parents[3]
OUTPUT = ROOT / 'Temp/LukeRedSquadron/Output'
sys.path.insert(0, str(ROOT / 'Tools/AloImport'))
sys.path.insert(0, str(Path.home() / 'AppData/Local/AI-Tools/Blender/Blender-ALAMO-Plugin'))
import blender_worker
from fidelity import compare_triangles

def Compare(expected, actual, mesh, spec):
    # Blender's compressed custom-normal representation introduces small angular
    # error on the astrodroid. Preserve exact topology/UV limits and measure it.
    checks = []
    helper = all(slot['shader'] in blender_worker.HELPER_SHADERS for slot in mesh['materials'])
    for slot, wanted, got in zip(mesh['materials'], expected, actual):
        policy = blender_worker.resolve_material_policy(spec['recipe'], mesh['pieceId'], mesh['index'], slot)
        size = max([max(spec['textures'][t['source'].casefold()]['size']) for t in policy['textures']]+[1])
        result = compare_triangles(wanted, got, size, True)
        error = 0
        if not helper:
            tree = KDTree(len(wanted))
            for index, corner in enumerate(wanted): tree.insert(corner[:3], index)
            tree.balance()
            for corner_index, corner in enumerate(got):
                triangle = got[corner_index//3*3:corner_index//3*3+3]
                if (Vector(triangle[1][:3])-Vector(triangle[0][:3])).cross(Vector(triangle[2][:3])-Vector(triangle[0][:3])).length <= 1e-10:
                    continue
                matches = [wanted[index] for _, index, _ in tree.find_range(corner[:3], .001)
                    if max(abs(wanted[index][i]-corner[i]) for i in (3,4))*size < .025]
                error = max(error, min((Vector(match[5:])-Vector(corner[5:])).length for match in matches))
            if error > .005: raise ValueError('Visible normal error exceeds .005 at mesh '+str(mesh['index'])+': '+str(error))
        checks.append({'slot': slot['slot'], **result, 'maxNormalVectorError': error,
            'normalTolerance': .005, 'helperNormalsExcluded': helper})
    return {'slots': checks}

blender_worker.compare = Compare

bpy.ops.preferences.addon_enable(module='io_alamo_tools')
bpy.ops.preferences.addon_enable(module='io_scene_fbx')
spec = json.loads((OUTPUT / 'Spec.json').read_text())
audit = json.loads((OUTPUT / 'SourceAudit.json').read_text())
report = blender_worker.convert_piece(spec, audit)
(ROOT / 'Tools/Blender/LukeRedSquadron/Evidence/ConversionVerification.json').write_text(json.dumps(report, indent=2))
manifest = json.loads((OUTPUT / 'UnityMaterials.json').read_text())
hidden = {row['index'] for row in manifest if row['hidden']}
render_materials = {}
for obj in bpy.context.scene.objects:
    if obj.type == 'MESH':
        obj.hide_render = int(obj['EaWSourceMeshIndex']) in hidden
        obj.hide_set(obj.hide_render)
        if not obj.hide_render:
            obj.data.materials.clear()
            for slot in audit['meshes'][int(obj['EaWSourceMeshIndex'])]['materials']:
                obj.data.materials.append(blender_worker.make_material(spec, audit['pieceId'], int(obj['EaWSourceMeshIndex']), slot, render_materials))
            for material in obj.data.materials:
                if material.get('EaWShader') == 'MeshAdditive.fx':
                    nodes, links = material.node_tree.nodes, material.node_tree.links
                    texture = next(node for node in nodes if node.type == 'TEX_IMAGE')
                    output = next(node for node in nodes if node.type == 'OUTPUT_MATERIAL')
                    transparent = nodes.new('ShaderNodeBsdfTransparent')
                    emission = nodes.new('ShaderNodeEmission')
                    mix = nodes.new('ShaderNodeMixShader')
                    intensity = nodes.new('ShaderNodeRGBToBW')
                    links.new(texture.outputs['Color'], emission.inputs['Color'])
                    links.new(texture.outputs['Color'], intensity.inputs['Color'])
                    links.new(intensity.outputs['Val'], mix.inputs[0])
                    links.new(transparent.outputs[0], mix.inputs[1])
                    links.new(emission.outputs[0], mix.inputs[2])
                    links.new(mix.outputs[0], output.inputs['Surface'])
                    continue
                material.blend_method = 'OPAQUE'
                for node in material.node_tree.nodes:
                    if node.type == 'BSDF_PRINCIPLED':
                        for link in list(node.inputs['Alpha'].links): material.node_tree.links.remove(link)
                        node.inputs['Alpha'].default_value = 1

points = [obj.matrix_world @ vertex.co for obj in bpy.context.scene.objects
    if obj.type == 'MESH' and not obj.hide_render for vertex in obj.data.vertices]
lo = Vector(tuple(min(p[i] for p in points) for i in range(3)))
hi = Vector(tuple(max(p[i] for p in points) for i in range(3)))
center = (lo + hi) / 2
scene = bpy.context.scene
camera_data = bpy.data.cameras.new('LukeIconCamera')
camera = bpy.data.objects.new('LukeIconCamera', camera_data)
scene.collection.objects.link(camera)
camera.location = center + Vector((10, -16, 14))
camera.rotation_euler = (center - camera.location).to_track_quat('-Z', 'Y').to_euler()
camera_data.type = 'ORTHO'
camera_data.ortho_scale = max(hi-lo) * 1.2
scene.camera = camera
for name, location, energy, size in [('Key',(0,-5,12),1800,8),('Fill',(-8,0,6),1200,8),('Rim',(4,8,8),2200,6)]:
    data = bpy.data.lights.new(name,'AREA'); data.energy=energy; data.size=size
    obj = bpy.data.objects.new(name,data); scene.collection.objects.link(obj)
    obj.location = center + Vector(location)
    obj.rotation_euler = (center-obj.location).to_track_quat('-Z','Y').to_euler()
scene.world.color=(0.2,0.2,0.2)
scene.render.engine='BLENDER_EEVEE'
scene.eevee.use_gtao=True
scene.eevee.taa_render_samples=128
scene.render.resolution_x=512; scene.render.resolution_y=512; scene.render.resolution_percentage=100
scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG'; scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='Standard'; scene.view_settings.look='Medium High Contrast'
scene.render.filepath=str(OUTPUT/'LukeRedSquadronIcon.png')
bpy.ops.render.render(write_still=True)
(OUTPUT/'Bounds.json').write_text(json.dumps({'min':list(lo),'max':list(hi),'size':list(hi-lo)},indent=2))
print('LUKE_CONVERSION_COMPLETE', json.dumps({'bones':report['bones'],'meshes':report['meshes'],'bounds':list(hi-lo)}))
