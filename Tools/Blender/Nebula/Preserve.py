"""Preserve editable conversion outputs and verify source hashes and texture pixels."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/NebulaImport')
OUTPUT = Path('output/remake-rebel-units/Nebula-Converted')
EVIDENCE = Path('Tools/Blender/Nebula/Evidence')
audit = json.loads((TASK/'SourceAudit.json').read_text())
for source, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(source).read_bytes()).hexdigest() == expected, source
pixels = 0
for variant, model in audit['variants'].items():
    destination = OUTPUT/variant
    destination.mkdir(parents=True, exist_ok=True)
    for name in (variant+'.blend', variant+'.fbx', 'ConversionReport.json'):
        shutil.copy2(TASK/variant/name, destination/name)
    shutil.copytree(TASK/variant/'Textures', destination/'Textures', dirs_exist_ok=True)
    for texture in model['textures'].values():
        original = Image.open(texture['source']).convert('RGBA')
        copy = Image.open(TASK/variant/'Textures'/(texture['stem']+'.png')).convert('RGBA')
        staged = Image.open(Path('Assets/Art/Textures/Models/RebellionShips/Nebula')/(texture['stem']+'.png')).convert('RGBA')
        assert copy.size == original.size and copy.tobytes() == original.tobytes()
        assert staged.size == original.size and staged.tobytes() == original.tobytes()
        pixels += 1
for name in ('SourceAudit.json','ConversionReport.json','ArtReport.json','HardpointMapping.json','Verification.json'):
    EVIDENCE.mkdir(parents=True, exist_ok=True)
    shutil.copy2(TASK/name, EVIDENCE/name)
    shutil.copy2(TASK/name, OUTPUT/name)
shutil.copytree(TASK/'Previews', OUTPUT/'Previews', dirs_exist_ok=True)
shutil.copytree(TASK/'Previews', Path('Tools/Blender/Nebula/Previews'), dirs_exist_ok=True)
icon = Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/NebulaIcon.png').convert('RGBA')
box = icon.getchannel('A').getbbox()
assert icon.size == (512,512) and box and min(box[:2]) > 0 and max(box[2:]) < 512
team_deltas = []
for i in range(1,8):
    difference = ImageChops.difference(Image.open(TASK/'Previews/Team0.png').convert('RGB'),Image.open(TASK/f'Previews/Team{i}.png').convert('RGB'))
    assert difference.getbbox(), i
    team_deltas.append(difference.getbbox())
result = dict(source_hashes=len(audit['source_hashes']),pixel_identical_textures=pixels,icon_alpha_bounds=box,team_differences=team_deltas)
(EVIDENCE/'SourceVerification.json').write_text(json.dumps(result, indent=2))
(OUTPUT/'SourceVerification.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result))
