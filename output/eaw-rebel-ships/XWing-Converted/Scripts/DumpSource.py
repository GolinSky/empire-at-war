import bpy,json

scene=bpy.data.scenes['XWing Source']
bpy.context.window.scene=scene
rig=next(o for o in scene.objects if o.type=='ARMATURE')
rig.animation_data.action=bpy.data.actions['Undeploy']
scene.frame_set(30)
bpy.context.view_layer.update()
result=[]
for o in scene.objects:
    if o.type!='MESH':continue
    o.data.calc_loop_triangles()
    result.append({'name':o.name,'triangles':[[[*(o.matrix_world@o.data.vertices[v].co),*o.data.uv_layers.active.data[l].uv] for v,l in zip(t.vertices,t.loops)] for t in o.data.loop_triangles]})
print('XWING_GEOMETRY',json.dumps(result))
