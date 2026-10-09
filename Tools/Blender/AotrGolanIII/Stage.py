"""Stage only verified Golan III FBXs and losslessly decoded textures."""
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/AotrGolanIIIImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
reports = json.loads((TASK / 'ConversionReport.json').read_text())
assert {r['name'] for r in reports} == set(audit['models'])
model_dir = ROOT / 'Assets/Art/Models/SpaceStations/AotrGolanIII'
texture_dir = ROOT / 'Assets/Art/Textures/Models/SpaceStations/AotrGolanIII'
material_dir = ROOT / 'Assets/Art/Materials/Models/SpaceStations/AotrGolanIII'
for directory in (model_dir,texture_dir,material_dir):
    directory.mkdir(parents=True,exist_ok=True)
for report in reports:
    shutil.copy2(TASK / 'Output' / (report['name']+'.fbx'), model_dir / (report['name']+'.fbx'))
for path in (TASK / 'Output/Textures').glob('*.png'):
    shutil.copy2(path, texture_dir / path.name)
print('Staged',len(reports),'FBXs and',len(list(texture_dir.glob('*.png'))),'PNGs')
