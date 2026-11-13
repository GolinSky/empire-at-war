"""Extract the original space sensor node and prepare a static conversion spec."""
import json
import sys
from pathlib import Path

from PIL import Image

PROJECT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(PROJECT / 'Tools/AloImport'))
from source_audit import audit_alo, meg_index, read_meg_member

SOURCE = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data')
OUTPUT = PROJECT / 'Temp/ZoneRelayImport'
OUTPUT.mkdir(parents=True, exist_ok=True)
model_index = meg_index(SOURCE / 'models.meg')
data, provenance = read_meg_member(model_index, 'DATA/ART/MODELS/NB_SPACESENSORNODE.ALO')
alo = OUTPUT / 'NB_SPACESENSORNODE.ALO'
alo.write_bytes(data)
audit = audit_alo(alo, 'SpaceSensorNode')
texture_index = meg_index(SOURCE / 'textures.meg')
textures = {}
for source, semantic in [('NB_SensorNode.tga', 'Albedo'), ('NB_SensorNode_Bump.tga', 'Normal')]:
    member = next(k for k in texture_index['members'] if Path(k).stem == Path(source).stem.lower())
    data, evidence = read_meg_member(texture_index, member)
    original = OUTPUT / Path(member).name
    original.write_bytes(data)
    image = Image.open(original).convert('RGBA')
    png = OUTPUT / ('SpaceSensorNode_' + semantic + '.png')
    image.save(png)
    textures[source.casefold()] = {'path': str(png), 'size': image.size, 'source': evidence}
materials = []
for mesh in audit['meshes']:
    for slot in mesh['materials']:
        materials.append({'selector': {'piece': 'SpaceSensorNode', 'meshIndex': mesh['index'],
            'slot': slot['slot'], 'shader': slot['shader'],
            'parameters': [{'name': k, 'value': v} for k, v in slot['properties'].items()]},
            'surface': 'opaque', 'normalConvention': 'directx',
            'textures': [{'source': value, 'semantic': semantic} for key, semantic in
                [('BaseTexture', 'albedo'), ('NormalTexture', 'normal')]
                if (value := slot['properties'].get(key))]})
spec = {'output': str(OUTPUT), 'textures': textures, 'recipe': {
    'pieces': [{'id': 'SpaceSensorNode', 'transportRigName': 'SpaceSensorNodeRig'}],
    'materials': materials}, 'piece': audit, 'provenance': provenance}
(OUTPUT / 'Spec.json').write_text(json.dumps(spec, indent=2))
print(json.dumps({'source': provenance, 'meshes': len(audit['meshes']), 'textures': list(textures)}))
