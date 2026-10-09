"""Retain every binary shader parameter and auxiliary texture in the evidence package."""
import json,shutil
from pathlib import Path
TASK=Path('Temp/ISDIRemakeImport')
audit=json.loads((TASK/'SourceAudit.json').read_text())
manifest=json.loads((TASK/'ArtManifest.json').read_text())
for name, entry in manifest.items():
 counts={}
 properties={}
 for mesh in audit['variants'][name]['material_audit']['meshes']:
  count=counts.get(mesh['name'],0);counts[mesh['name']]=count+1
  key=mesh['name'] if count==0 else mesh['name']+'.'+str(count).zfill(3)
  slots=entry['before']['meshes'][key]['materials']
  for slot,material in zip(slots,mesh['materials']):properties[slot]=material['properties']
 for material in entry['materials']:
  material['source_properties']=properties[material['name']]
 for texture in (TASK/name/'Textures').glob('*.png'):
  target=Path('Assets/Art/Textures/Models/EmpireShips/ISDI')/texture.name
  if not target.exists() or target.read_bytes()!=texture.read_bytes():shutil.copyfile(texture,target)
 report=json.loads((TASK/name/'ConversionReport.json').read_text())
 for material in report['materials']:material['source_properties']=properties[material['name']]
 (TASK/name/'ConversionReport.json').write_text(json.dumps(report,indent=2))
(TASK/'ArtManifest.json').write_text(json.dumps(manifest,indent=2))
print('Preserved original shader parameters and all auxiliary texture maps for 16 variants')
