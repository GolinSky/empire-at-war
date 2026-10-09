"""Store the audited source parameters in the isolated Blender conversions."""
import json,subprocess
from pathlib import Path
TASK=Path('Temp/ISDIRemakeImport')
manifest=json.loads((TASK/'ArtManifest.json').read_text())
audit=json.loads((TASK/'SourceAudit.json').read_text())
rows=[dict(path=(TASK/name/(name+'.blend')).resolve().as_posix(),alo=audit['variants'][name]['file'],materials=[dict(name=m['name'],properties=m['source_properties']) for m in entry['materials']]) for name,entry in manifest.items()]
code='import bpy,json\nassert bpy.types.blendermcp_server.port==9886\nrows='+repr(rows)+"\n"+"""for row in rows:
 bpy.ops.wm.open_mainfile(filepath=row['path'])
 for definition in row['materials']:
  bpy.data.materials[definition['name']]['EaWShaderParameters']=json.dumps(definition['properties'],sort_keys=True)
 bpy.context.scene['SourceWorkshop']='1770851727'
 bpy.context.scene['SourceALO']=row['alo']
 bpy.ops.wm.save_as_mainfile(filepath=row['path'])
print('EDITABLE_METADATA_COMPLETE',len(rows))
"""
request=TASK/'Metadata.request.json'
request.write_text(json.dumps(dict(tool='execute_blender_code',arguments=dict(code=code,user_prompt='Preserve source shader parameters in the editable ISD I conversions.'))))
subprocess.run(['uvx','--from','mcp-for-blender==2.1.3','python','Tools/Blender/ISDIRemake/Call.py',str(request)],check=True)
