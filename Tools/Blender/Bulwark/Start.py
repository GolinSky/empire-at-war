"""Start this import's MCP listener without touching the shared Blender process."""
import bpy
import blender_mcp

for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)

if hasattr(bpy.types, 'blendermcp_server'):
    bpy.types.blendermcp_server.stop()
bpy.context.scene.blendermcp_port = 9898
bpy.types.blendermcp_server = blender_mcp.BlenderMCPServer(host='127.0.0.1', port=9898)
bpy.types.blendermcp_server.start()
