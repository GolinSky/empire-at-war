"""Stage the exact AOTR X-wing source and lossless texture conversions."""
import json
import sys
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/AloImport'))
from source_audit import audit_alo, resolve_source, sha256

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART')
OUTPUT = ROOT / 'Temp/LukeRedSquadron/Output'
STEM = 'LukeRedSquadron'
OUTPUT.mkdir(parents=True, exist_ok=True)
(OUTPUT / 'Textures').mkdir(exist_ok=True)
audit = audit_alo(SOURCE / 'MODELS/RV_X_Wing.ALO', STEM)
policies, textures, manifest = [], {}, []
for mesh in audit['meshes']:
    for slot in mesh['materials']:
        helper = slot['shader'] in {'MeshShadowVolume.fx', 'MeshCollision.fx'}
        additive = slot['shader'] == 'MeshAdditive.fx'
        mappings = []
        for name, semantic in [('BaseTexture', 'albedo'), ('NormalTexture', 'normal')]:
            if name not in slot['properties']:
                continue
            source_name = slot['properties'][name]
            source = resolve_source(SOURCE, 'TEXTURES/' + source_name)
            target = OUTPUT / 'Textures' / (STEM + '_' + Path(source_name).stem + '.png')
            image = Image.open(source).convert('RGBA')
            image.save(target)
            textures[source_name.casefold()] = {'path': str(target), 'size': image.size,
                'source': str(source), 'sha256': sha256(source)}
            mappings.append({'source': source_name, 'semantic': semantic})
        policies.append({'selector': {'piece': STEM, 'meshIndex': mesh['index'], 'slot': slot['slot'],
            'shader': slot['shader'], 'parameters': [{'name': k, 'value': v} for k, v in slot['properties'].items()]},
            'surface': 'additive' if additive else 'opaque', 'normalConvention': 'directx', 'textures': mappings})
        manifest.append({'index': mesh['index'], 'mesh': mesh['name'], 'hidden': helper or mesh['hidden'],
            'shader': slot['shader'], 'signature': slot['signature'],
            'material': f'{STEM}_M{mesh["index"]:03}_S{slot["slot"]:02}_{slot["signature"][:12]}',
            'textures': {m['semantic']: 'Assets/Art/Textures/Models/RebellionShips/'+STEM+'/'+Path(textures[m['source'].casefold()]['path']).name for m in mappings}})
spec = {'output': str(OUTPUT), 'textures': textures, 'recipe': {'pieces': [{'id': STEM}], 'materials': policies,
    'exceptions': [{'ruleId': 'helper-normal-representability', 'sourceIds': [f'{STEM}:mesh:{i}' for i in [5, 6, 7, 10]]}]}}
(OUTPUT / 'Spec.json').write_text(json.dumps(spec, indent=2))
(OUTPUT / 'SourceAudit.json').write_text(json.dumps(audit, indent=2))
(OUTPUT / 'UnityMaterials.json').write_text(json.dumps(manifest, indent=2))
(ROOT / 'Tools/Blender/LukeRedSquadron/Evidence/SourceAudit.json').write_text(json.dumps(audit, indent=2))
print(json.dumps({'sourceHash': audit['sha256'], 'bones': len(audit['bones']), 'meshes': len(audit['meshes']), 'textures': len(textures)}))
