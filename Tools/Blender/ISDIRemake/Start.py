"""Bootstrap the isolated conversion process, leaving the shared Blender untouched."""
import bpy
import blender_mcp

if hasattr(bpy.types, 'blendermcp_server'):
    bpy.types.blendermcp_server.stop()
bpy.context.scene.blendermcp_port = 9886
bpy.types.blendermcp_server = blender_mcp.BlenderMCPServer(host='127.0.0.1', port=9886)
bpy.types.blendermcp_server.start()
