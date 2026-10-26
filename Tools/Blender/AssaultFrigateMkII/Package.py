"""Verify unchanged sources, icon alpha and all eight palettes; retain import evidence."""
import hashlib
import json
import shutil
import struct
from pathlib import Path
from PIL import Image

NAME = 'AssaultFrigateMkII'
TASK = Path('Temp/AssaultFrigateMkIIImport')
OUTPUT = Path('output/remake-rebel-units/AssaultFrigateMkII-Converted')
audit = json.loads((TASK/'SourceAudit.json').read_text())
for filename, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(filename).read_bytes()).hexdigest() == expected, filename
conversion = json.loads((TASK/'ConversionReport.json').read_text())
verification = json.loads((TASK/'VerificationReport.json').read_text())
assert len(conversion['models']) == 15
icon = Path('Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/AssaultFrigateMkIIIcon.png')
image = Image.open(icon).convert('RGBA')
assert image.size == (512,512)
alpha = image.getchannel('A')
assert alpha.getextrema() == (0,255)
bounds = alpha.getbbox()
assert bounds and bounds[0]>4 and bounds[1]>4 and bounds[2]<508 and bounds[3]<508, bounds
palettes = {}
for prefix in ('Team','WreckTeam'):
    images = [Image.open(TASK/'Previews'/f'{prefix}{index}.png').convert('RGBA') for index in range(8)]
    assert len({hashlib.sha256(item.tobytes()).hexdigest() for item in images}) == 8
    original = images[0].tobytes()
    changed = [sum(original[i:i+4] != pixels[i:i+4] for i in range(0,len(original),4)) for pixels in (item.tobytes() for item in images[1:])]
    assert min(changed)>100, changed
    palettes[prefix] = dict(distinctRenders=8,minimumChangedPixels=min(changed))
for directory in ('Textures','Previews'):
    shutil.copytree(TASK/directory,OUTPUT/directory,dirs_exist_ok=True)
for path in list(TASK.glob('*.fbx'))+[TASK/name for name in ('SourceAudit.json','BinaryMaterialAudit.json','ConversionReport.json','ArtReport.json','HardpointMapping.json','VerificationReport.json')]:
    OUTPUT.mkdir(parents=True,exist_ok=True)
    shutil.copy2(path,OUTPUT/path.name)
# The final saved file contains all source scenes; inspect its binary scene records without loading code.
editable = TASK/(NAME+'Part13.blend')
raw = editable.read_bytes()
assert raw[:7] == b'BLENDER'
pointer = 8 if raw[7:8] == b'-' else 4
endian = '<' if raw[8:9] == b'v' else '>'
header = struct.Struct(endian+'4sI'+('Q' if pointer==8 else 'I')+'II')
offset, scenes = 12, []
while offset+header.size <= len(raw):
    code, size, address, dna, count = header.unpack_from(raw,offset)
    offset += header.size
    if code == b'SC\x00\x00':
        # Blender 3.6 ID stores next, prev, newid, lib and asset_data before name[66].
        scenes.append(raw[offset+pointer*5:offset+pointer*5+66].split(b'\x00',1)[0].decode('utf-8')[2:])
    offset += size
assert all(model+' Source' in scenes for model in conversion['models']), scenes
shutil.copy2(editable,OUTPUT/(NAME+'.blend'))
shutil.copy2(icon,OUTPUT/icon.name)
summary = dict(sourceHashesVerified=len(audit['source_hashes']),editableSourceScenesVerified=15,iconAlphaBounds=list(bounds),palettes=palettes,unity=verification['unityGeometry'],automatedTestsRun=False,playModeRun=False)
(OUTPUT/'PackageVerification.json').write_text(json.dumps(summary,indent=2))
shutil.copy2('Tools/Blender/AssaultFrigateMkII/README.md',OUTPUT/'README.md')
print(json.dumps(summary,indent=2))
