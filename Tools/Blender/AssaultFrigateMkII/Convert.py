"""Generate and run a safe-mode MCP conversion using the verified round-trip workflow."""
import json
import re
import subprocess
from pathlib import Path

NAME = 'AssaultFrigateMkII'
TASK = Path('Temp/AssaultFrigateMkIIImport')
audit = json.loads((TASK/'SourceAudit.json').read_text())
functions = Path('Tools/Blender/MC80Independence/Convert.py').read_text()
functions = functions[functions.index('def Snapshot'):functions.index('assert bpy.types.blendermcp_server.port')]
body = Path('Tools/Blender/CorellianBattlecruiser/Convert.py').read_text().replace('CorellianBattlecruiser', NAME)
start = body.index('for stale in list(bpy.data.scenes):')
end = body.index('materials = {}')
body = body[:start]+body[end:]
start = body.index('    if index==0:')
end = body.index('    bpy.context.window.scene = scene', start+20)
# Replace the complete first-import branch and retain the verified borrowed-object import.
end = body.index('    bpy.context.window.scene = scene\n    rig', start)
branch = '''    scene = bpy.data.scenes.new(model_name+' Source')
    bpy.context.window.scene = scene
    borrowed = [(o,o.hide_render,o.hide_viewport,o.hide_get()) for o in list(bpy.data.objects)]
    for obj,hidden,viewport,view_hidden in borrowed:
        scene.collection.objects.link(obj)
    try:
        bpy.ops.import_mesh.alo(filepath=model['file'],importAnimations=False)
    finally:
        for obj,hidden,viewport,view_hidden in borrowed:
            obj.hide_render,obj.hide_viewport=hidden,viewport
            scene.collection.objects.unlink(obj)
'''
body = body[:start]+branch+body[end:]
body = body.replace("audit['models']", "audit['variants']")
body = body.replace("copy=True)", "copy=True,compress=True)")
body = body.replace("bpy.data.scenes[NAME+' Source']", "bpy.data.scenes[reports and next(iter(reports))+' Source']")
code = 'import bpy, math, itertools\nfrom mathutils import Vector\nassert bpy.app.version_string == "3.6.23"\n'+functions
code += '\naudit = '+repr(audit)+'\nbinary = '+repr(json.loads((TASK/'BinaryMaterialAudit.json').read_text()))+'\n'+body
request = TASK/'ConvertRequest.json'
request.write_text(json.dumps(dict(tool='execute_blender_code',arguments=dict(code=code,user_prompt='import Assault Frigate Mk II; read Rebel units and ALO_MODEL_IMPORT_GUIDE; REMAKE 4.0 Workshop 1770851727.'))))
result = subprocess.run(['uvx','--from','mcp-for-blender==2.1.3','python','Tools/Blender/mcp_call.py',str(request)],capture_output=True,text=True,encoding='utf-8')
(TASK/'Conversion.log').write_text(result.stdout+'\n'+result.stderr)
match = re.search(r'CONVERSION_JSON (.+)',result.stdout)
if result.returncode or match is None:
    raise RuntimeError(result.stdout[-4500:]+result.stderr[-1000:])
report = json.loads(match.group(1))
(TASK/'ConversionReport.json').write_text(json.dumps(report,indent=2))
print('Converted',len(report['models']),'models; maximum geometry error',max(v['geometry_error'] for v in report['models'].values()))
