import bpy, json
from mathutils import Vector

# Clear() is allowed only in this task's dedicated Blender process.
assert bpy.types.blendermcp_server.port == 9880, "Use the isolated Victory II Blender MCP session on port 9880."

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
        center=tuple(round(sum(p[i] for p in corners)/len(corners)) for i in range(3))
        key=base+'|'+str(len(corners))+'|'+str(center)
        assert key not in result,key
        result[key]=corners
    return result

def SortCorner(p):
    return tuple(round(x,2 if i<3 else 5) for i,x in enumerate(p))

reports=[]
for name,alo in [('VictoryIIAdvanced','EV_VSD_II.ALO'),('VictoryIIAdvancedTurret01','Empire_Imperial_Star_Destroyer_Triple_Turbolaser_01.ALO'),('VictoryIIAdvancedTurret02','Empire_Imperial_Star_Destroyer_Triple_Turbolaser_02.ALO')]:
    Clear()
    bpy.ops.import_mesh.alo(filepath='D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/'+alo,importAnimations=False)
    before=Geometry()
    print('SOURCE_GEOMETRY '+json.dumps(dict(name=name,geometry=before)))
    Clear()
    bpy.ops.import_scene.fbx(filepath='F:/Private/empire-at-war/Temp/VictoryIIAdvancedImport/Output/'+name+'.fbx',use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
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
    assert position_error<.35,position_error
    assert uv_error<.00001,uv_error
    reports.append(dict(name=name,mesh_count=len(before),triangle_corners=sum(len(v) for v in before.values()),position_error=position_error,uv_error=uv_error))
print('GEOMETRY_JSON '+json.dumps(reports))
Clear()
bpy.ops.import_scene.fbx(filepath='F:/Private/empire-at-war/Temp/VictoryIIAdvancedImport/Output/VictoryIIAdvanced.fbx',use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
for obj in bpy.context.scene.objects:
    if obj.type=='ARMATURE':
        obj.hide_set(True)
    if obj.type=='MESH':
        obj.hide_render=bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
for area in bpy.context.screen.areas:
    if area.type=='VIEW_3D':
        area.spaces.active.shading.type='MATERIAL'
        area.spaces.active.overlay.show_overlays=False
        region=area.spaces.active.region_3d
        region.view_location=Vector((0,0,0))
        region.view_distance=650
        region.view_rotation=Vector((1,-1,1)).to_track_quat('Z','Y')
        region.view_perspective='ORTHO'
