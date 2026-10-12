"""Preserve the editable conversion and verify untouched source files."""
import hashlib
import json
import shutil
from pathlib import Path

task = Path('Temp/QuasarFireImport')
output = Path('output/eaw-rebel-ships/QuasarFire-Converted')
output.mkdir(parents=True, exist_ok=True)
audit = json.loads((task/'SourceAudit.json').read_text())
for filename, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(filename).read_bytes()).hexdigest() == expected, filename
for filename in ('QuasarFire.blend', 'QuasarFire.fbx'):
    shutil.copy2(task/'QuasarFire'/filename, output/filename)
shutil.copytree(task/'QuasarFire/Textures', output/'Textures', dirs_exist_ok=True)
shutil.copytree(task/'Previews', output/'Previews', dirs_exist_ok=True)
for filename in ('SourceAudit.json', 'ConversionReport.json', 'ArtReport.json', 'HardpointMapping.json', 'VerifiedRegistration.json'):
    shutil.copy2(task/filename, output/filename)
(output/'SourceIntegrity.json').write_text(json.dumps(dict(filesVerified=len(audit['source_hashes']), unchanged=True), indent=2))
print('Packaged Quasar Fire; unchanged source files:', len(audit['source_hashes']))
