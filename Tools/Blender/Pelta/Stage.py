"""Stage the dedicated, Blender-verified Pelta art in type-first Unity folders."""
from pathlib import Path
import shutil
import json

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/PeltaImport'
OUTPUT = TASK / 'Output'
MODELS = ROOT / 'Assets/Art/Models/RepublicShips/Pelta'
TEXTURES = ROOT / 'Assets/Art/Textures/Models/RepublicShips/Pelta'
MODELS.mkdir(parents=True, exist_ok=True)
TEXTURES.mkdir(parents=True, exist_ok=True)
shutil.copy2(OUTPUT / 'Pelta.fbx', MODELS / 'Pelta.fbx')
report = json.loads((TASK / 'ConversionReport.json').read_text())
names = {row[key] + '.png' for row in report['materials'] for key in ('base', 'normal', 'mask') if row[key]}
for name in names:
    shutil.copy2(OUTPUT / 'Textures' / name, TEXTURES / name)
print('Staged RaW Pelta FBX and its four audited textures')
