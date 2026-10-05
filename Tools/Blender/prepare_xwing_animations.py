"""Decode the vanilla 18-byte ALA header and stage Blender S-Foils actions.

Run prepare_xwing.py first. Execute the generated ImportAnimations.py through
Blender MCP only on a fresh RV_XWING.ALO import in the XWing Source scene.
"""
from pathlib import Path
import struct,json

def chunks(data):
    position=0
    while position<len(data):
        kind,size=struct.unpack_from('<II',data,position)
        length=size&0x7fffffff
        yield kind,data[position+8:position+8+length]
        position+=8+length
    assert position==len(data)

def minis(data):
    position=0
    result={}
    while position<len(data):
        kind,length=struct.unpack_from('<BB',data,position)
        result[kind]=data[position+2:position+2+length]
        position+=2+length
    assert position==len(data)
    return result

animations={}
root=json.loads(Path('Temp/XWingImport/Output/BinaryBones.json').read_text())[0]
assert root['name']=='Root' and root['parent_index']==4294967295
assert root['matrix']==[1,0,0,0,0,1,0,0,0,0,1,0]
for name in ['Deploy','Undeploy']:
    path=Path('output/eaw-rebel-ships/DATA/ART/MODELS/RV_XWING_'+name.upper()+'_00.ALA')
    root=list(chunks(path.read_bytes()))
    assert root[0][0]==0x1000
    content=list(chunks(root[0][1]))
    header=minis(content[0][1])
    assert len(content[0][1])==18
    frames=struct.unpack('<I',header[1])[0]
    fps=struct.unpack('<f',header[2])[0]
    count=struct.unpack('<I',header[3])[0]
    tracks=[]
    names=[]
    for kind,payload in content[1:]:
        assert kind==0x1002
        pieces=dict(chunks(payload))
        bone=minis(pieces[0x1003])
        original=bone[4].rstrip(b'\0').decode('ascii')
        unique=original if original not in names else original+'.001'
        names.append(unique)
        offset=struct.unpack('<3f',bone[6])
        scale=struct.unpack('<3f',bone[7])
        rotations=[]
        positions=[]
        for frame in range(frames):
            index=0 if len(pieces[0x1006])==8 else frame*8
            x,y,z,w=struct.unpack_from('<4h',pieces[0x1006],index)
            rotations.append([w/32767,x/32767,y/32767,z/32767])
            positions.append([offset[i]+struct.unpack_from('<3H',pieces[0x1004],frame*6)[i]*scale[i] for i in range(3)] if 0x1004 in pieces else list(offset))
        tracks.append(dict(name=unique,positions=positions,rotations=rotations))
    assert len(tracks)==count
    animations[name]=dict(frames=frames,fps=fps,tracks=tracks)
Path('Temp/XWingImport/Output/Animations.json').write_text(json.dumps(animations))
code="import bpy,json\nfrom mathutils import Matrix, Quaternion, Vector\nanimations=json.loads("+repr(json.dumps(animations))+ ")\n"
code+="""rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
assert len(rig.data.bones)==13
rig.name='XWingRig'
bpy.context.view_layer.objects.active=rig
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
root=rig.data.edit_bones.new('Root')
root.head=(0,0,0)
root.tail=(0,1,0)
for bone in rig.data.edit_bones:
    if bone!=root and bone.parent is None:
        bone.parent=root
bpy.ops.object.mode_set(mode='OBJECT')
rig.animation_data_create()
scene=bpy.context.scene
for name,animation in animations.items():
    action=bpy.data.actions.new(name)
    action.use_fake_user=True
    rig.animation_data.action=action
    scene.render.fps=int(animation['fps'])
    for frame in range(animation['frames']):
        scene.frame_set(frame)
        for track in animation['tracks']:
            pose=rig.pose.bones[track['name']]
            matrix=Matrix.Translation(Vector(track['positions'][frame]))@Quaternion(track['rotations'][frame]).to_matrix().to_4x4()
            pose.matrix=pose.parent.matrix@matrix
            pose.rotation_mode='QUATERNION'
            pose.keyframe_insert(data_path='location')
            pose.keyframe_insert(data_path='rotation_quaternion')
        bpy.context.view_layer.update()
    for curve in action.fcurves:
        for key in curve.keyframe_points:
            key.interpolation='LINEAR'
rig.animation_data.action=bpy.data.actions['Deploy']
scene.frame_start=0
scene.frame_end=30
scene.frame_set(30)
bpy.context.view_layer.update()
print(json.dumps({'actions':[(a.name,list(a.frame_range)) for a in bpy.data.actions],'wing_poses':{b.name:{'head':list(b.head),'quaternion':list(b.rotation_quaternion)} for b in rig.pose.bones if b.name.startswith('bone_wing')}}))
"""
Path('Temp/XWingImport/ImportAnimations.py').write_text(code)
