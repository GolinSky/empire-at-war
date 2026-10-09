"""Capture position/UV corners through Blender MCP for Unity import verification."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path('Temp/ISDIIReplacement')
variants = list(json.loads((ROOT/'SourceAudit.json').read_text())['variants'])
code = f"ROOT={ROOT.resolve().as_posix()!r}\nVARIANTS={variants!r}\n" + '''
import bpy, json
assert bpy.types.blendermcp_server.port == 9890
result={}
for name in VARIANTS:
    for obj in list(bpy.data.objects): bpy.data.objects.remove(obj,do_unlink=True)
    bpy.ops.import_scene.fbx(filepath=ROOT+'/'+name+'/'+name+'.fbx',use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
    bpy.context.view_layer.update()
    meshes={}
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':continue
        obj.data.calc_loop_triangles()
        uv=obj.data.uv_layers.active.data
        corners=set()
        for tri in obj.data.loop_triangles:
            for loop in tri.loops:
                pos=obj.matrix_world @ obj.data.vertices[obj.data.loops[loop].vertex_index].co
                corners.add(tuple(pos)+tuple(uv[loop].uv))
        meshes[obj.get('EaWName',obj.name)]=list(corners)
    result[name]=meshes
print('GEOMETRY_JSON '+json.dumps(result))
'''
request=ROOT/'GeometryReferenceRequest.json'
request.write_text(json.dumps(dict(tool='execute_blender_code',arguments=dict(code=code,user_prompt='Use Blender for conversion, preserving source geometry, UVs, textures, hierarchy, and attachment positions.'))))
result=subprocess.run(['uvx','--from','mcp-for-blender==2.1.3','python','Tools/Blender/ISDIIReplacement/Call.py',str(request)],capture_output=True,text=True)
match=re.search(r'GEOMETRY_JSON (.+)',result.stdout)
if result.returncode or match is None: raise RuntimeError(result.stdout[:3000])
(ROOT/'GeometryReference.json').write_text(match.group(1))
print('Captured',len(json.loads(match.group(1))),'geometry/UV references.')
