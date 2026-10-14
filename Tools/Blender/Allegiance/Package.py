"""Retain editable conversion files, previews, and verified import evidence."""
import hashlib
import json
import shutil
from pathlib import Path

task = Path('Temp/AllegianceImport')
evidence = Path('Tools/Blender/Allegiance/Evidence')
output = Path('output/remake-empire-units/Allegiance-Converted')
audit = json.loads((task / 'SourceAudit.json').read_text())
for source, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(source).read_bytes()).hexdigest() == expected, source
evidence.mkdir(parents=True, exist_ok=True)
output.mkdir(parents=True, exist_ok=True)
for name in ['SourceAudit', 'ConversionReport', 'GeometryVerification', 'HardpointMapping',
             'GameplayBounds', 'VerificationResult', 'UnitTests']:
    shutil.copy2(task / (name + '.json'), evidence / (name + '.json'))
for path in (task / 'Output').glob('*'):
    if path.is_file():
        shutil.copy2(path, output / path.name)
shutil.copytree(task / 'Output/Textures', output / 'Textures', dirs_exist_ok=True)
shutil.copytree(task / 'Previews', output / 'Previews', dirs_exist_ok=True)
shutil.copytree(evidence, output / 'Evidence', dirs_exist_ok=True)
print('Original sources unchanged; packed blends, FBXs, textures and evidence retained at', output)
