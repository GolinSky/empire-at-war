"""Copy verified editable sources, FBXs, PNGs and evidence into a durable local package."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/AotrEmpressStationImport'
OUTPUT = ROOT / 'output/aotr-space-stations/Empress-Converted'
reports = json.loads((TASK / 'ConversionReport.json').read_text())
assert json.loads((TASK / 'VerifiedRegistration.json').read_text())['success']
assert json.loads((TASK / 'VerifiedSourceGeometry.json').read_text())['success']
assert len(json.loads((TASK / 'VerifiedEditableBlends.json').read_text())) == len(reports)
files = [TASK / 'Output' / (r['name'] + extension) for r in reports for extension in ('.fbx','.blend')]
files += list((TASK / 'Output/Textures').glob('*.png'))
files += list((TASK / 'Previews').glob('*.png'))
files += [TASK / name for name in ('SourceAudit.json','ConversionReport.json','VerifiedRegistration.json','VerifiedSourceGeometry.json','VerifiedEditableBlends.json','VerifiedTeamRenders.json','ArtReport.json','GameplayMounts.json')]
OUTPUT.mkdir(parents=True, exist_ok=True)
hashes = {}
for source in files:
    relative = source.relative_to(TASK / 'Output') if source.is_relative_to(TASK / 'Output') else source.relative_to(TASK)
    target = OUTPUT / relative
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(source,target)
    digest = hashlib.sha256(source.read_bytes()).hexdigest()
    assert hashlib.sha256(target.read_bytes()).hexdigest() == digest
    hashes[str(relative)] = digest
shutil.copy2(ROOT / 'Tools/Blender/AotrEmpressStation/README.md',OUTPUT / 'README.md')
(OUTPUT / 'Manifest.json').write_text(json.dumps(hashes,indent=2))
print('Packaged',len(reports),'editable blends/FBXs, PNG textures, Unity previews and verification evidence at',OUTPUT)
