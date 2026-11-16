import bpy, math
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='F:/Private/empire-at-war/Temp/GinivexImport/Ginivex.blend')
scene=bpy.context.scene
for obj in scene.objects:
 if obj.type=='MESH': obj.hide_render=obj.name not in ('Hull','Object01','Box01','Box02')
scene.render.engine='BLENDER_EEVEE'
scene.render.resolution_x=512;scene.render.resolution_y=512;scene.render.resolution_percentage=100
scene.render.film_transparent=True
scene.render.image_settings.file_format='PNG';scene.render.image_settings.color_mode='RGBA'
scene.view_settings.view_transform='Standard';scene.view_settings.look='Medium High Contrast';scene.view_settings.exposure=0;scene.view_settings.gamma=1
scene.world.color=(0.3,0.3,0.3)
for pos,power,size in [((100,-180,220),800000,180),((-180,-100,80),500000,150),((60,200,80),700000,120)]:
 data=bpy.data.lights.new('Key','AREA');data.energy=power;data.size=size
 obj=bpy.data.objects.new('Key',data);scene.collection.objects.link(obj);obj.location=pos;obj.rotation_euler=(Vector((0,25,0))-obj.location).to_track_quat('-Z','Y').to_euler()
data=bpy.data.cameras.new('IconCamera');cam=bpy.data.objects.new('IconCamera',data);scene.collection.objects.link(cam);scene.camera=cam
cam.location=(260,-320,190);cam.rotation_euler=(Vector((0,25,0))-cam.location).to_track_quat('-Z','Y').to_euler();data.type='ORTHO';data.ortho_scale=200
scene.render.filepath='F:/Private/empire-at-war/Temp/GinivexImport/GinivexIcon.png'
bpy.ops.render.render(write_still=True)
