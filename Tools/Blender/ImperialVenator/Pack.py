"""Keep editable conversion outputs outside Unity's runtime assets."""
import hashlib
import json
import os
import subprocess
import zipfile
from pathlib import Path

ROOT = Path('Temp/ImperialVenatorImport')
audit = json.loads((ROOT / 'SourceAudit.json').read_text())
target = Path('output/ImperialVenator/ImperialVenator-Converted.zip')
clean = ROOT / 'CleanBlends'
clean.mkdir(exist_ok=True)
blender = Path(os.environ['LOCALAPPDATA']) / 'AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe'
for name in audit['variants']:
    result = subprocess.run([str(blender), '-b', str((ROOT / name / (name + '.blend')).resolve()), '--python',
                             str(Path('Tools/Blender/ImperialVenator/TrimBlend.py').resolve()), '--', name,
                             str((clean / (name + '.blend')).resolve())], capture_output=True, text=True)
    (clean / (name + '.log')).write_text(result.stdout + result.stderr)
    assert result.returncode == 0 and 'TRIM_VERIFIED' in result.stdout, name
    print(name, 'packed source blend', (clean / (name + '.blend')).stat().st_size, 'bytes', flush=True)
with zipfile.ZipFile(target, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for name in audit['variants']:
        for path in sorted((ROOT / name).rglob('*')):
            if path.is_file() and path.suffix in ('.blend', '.fbx', '.png', '.json'):
                archive.write(clean / path.name if path.suffix == '.blend' else path, path.relative_to(ROOT))
    for name in ('SourceAudit.json', 'BinaryMaterialAudit.json', 'ArtBounds.json', 'GameplayMounts.json', 'UnityGeometry.json', 'Verification.json', 'EngineInspection.json'):
        archive.write(ROOT / name, 'Evidence/' + name)
    archive.write('Tools/Blender/ImperialVenator/README.md', 'README.md')
    for path in sorted(Path('output/ImperialVenator/Previews').glob('*.png')):
        archive.write(path, 'Previews/' + path.name)
with zipfile.ZipFile(target) as archive:
    assert archive.testzip() is None
    assert len([n for n in archive.namelist() if n.endswith('.blend')]) == 12
print(str(target), target.stat().st_size, 'bytes; 12 editable blends and FBXs; archive CRC verified.')
Path(str(target) + '.sha256').write_text(hashlib.sha256(target.read_bytes()).hexdigest() + '  ' + target.name + '\n')
