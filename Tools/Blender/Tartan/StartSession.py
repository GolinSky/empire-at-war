import bpy

if hasattr(bpy.types, 'blendermcp_server'):
    bpy.types.blendermcp_server.stop()
    del bpy.types.blendermcp_server
bpy.context.scene.blendermcp_port = 9885
bpy.ops.blendermcp.start_server()
