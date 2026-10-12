"""Preserve editable conversions and verification evidence outside Temp."""
import hashlib
import json
import shutil
from pathlib import Path

task = Path('Temp/LiberatorImport')
destination = Path('output/remake-rebel-units/Liberator-Converted')
destination.mkdir(parents=True, exist_ok=True)
audit = json.loads((task/'SourceAudit.json').read_text())
for name, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(name).read_bytes()).hexdigest() == expected, name
for source in task.glob('*/Liberator*.blend'):
    folder = destination/source.parent.name
    folder.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, folder/source.name)
    shutil.copy2(source.parent/'ConversionReport.json', folder/'ConversionReport.json')
for name in ('SourceAudit.json', 'ArtManifest.json', 'ArtBounds.json', 'HardpointMapping.json', 'Verification.json'):
    shutil.copy2(task/name, destination/name)
shutil.copytree(task/'Previews', destination/'Previews', dirs_exist_ok=True)
verification = json.loads((task/'Verification.json').read_text())
verification['sourceHashesUnchanged'] = True
verification['automatedUnityTestsRun'] = False
verification['playModeVerified'] = False
Path('Tools/Blender/Liberator/ImportEvidence.json').write_text(json.dumps(verification, indent=2))
print('Preserved sixteen editable Blender files; all source hashes unchanged.')
