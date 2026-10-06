import bpy, json
from mathutils import Vector

# Clear() is allowed only in this task's dedicated Blender process.
assert bpy.types.blendermcp_server.port == 9880, "Use the isolated Victory II Blender MCP session on port 9880."

OUTPUT = 'F:/Private/empire-at-war/Temp/VictoryIIAdvancedImport/Output'
SOURCE = 'D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/'


def Clear():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)


def RestoreRoot(rig, count):
    assert len(rig.data.bones) == count - 1
    assert 'Root' not in rig.data.bones
    bpy.ops.object.select_all(action='DESELECT')
    rig.hide_set(False)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode='EDIT')
    root = rig.data.edit_bones.new('Root')
    root.head = (0, 0, 0)
    root.tail = (0, 1, 0)
    for bone in rig.data.edit_bones:
        if bone != root and bone.parent is None:
            bone.parent = root
    bpy.ops.object.mode_set(mode='OBJECT')


def Snapshot(scene):
    bpy.context.view_layer.update()
    meshes = [o for o in scene.objects if o.type == 'MESH']
    points = [o.matrix_world @ v.co for o in meshes if not o.hide_render for v in o.data.vertices]
    return dict(meshes={o.get('EaWName', o.name):dict(triangles=sum(len(p.vertices)-2 for p in o.data.polygons),uv_layers=len(o.data.uv_layers),hidden=o.hide_render,materials=[s.material.name for s in o.material_slots]) for o in meshes},
                bones={o.get('EaWName',o.name)+'/'+b.name:dict(parent=b.parent.name if b.parent else None,head=list(o.matrix_world @ b.head_local)) for o in scene.objects if o.type=='ARMATURE' for b in o.data.bones},
                bounds_min=[min(p[i] for p in points) for i in range(3)],bounds_max=[max(p[i] for p in points) for i in range(3)])


def Export(name):
    scene = bpy.context.scene
    for obj in scene.objects:
        obj['EaWName'] = obj.name
        if obj.type != 'MESH':
            continue
        obj['EaWHidden'] = obj.hide_render
        world = obj.matrix_world.copy()
        if obj.constraints:
            assert len(obj.constraints)==1 and obj.constraints[0].type=='CHILD_OF'
            constraint=obj.constraints[0]
            obj.parent=constraint.target
            obj.parent_type='BONE'
            obj.parent_bone=constraint.subtarget
            obj.constraints.remove(constraint)
            bpy.context.view_layer.update()
            obj.matrix_world=world
    materials=[]
    for mat in {s.material for o in scene.objects if o.type=='MESH' for s in o.material_slots}:
        shader=mat.shaderList.shaderList
        base=mat.BaseTexture
        normal=mat.NormalTexture if 'Bump' in shader else None
        materials.append(dict(name=mat.name,shader=shader,base=base,normal=normal))
        mat['EaWShader']=shader
        mat.use_nodes=True
        mat.node_tree.nodes.clear()
        output=mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
        surface=mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
        mat.node_tree.links.new(surface.outputs['BSDF'],output.inputs['Surface'])
        for filename, socket in [(base,'Base Color'),(normal,'Normal')]:
            if not filename or filename=='None':
                continue
            image=next(i for i in bpy.data.images if i.name.lower()==filename.lower())
            assert image.size[0] > 0, filename
            png=OUTPUT+'/Textures/'+filename[:-4]+'.png'
            image.filepath_raw=png
            image.file_format='PNG'
            image.save()
            image.pack()
            node=mat.node_tree.nodes.new('ShaderNodeTexImage')
            node.image=image
            if socket=='Normal':
                image.colorspace_settings.is_data=True
                bump=mat.node_tree.nodes.new('ShaderNodeNormalMap')
                mat.node_tree.links.new(node.outputs['Color'],bump.inputs['Color'])
                mat.node_tree.links.new(bump.outputs['Normal'],surface.inputs['Normal'])
            else:
                mat.node_tree.links.new(node.outputs['Color'],surface.inputs['Base Color'])
    before=Snapshot(scene)
    bpy.ops.wm.save_as_mainfile(filepath=OUTPUT+'/'+name+'.blend', copy=True)
    bpy.ops.export_scene.fbx(filepath=OUTPUT+'/'+name+'.fbx',use_selection=False,object_types={'MESH','ARMATURE','EMPTY'},add_leaf_bones=False,use_armature_deform_only=False,bake_anim=False,use_custom_props=True,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
    validation=bpy.data.scenes.new(name+' Validation')
    bpy.context.window.scene=validation
    bpy.ops.import_scene.fbx(filepath=OUTPUT+'/'+name+'.fbx',use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
    for obj in validation.objects:
        if obj.type=='MESH':
            obj.hide_render=bool(obj['EaWHidden'])
    after=Snapshot(validation)
    assert set(before['meshes'])==set(after['meshes'])
    assert set(before['bones'])==set(after['bones']), (set(before['bones'])-set(after['bones']),set(after['bones'])-set(before['bones']))
    for key in before['meshes']:
        assert before['meshes'][key]['triangles']==after['meshes'][key]['triangles'],key
        assert before['meshes'][key]['uv_layers']==after['meshes'][key]['uv_layers'],key
    error=max((Vector(before['bones'][k]['head'])-Vector(after['bones'][k]['head'])).length for k in before['bones'])
    # Blender's FBX importer shortens the near-coincident engine bone to 0.01,
    # introducing <0.1 source-unit error in its descendants. Check Unity separately.
    assert error < .1,error
    bounds_error=max(abs(before[k][i]-after[k][i]) for k in ('bounds_min','bounds_max') for i in range(3))
    assert bounds_error < .25,bounds_error
    print('CONVERSION_JSON '+json.dumps(dict(name=name,source=before,roundtrip=after,materials=materials,bone_error=error,bounds_error=bounds_error)))
    Clear()
    bpy.context.window.scene=scene
    bpy.data.scenes.remove(validation)


Clear()
previous_scene = bpy.context.window.scene
bpy.context.window.scene = bpy.data.scenes.new('VictoryIIAdvanced Source')
bpy.ops.import_mesh.alo(filepath=SOURCE+'EV_VSD_II.ALO',importAnimations=False)
main=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
main.name='VictoryIIAdvancedRig'
RestoreRoot(main,180)
Export('VictoryIIAdvanced')
for suffix in ('01','02'):
    name='VictoryIIAdvancedTurret'+suffix
    bpy.context.window.scene=bpy.data.scenes.new(name+' Source')
    bpy.ops.import_mesh.alo(filepath=SOURCE+'Empire_Imperial_Star_Destroyer_Triple_Turbolaser_'+suffix+'.ALO',importAnimations=False)
    rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
    rig.name=name+'Rig'
    RestoreRoot(rig,6)
    Export(name)
bpy.context.window.scene=bpy.data.scenes.new('VictoryIIAdvanced Preview')
bpy.ops.import_scene.fbx(filepath=OUTPUT+'/VictoryIIAdvanced.fbx',use_anim=False,use_custom_props=True,automatic_bone_orientation=False)
for obj in bpy.context.scene.objects:
    if obj.type=='ARMATURE': obj.hide_set(True)
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
