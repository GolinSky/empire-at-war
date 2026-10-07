"""Bootstrap an isolated Blender session; shared MCP port 9876 stays untouched."""
import bpy
import blender_mcp

if hasattr(bpy.types, 'blendermcp_server'):
    bpy.types.blendermcp_server.stop()
bpy.context.scene.blendermcp_port = 9885
bpy.types.blendermcp_server = blender_mcp.BlenderMCPServer(host='127.0.0.1', port=9885)
bpy.types.blendermcp_server.start()
