"""Stage only the verified FBX and referenced textures; keep existing GUIDs."""
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/HeavyDreadnoughtImport'
shutil.copy2(TASK / 'Output/HeavyDreadnought.fbx', ROOT / 'Assets/Art/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought.fbx')
report = json.loads((TASK / 'ConversionReport.json').read_text())
for name in {row[key] for row in report['materials'] for key in ('base', 'normal') if row[key]}:
    shutil.copy2(TASK / 'Output/Textures' / (name + '.png'), ROOT / 'Assets/Art/Textures/Models/RepublicShips/HeavyDreadnought' / (name + '.png'))
print('Staged verified Dreadnaught FBX and three referenced PNG textures.')
