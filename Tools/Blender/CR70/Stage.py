"""Stage the dedicated, Blender-verified Corvette art in type-first Unity folders."""
from pathlib import Path
import shutil
import json

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/CR70ChargerImport'
OUTPUT = TASK / 'Output'
MODELS = ROOT / 'Assets/Art/Models/RepublicShips/CR70'
TEXTURES = ROOT / 'Assets/Art/Textures/Models/RepublicShips/CR70'
MODELS.mkdir(parents=True, exist_ok=True)
TEXTURES.mkdir(parents=True, exist_ok=True)
shutil.copy2(OUTPUT / 'CR70.fbx', MODELS / 'CR70.fbx')
report = json.loads((TASK / 'ConversionReport.json').read_text())
names = {row[key] + '.png' for row in report['materials'] for key in ('base', 'normal', 'mask') if row[key]}
for name in names:
    shutil.copy2(OUTPUT / 'Textures' / name, TEXTURES / name)
print('Staged RaW Charger FBX and its four audited textures')
