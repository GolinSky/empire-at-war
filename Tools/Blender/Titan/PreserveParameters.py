import json
from pathlib import Path
p=Path('Temp/TitanImport/ConversionReport.json');r=json.loads(p.read_text());audit=json.loads(Path('Temp/TitanImport/SourceAudit.json').read_text())
for variant,entry in r['models'].items():
 counts={};props={}
 for mesh in audit['variants'][variant]['material_audit']['meshes']:
  index=counts.get(mesh['name'],0);counts[mesh['name']]=index+1
  key=mesh['name'] if index==0 else mesh['name']+'.'+str(index).zfill(3)
  for slot,definition in zip(entry['before']['meshes'][key]['materials'],mesh['materials']):props[slot]=definition['properties']
 for m in entry['materials']:m['source_properties']=props[m['name']]
 for m in r['materials']:
  if m['name'] in props:m['source_properties']=props[m['name']]
p.write_text(json.dumps(r,indent=2))
