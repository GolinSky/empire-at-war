import bpy, json
from mathutils import Vector
from pathlib import Path
AUDIT=json.loads(Path('Temp/AllegianceImport/SourceAudit.json').read_text())

def Clear():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)

def Geometry():
    bpy.context.view_layer.update()
    result={}
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH':
            continue
        obj.data.calc_loop_triangles()
        uv=obj.data.uv_layers.active.data
        corners=[]
        for triangle in obj.data.loop_triangles:
            for loop in triangle.loops:
                vertex=obj.data.loops[loop].vertex_index
                p=obj.matrix_world @ obj.data.vertices[vertex].co
                corners.append(tuple(p)+tuple(uv[loop].uv))
        unique={tuple(round(x,2 if i<3 else 5) for i,x in enumerate(p)):p for p in corners}
        corners=list(unique.values())
        corners.sort(key=SortCorner)
        base=obj.get('EaWName',obj.name).rsplit('.',1)[0]
        center=tuple(0.0+round(sum(p[i] for p in corners)/len(corners),2) for i in range(3))
        key=base+'|'+str(len(corners))+'|'+str(center)
        assert key not in result,key
        result[key]=corners
    return result

def SortCorner(p):
    return tuple(round(x,2 if i<3 else 5) for i,x in enumerate(p))

reports=[]
for name,record in AUDIT['models'].items():
    Clear()
    bpy.ops.import_mesh.alo(filepath=record['file'],importAnimations=False)
    before=Geometry()
    Clear()
    bpy.ops.import_scene.fbx(filepath='F:/Private/empire-at-war/Temp/AllegianceImport/Output/'+name+'.fbx',use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
    after=Geometry()
    assert set(before)==set(after),(set(before)-set(after),set(after)-set(before))
    position_error=0
    uv_error=0
    for key in before:
        assert len(before[key])==len(after[key]),key
        by_uv={}
        for p in after[key]:
            uv_key=tuple(round(x,5) for x in p[3:])
            by_uv.setdefault(uv_key,[]).append(p)
        for a in before[key]:
            candidates=by_uv[tuple(round(x,5) for x in a[3:])]
            best=min((sum((a[i]-b[i])**2 for i in range(3)),b) for b in candidates)[1]
            position_error=max(position_error,max(abs(a[i]-best[i]) for i in range(3)))
            uv_error=max(uv_error,max(abs(a[i]-best[i]) for i in range(3,5)))
    assert position_error<.001,position_error
    assert uv_error<.00001,uv_error
    reports.append(dict(name=name,mesh_count=len(before),triangle_corners=sum(len(v) for v in before.values()),position_error=position_error,uv_error=uv_error))
Path('Temp/AllegianceImport/GeometryVerification.json').write_text(json.dumps(reports,indent=2))
print('Verified',len(reports),'models; max position/UV error',max(r['position_error'] for r in reports),max(r['uv_error'] for r in reports))

