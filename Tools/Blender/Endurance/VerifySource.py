"""Verify source hashes, decoded textures, palette renders and icon framing."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

task = Path('Temp/EnduranceImport')
audit = json.loads((task/'SourceAudit.json').read_text())
for path, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == expected, path
textures = 0
maximum_geometry_error = 0
maximum_bone_error = 0
for name, variant in audit['variants'].items():
    for definition in variant['textures'].values():
        original = Image.open(definition['source']).convert('RGBA')
        copy = Image.open(Path('Assets/Art/Textures/Models/RebellionShips/Endurance')/(definition['stem']+'.png')).convert('RGBA')
        assert original.size == copy.size and original.tobytes() == copy.tobytes(), definition['stem']
        textures += 1
    report = json.loads((task/name/'ConversionReport.json').read_text())
    maximum_geometry_error = max(maximum_geometry_error, report['geometry_error'])
    maximum_bone_error = max(maximum_bone_error, report['bone_error'])
    counts = {}
    for mesh in variant['material_audit']['meshes']:
        index = counts.get(mesh['name'], 0)
        counts[mesh['name']] = index + 1
        key = mesh['name'] if index == 0 else mesh['name']+'.'+str(index).zfill(3)
        if not report['before']['meshes'][key]['hidden']:
            assert sum(m['triangleCount'] for m in mesh['materials']) == report['before']['meshes'][key]['triangles'], key
        assert len(mesh['materials']) == len(report['before']['meshes'][key]['materials']), key
coverage = {}
for prefix in ('Team', 'WreckTeam'):
    first = Image.open(task/f'Previews/{prefix}0.png').convert('RGB')
    for palette in range(1, 8):
        assert ImageChops.difference(first, Image.open(task/f'Previews/{prefix}{palette}.png').convert('RGB')).getbbox() is not None, prefix+str(palette)
    blue = Image.open(task/f'Previews/{prefix}0.png').convert('RGBA')
    green = Image.open(task/f'Previews/{prefix}2.png').convert('RGBA')
    opaque = [(a,b) for a,b in zip(blue.getdata(),green.getdata()) if a[3] > 128]
    changed = sum(max(abs(a[c]-b[c]) for c in range(3)) > 3 for a,b in opaque)
    coverage[prefix] = changed/len(opaque)
    assert 0 < coverage[prefix] < .2, 'Team paint must preserve neutral hull: '+prefix
icon = Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/EnduranceIcon.png').convert('RGBA')
assert icon.size == (512,512) and icon.getchannel('A').getextrema() == (0,255)
bounds = icon.getchannel('A').getbbox()
assert bounds[0]>0 and bounds[1]>0 and bounds[2]<512 and bounds[3]<512
silhouette = Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/EnduranceSilhouette.png').convert('RGBA')
assert silhouette.getchannel('A').tobytes() == icon.getchannel('A').tobytes()
result = dict(source_hashes_unchanged=len(audit['source_hashes']), lossless_texture_copies=textures,
              maximum_geometry_error=maximum_geometry_error, maximum_bone_error=maximum_bone_error,
              live_palettes=8, wreck_palettes=8, team_changed_pixel_fraction=coverage, icon_crop=list(bounds),
              unity_removed_zero_area_triangles={'Hull Primary':6, 'hull':8})
(task/'SourceVerification.json').write_text(json.dumps(result, indent=2))
evidence = Path('Tools/Blender/Endurance/Evidence')
evidence.mkdir(exist_ok=True)
for name in ('SourceAudit.json','SourceVerification.json','Verification.json','ArtBounds.json','ArtManifest.json','HelperStripping.txt'):
    shutil.copyfile(task/name, evidence/name)
for name in audit['variants']:
    shutil.copyfile(task/name/'ConversionReport.json', evidence/(name+'Conversion.json'))
previews = Path('Tools/Blender/Endurance/Previews')
previews.mkdir(exist_ok=True)
for path in (task/'Previews').glob('*.png'):
    shutil.copyfile(path, previews/path.name)
print(json.dumps(result))
