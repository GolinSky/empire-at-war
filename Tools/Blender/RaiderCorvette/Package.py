"""Verify unchanged source files and retain editable art plus import evidence."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/RaiderCorvetteImport')
DESTINATION = Path('output/aotr-empire-units/RaiderCorvette-Converted')
audit = json.loads((TASK / 'SourceAudit.json').read_text(encoding='utf-8-sig'))
for filename, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(filename).read_bytes()).hexdigest() == expected, filename
previews = {}
for prefix in ('Team', 'WreckTeam'):
    baseline = Image.open(TASK / f'Previews/{prefix}0.png').convert('RGBA')
    for index in range(8):
        image = Image.open(TASK / f'Previews/{prefix}{index}.png').convert('RGBA')
        box = image.getchannel('A').getbbox()
        assert box and min(box[:2]) > 0 and max(box[2:]) < 512, (prefix, index, box)
        difference = ImageChops.difference(baseline.convert('RGB'), image.convert('RGB'))
        red, green, blue = difference.split()
        changed = image.width * image.height - ImageChops.lighter(ImageChops.lighter(red, green), blue).histogram()[0]
        if index:
            assert changed > 0, (prefix, index, 'No team-color response')
        previews[f'{prefix}{index}'] = dict(alpha_bounds=box, changed_pixels_from_blue=changed)
for name, path in [('Icon', 'Assets/Art/Textures/Ui/Icons/ShipIcon/RaiderCorvetteIcon.png'),
                   ('Silhouette', 'Assets/Art/Textures/Ui/Icons/ShipIcon/RaiderCorvetteSilhouette.png'),
                   ('Placement', str(TASK / 'Previews/Placement.png'))]:
    image = Image.open(path).convert('RGBA')
    box = image.getchannel('A').getbbox()
    assert box and min(box[:2]) > 0 and max(box[2:]) < 512, (name, box)
    previews[name] = dict(alpha_bounds=box)
(TASK / 'PreviewVerification.json').write_text(json.dumps(previews, indent=2), encoding='utf-8')
DESTINATION.mkdir(parents=True, exist_ok=True)
for source in (TASK / 'Output').iterdir():
    if source.suffix in ('.blend', '.fbx'):
        shutil.copy2(source, DESTINATION / source.name)
shutil.copytree(TASK / 'Output/Textures', DESTINATION / 'Textures', dirs_exist_ok=True)
shutil.copytree(TASK / 'Previews', DESTINATION / 'Previews', dirs_exist_ok=True)
shutil.copy2('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Credits_and_Permissions.pdf',
             DESTINATION / 'Credits_and_Permissions.pdf')
for name in ('SourceAudit', 'ConversionReport', 'GeometryVerification', 'Verification',
             'HardpointMapping', 'GameplayBounds', 'ArtBounds', 'PreviewVerification'):
    shutil.copy2(TASK / f'{name}.json', DESTINATION / f'{name}.json')
(DESTINATION / 'SOURCE_CREDITS.txt').write_text(
    'Source: Awakening of the Rebellion 2.11.9, Steam Workshop item 1397421866.\n'
    'Installed source unit: E_Raider_Corvette; Raider_Corvette.ALO.\n'
    'Original model/texture rights and attribution remain with their creators.\n'
    'Full supplied credits: Credits_and_Permissions.pdf, retained unchanged.\n'
    'No model-specific attribution has been verified; consult the supplied credits.\n'
    'SourceAudit.json records original file paths and SHA-256 hashes.\n'
    'No original source file was modified.\n', encoding='utf-8')
print(json.dumps(dict(source_files_unchanged=len(audit['source_hashes']),
                     team_palettes=8, live_and_wreck=True, retained=str(DESTINATION),
                     preview_checks=previews), indent=2))
