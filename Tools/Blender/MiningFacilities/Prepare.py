"""Extract the three requested vanilla models without changing game files."""
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/Blender/RebelSpaceStation'))
from Prepare import Audit
from PrepareAttachments import Extract

GAME = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data')
OUTPUT = ROOT / 'Temp/MiningFacilitiesImport'
MODELS = {'MiningFacility': 'NB_ASTEROIDMINING_FULL.ALO',
          'BattleMiningFacility': 'NB_APBASE.ALO',
          'MiningConstruction': 'NB_CMC22MINING.ALO'}


def Main():
    source = OUTPUT / 'Source'
    textures = OUTPUT / 'Output/Textures'
    source.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    Extract(GAME / 'config.meg', ['STARBASES.XML', 'HARDPOINTS.XML', 'SPACEBUILDABLESSKIRMISH.XML'], source)
    station = next(x for x in ET.parse(source / 'STARBASES.XML').getroot() if x.attrib.get('Name') == 'Pirate_Asteroid_Base')
    hardpoints = {x.attrib.get('Name'): x for x in ET.parse(source / 'HARDPOINTS.XML').getroot()}
    attachments = []
    for name in station.findtext('HardPoints').split(','):
        hp = hardpoints[name.strip()]
        model = Path(hp.findtext('Model_To_Attach').strip()).stem.upper()
        MODELS[model] = model + '.ALO'
        attachments.append(dict(model=model, bone=hp.findtext('Attachment_Bone').strip()))
    (OUTPUT / 'Attachments.json').write_text(json.dumps(attachments, indent=2))
    Extract(GAME / 'models.meg', MODELS.values(), source)
    audit = dict(models={}, textures={})
    for name, filename in MODELS.items():
        model = Audit(source / filename)
        model['level'] = 0
        audit['models'][name] = model
        for mesh in model['meshes']:
            for material in mesh['materials']:
                for key, ref in material['properties'].items():
                    if not isinstance(ref, str) or not ref.lower().endswith(('.dds', '.tga')):
                        continue
                    if ref.lower() in audit['textures']:
                        continue
                    filename = Path(ref).stem + '.DDS'
                    Extract(GAME / 'textures.meg', [filename], source)
                    path = source / filename
                    texture_name = 'MiningFacilities_' + path.stem
                    png = textures / (texture_name + '.png')
                    pixels = Image.open(path).convert('RGBA')
                    pixels.save(png)
                    assert Image.open(png).convert('RGBA').tobytes() == pixels.tobytes()
                    audit['textures'][ref.lower()] = dict(source=str(path), png=str(png), name=texture_name,
                        sha256=hashlib.sha256(path.read_bytes()).hexdigest(), size=pixels.size, normal=key == 'NormalTexture')
    audit['archive'] = str(GAME)
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(audit, indent=2), encoding='utf-8')
    print(json.dumps(audit, indent=2))


if __name__ == '__main__':
    Main()
