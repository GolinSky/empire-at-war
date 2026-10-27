"""Stage the verified Bulwark transport FBX and lossless PNG maps."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/BulwarkImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
assert json.loads((TASK / 'GeometryReport.json').read_text())['success']
for path, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == expected, path
models = ROOT / 'Assets/Art/Models/SeparatistShips/Bulwark'
textures = ROOT / 'Assets/Art/Textures/Models/SeparatistShips/Bulwark'
models.mkdir(parents=True, exist_ok=True)
textures.mkdir(parents=True, exist_ok=True)
for name in audit['models']:
    shutil.copy2(TASK / 'Output' / (name + '.fbx'), models / (name + '.fbx'))
for record in audit['textures'].values():
    for stem in [record['stem']] + ([record['team_mask']] if record.get('team_mask') else []):
        image = TASK / 'Output/Textures' / (stem + '.png')
        shutil.copy2(image, textures / image.name)
print('Staged Bulwark FBX and PNG maps; source ALO/DDS/XML hashes are unchanged.')
