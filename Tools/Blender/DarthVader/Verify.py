import sys,json
from pathlib import Path
import bpy
from mathutils import Vector
sys.path.insert(0,'F:/Private/empire-at-war/Tools/AloImport')
from blender_worker import matrices,expected_corners,actual_corners
from fidelity import compare_triangles
out=Path('F:/Private/empire-at-war/Temp/VaderImport/Output')
a=json.loads((out/'SourceAudit.json').read_text())
for o in list(bpy.data.objects):bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.import_scene.fbx(filepath=str(out/'DarthVader.fbx'),use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
world=matrices(a['bones']);results=[]
for m in a['meshes']:
 o=next(o for o in bpy.context.scene.objects if o.get('EaWSourceMeshIndex')==m['index'])
 bi=next(c['bone_index'] for c in a['connections'] if c['object_index']==m['object_index'])
 expect=expected_corners(m,world[bi]);actual=actual_corners(o)
 try: result=compare_triangles(expect[0],actual[0],1024,m['index'] in (7,9))
 except ValueError as e: result={'error':str(e)}
 results.append({'name':m['name'],'index':m['index'],**result})
rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
bone_error=max((rig.matrix_world@rig.data.bones[b['name']].head_local-world[b['index']].translation).length for b in a['bones'])
pts=[o.matrix_world@v.co for o in bpy.context.scene.objects if o.type=='MESH' and o.get('EaWSourceMeshIndex',10)<7 for v in o.data.vertices]
report={'sourceSha256':a['sha256'],'meshes':results,'bones':len(rig.data.bones),'boneError':bone_error,'boundsMin':[min(p[i] for p in pts) for i in range(3)],'boundsMax':[max(p[i] for p in pts) for i in range(3)],'attachments':{b['name']:list(world[b['index']].translation) for b in a['bones'] if b['name'].startswith(('Muzzle','HP_'))}}
(out/'Verification.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
