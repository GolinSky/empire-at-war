"""Stage the verified Invincible transport FBX and lossless PNG maps."""
import hashlib
import json
from pathlib import Path
import shutil

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/InvincibleImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
assert json.loads((TASK / 'GeometryReport.json').read_text())['success']
for path, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == expected, path
models = ROOT / 'Assets/Art/Models/SeparatistShips/Invincible'
textures = ROOT / 'Assets/Art/Textures/Models/SeparatistShips/Invincible'
models.mkdir(parents=True, exist_ok=True)
textures.mkdir(parents=True, exist_ok=True)
shutil.copy2(TASK / 'Output/Invincible.fbx', models / 'Invincible.fbx')
for image in (TASK / 'Output/Textures').glob('*.png'):
    shutil.copy2(image, textures / image.name)
print('Staged Invincible FBX and PNG maps; source ALO/DDS/XML hashes are unchanged.')
