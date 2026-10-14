from pathlib import Path
import json
rows=[]
r=json.loads(Path('Temp/TitanImport/ConversionReport.json').read_text());audit=json.loads(Path('Temp/TitanImport/SourceAudit.json').read_text())
for name,entry in r['models'].items():
 rows.append(dict(path=(Path('Temp/TitanImport')/name/(name+'.blend')).resolve().as_posix(),alo=audit['variants'][name]['file'],materials=[dict(name=m['name'],properties=m['source_properties']) for m in entry['materials']]))
code='import bpy,json\nassert bpy.types.blendermcp_server.port==9886\nrows='+repr(rows)+'\n'+'''for row in rows:
 bpy.ops.wm.open_mainfile(filepath=row['path'])
 for definition in row['materials']:
  bpy.data.materials[definition['name']]['EaWShaderParameters']=json.dumps(definition['properties'],sort_keys=True)
 bpy.context.scene['SourceWorkshop']='1770851727'
 bpy.context.scene['SourceALO']=row['alo']
 bpy.ops.wm.save_as_mainfile(filepath=row['path'])
print('EDITABLE_METADATA_COMPLETE',len(rows))
'''
Path('Temp/TitanImport/Metadata.request.json').write_text(json.dumps(dict(tool='execute_blender_code',arguments=dict(code=code,user_prompt='import Titan-class Battlecruiser - read about it in Empire Fleet Import note. read ALO_MODEL_IMPORT_GUIDE. hangar squadrons left disabled. run test - fix relate to this unit issues. commit after'))))
