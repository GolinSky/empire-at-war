"""Stage the dedicated, Blender-verified Centax art in type-first Unity folders."""
from pathlib import Path
import shutil
import json

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/CentaxImport'
OUTPUT = TASK / 'Output'
MODELS = ROOT / 'Assets/Art/Models/RepublicShips/Centax'
TEXTURES = ROOT / 'Assets/Art/Textures/Models/RepublicShips/Centax'
MODELS.mkdir(parents=True, exist_ok=True)
TEXTURES.mkdir(parents=True, exist_ok=True)
for name in ['Centax', 'CentaxWreck']:
    shutil.copy2(OUTPUT / (name+'.fbx'), MODELS / (name+'.fbx'))
report = json.loads((TASK / 'ConversionReport.json').read_text())
names = {row[key] + '.png' for row in report['materials'] for key in ('base', 'normal', 'mask') if row[key]}
for name in names:
    shutil.copy2(OUTPUT / 'Textures' / name, TEXTURES / name)
print('Staged Centax FBX and audited textures')
