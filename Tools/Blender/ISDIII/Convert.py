import bpy, json
assert bpy.types.blendermcp_server.port == 9883, 'Use the isolated ISD III MCP session.'
from mathutils import Vector

OUTPUT = 'F:/Private/empire-at-war/Temp/ISDIIIImport/Output'
SOURCE = 'D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/'


def Clear():
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for material in list(bpy.data.materials):
        if material.users == 0:
            bpy.data.materials.remove(material)
    for mesh in list(bpy.data.meshes):
        if mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for armature in list(bpy.data.armatures):
        if armature.users == 0:
            bpy.data.armatures.remove(armature)


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
        if base=='Hangar.dds':
            base='Hangar.tga'
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
            matches=[i for i in bpy.data.images if i.name.lower()==filename.lower()]
            image=matches[0] if matches else bpy.data.images.load(SOURCE.replace('MODELS/', 'TEXTURES/')+filename)
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
        if before['meshes'][key]['triangles']!=after['meshes'][key]['triangles']:
            print('TRIANGLE_CHANGE', key, before['meshes'][key]['triangles'], after['meshes'][key]['triangles'])
            assert key=='Collision' and before['meshes'][key]['hidden'],key
        assert before['meshes'][key]['uv_layers']==after['meshes'][key]['uv_layers'],key
    error=max((Vector(before['bones'][k]['head'])-Vector(after['bones'][k]['head'])).length for k in before['bones'])
    assert error < .001,error
    bounds_error=max(abs(before[k][i]-after[k][i]) for k in ('bounds_min','bounds_max') for i in range(3))
    assert bounds_error < .001,bounds_error
    report = dict(name=name,source=before,roundtrip=after,materials=materials,bone_error=error,bounds_error=bounds_error)
    reports.append(report)
    print(name, 'meshes', len(before['meshes']), 'triangles', sum(m['triangles'] for m in before['meshes'].values()), 'bones', len(before['bones']), 'bone_error', error, 'bounds_error', bounds_error)
    Clear()
    bpy.context.window.scene=scene
    bpy.data.scenes.remove(validation)



reports = []
models = json.loads('{"ISDIII": {"file": "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1397421866\\\\Data\\\\ART\\\\MODELS\\\\EV_ISD3.ALO", "bones": 202, "root": {"name": "Root", "parent_index": 4294967295, "visible": 1, "matrix": [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0]}}, "ISDIIIHeavyDualTurret": {"file": "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1397421866\\\\Data\\\\ART\\\\MODELS\\\\EV_ISD3_TURRET_01.ALO", "bones": 8, "root": {"name": "Root", "parent_index": 4294967295, "visible": 1, "matrix": [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0]}}, "ISDIIIMediumTripleTurret": {"file": "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1397421866\\\\Data\\\\ART\\\\MODELS\\\\EV_ISD1_Triple_TL12.ALO", "bones": 7, "root": {"name": "Root", "parent_index": 4294967295, "visible": 1, "matrix": [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0]}}, "ISDIIIIonTurret": {"file": "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1397421866\\\\Data\\\\ART\\\\MODELS\\\\Empire_Imperial_SD_ICQ_01.ALO", "bones": 11, "root": {"name": "Root", "parent_index": 4294967295, "visible": 1, "matrix": [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0]}}, "ISDIIICompositeTurret": {"file": "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1397421866\\\\Data\\\\ART\\\\MODELS\\\\EV_ISD3_PROTON_TURRET_00.ALO", "bones": 9, "root": {"name": "Root", "parent_index": 4294967295, "visible": 1, "matrix": [1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0]}}}')
for name, source in models.items():
    Clear()
    bpy.context.scene.name = name + ' Source'
    root = source['root']
    assert root['name'] == 'Root' and root['parent_index'] == 4294967295
    assert root['matrix'] == [1,0,0,0,0,1,0,0,0,0,1,0]
    bpy.ops.import_mesh.alo(filepath=source['file'], importAnimations=False)
    rig = next(o for o in bpy.context.scene.objects if o.type == 'ARMATURE')
    rig.name = name + 'Rig'
    RestoreRoot(rig, source['bones'])
    Export(name)
print('CONVERSION_JSON ' + json.dumps(reports))
