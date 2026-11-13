"""Extract the vanilla construction pod and its exact referenced textures."""
import json
import sys
from pathlib import Path
from PIL import Image

PROJECT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(PROJECT / 'Tools/AloImport'))
from source_audit import audit_alo, meg_index, read_meg_member

SOURCE = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data')
OUTPUT = PROJECT / 'Temp/ConstructionPodImport'
OUTPUT.mkdir(parents=True, exist_ok=True)
data, provenance = read_meg_member(meg_index(SOURCE / 'models.meg'), 'DATA/ART/MODELS/EB_CONSTRUCTION_POD.ALO')
alo = OUTPUT / 'EB_CONSTRUCTION_POD.ALO'
alo.write_bytes(data)
audit = audit_alo(alo, 'ConstructionPod')
index = meg_index(SOURCE / 'textures.meg')
textures = {}
for source, suffix in [('EB_Station.tga', 'Hull_Albedo'), ('EB_Station_bump.tga', 'Hull_Normal'), ('NB_Girder2.tga', 'Girder_Albedo')]:
    matches = [k for k in index['members'] if Path(k).stem == Path(source).stem.lower()]
    assert len(matches) == 1, matches
    data, evidence = read_meg_member(index, matches[0])
    original = OUTPUT / Path(matches[0]).name
    original.write_bytes(data)
    image = Image.open(original).convert('RGBA')
    png = OUTPUT / ('ConstructionPod_' + suffix + '.png')
    image.save(png)
    textures[source.casefold()] = {'path': str(png), 'size': image.size, 'source': evidence}
materials = []
for mesh in audit['meshes']:
    for slot in mesh['materials']:
        materials.append({'selector': {'piece': 'ConstructionPod', 'meshIndex': mesh['index'], 'slot': slot['slot'],
            'shader': slot['shader'], 'parameters': [{'name': k, 'value': v} for k, v in slot['properties'].items()]},
            'surface': 'alpha' if slot['shader'] == 'MeshAlpha.fx' else 'opaque', 'normalConvention': 'directx',
            'textures': [{'source': value, 'semantic': semantic} for key, semantic in
                [('BaseTexture', 'albedo'), ('NormalTexture', 'normal')] if (value := slot['properties'].get(key))]})
spec = {'output': str(OUTPUT), 'textures': textures, 'recipe': {
    'pieces': [{'id': 'ConstructionPod', 'transportRigName': 'ConstructionPodRig'}], 'materials': materials,
    'exceptions': [{'ruleId': 'helper-normal-representability', 'sourceIds': ['ConstructionPod:mesh:4']}]},
    'piece': audit, 'provenance': provenance}
(OUTPUT / 'Spec.json').write_text(json.dumps(spec, indent=2))
print(json.dumps({'meshes': len(audit['meshes']), 'bones': len(audit['bones']), 'textures': list(textures)}))
