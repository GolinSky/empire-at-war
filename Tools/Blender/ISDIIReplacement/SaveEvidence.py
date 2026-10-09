"""Collect source hashes, conversion checks, saved Unity checks and previews."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

ROOT = Path('Temp/ISDIIReplacement')
OUTPUT = Path('output/ISDIIReplacement')
audit = json.loads((ROOT/'SourceAudit.json').read_text())
for path, digest in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == digest, path
previews = OUTPUT/'Previews'
previews.mkdir(parents=True, exist_ok=True)
for path in (ROOT/'Previews').glob('*.png'):
    if path.stem in {'Stripes','StripeOffset'}: continue
    shutil.copyfile(path,previews/path.name)
for name in ('Rear', 'RearTop', 'RearBottom'):
    shutil.copyfile(ROOT/'EngineInspection'/f'{name}.png', OUTPUT/f'Engines{name}.png')
team_images = [Image.open(previews/f'Team{i}.png').convert('RGBA') for i in range(8)]
assert len({hashlib.sha256(img.tobytes()).hexdigest() for img in team_images}) == 8
for i, image in enumerate(team_images):
    assert image.getchannel('A').getbbox() is not None, i
    if i: assert ImageChops.difference(team_images[0],image).convert('RGB').getbbox() is not None, i
conversion = [json.loads(p.read_text()) for p in sorted(ROOT.glob('*/ConversionReport.json'))]
evidence = dict(source_mod='1770851727',unit='Star_Destroyer_II',chain=audit['chain'],
    source_hashes=audit['source_hashes'],source_hashes_unchanged=True,attachments=audit['attachments'],
    conversions=[dict(model=r['variant'],geometry_error=r['geometry_error'],bone_error=r['bone_error']) for r in conversion],
    editable_blends=json.loads((ROOT/'VerifiedEditableBlends.json').read_text()),
    saved_unity=json.loads((ROOT/'VerifiedUnity.json').read_text()),
    obsolete=json.loads((ROOT/'ObsoleteVisuals.json').read_text()),team_palettes_verified=8,
    automated_tests_run=False,combat_play_mode_run=False)
if (ROOT/'FinalEditorChecks.json').exists():
    final=json.loads((ROOT/'FinalEditorChecks.json').read_text())
    assert not final['compiling'] and not final['playing'] and not final['dirty_prefabs']
    assert all(not shader['error'] for shader in final['shaders'])
    assert all(not scene['isDirty'] for scene in final['scenes'])
    evidence['final_editor']=final
    shutil.copyfile(ROOT/'FinalEditorChecks.json',OUTPUT/'FinalEditorChecks.json')
Path('Tools/Blender/ISDIIReplacement/ImportEvidence.json').write_text(json.dumps(evidence,indent=2))
shutil.copyfile(ROOT/'SourceAudit.json',OUTPUT/'SourceAudit.json')
shutil.copyfile(ROOT/'VerifiedUnity.json',OUTPUT/'VerifiedUnity.json')
print('Saved evidence:',len(conversion),'models,',len(audit['source_hashes']),'unchanged hashes, eight different team renders.')
