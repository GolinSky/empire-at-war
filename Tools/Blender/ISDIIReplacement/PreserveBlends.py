"""Verify and preserve compressed, texture-packed editable source conversions."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path('Temp/ISDIIReplacement')
OUTPUT = Path('output/ISDIIReplacement/Blender')
OUTPUT.mkdir(parents=True, exist_ok=True)
variants = list(json.loads((ROOT/'SourceAudit.json').read_text())['variants'])
code = f"ROOT={ROOT.resolve().as_posix()!r}\nOUTPUT={OUTPUT.resolve().as_posix()!r}\nVARIANTS={variants!r}\n" + '''
import bpy, json
assert bpy.types.blendermcp_server.port == 9890
result=[]
for name in VARIANTS:
    bpy.ops.wm.open_mainfile(filepath=ROOT+'/'+name+'/'+name+'.blend')
    meshes=[obj for obj in bpy.context.scene.objects if obj.type=='MESH']
    for obj in meshes: obj.hide_set(obj.hide_render)
    bpy.ops.file.pack_all()
    images=[img for img in bpy.data.images if img.source=='FILE' and img.users>0]
    assert all(img.packed_file is not None for img in images), name
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT+'/'+name+'.blend',compress=True,copy=True)
    result.append(dict(model=name,meshes=len(meshes),bones=sum(len(obj.data.bones) for obj in bpy.context.scene.objects if obj.type=='ARMATURE'),packed_images=len(images)))
print('EDITABLE_JSON '+json.dumps(result))
'''
request = ROOT/'PreserveBlendsRequest.json'
request.write_text(json.dumps(dict(tool='execute_blender_code',arguments=dict(code=code,user_prompt='Use Blender for conversion, preserving source geometry, UVs, textures, hierarchy, and attachment positions.'))))
result = subprocess.run(['uvx','--from','mcp-for-blender==2.1.3','python','Tools/Blender/ISDIIReplacement/Call.py',str(request)],capture_output=True,text=True)
(ROOT/'PreserveBlends.log').write_text(result.stdout+result.stderr)
match = re.search(r'EDITABLE_JSON (.+)',result.stdout)
if result.returncode or match is None: raise RuntimeError(result.stdout[:3000])
records=json.loads(match.group(1))
(ROOT/'VerifiedEditableBlends.json').write_text(json.dumps(records,indent=2))
print('Preserved',len(records),'editable blends; all used image textures packed.')
