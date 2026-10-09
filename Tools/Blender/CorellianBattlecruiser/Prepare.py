"""Audit installed AOTR source and stage lossless texture pixels; originals stay untouched."""
import json
from pathlib import Path
from PIL import Image

NAME = 'CorellianBattlecruiser'
TASK = Path('Temp/CorellianBattlecruiserImport')
TASK.mkdir(parents=True, exist_ok=True)
# Reuse the checked-in binary readers, without running their ship-specific entry points.
scope = {'__name__': 'audit'}
reader = Path('Tools/Blender/MC80Independence/AuditMaterials.py').read_text()
exec(reader.split('variants = ')[0], scope)
model = scope['Audit'](Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/MODELS/CV_Battlecruiser.ALO'))
prepare = Path('Tools/Blender/MC80Independence/Prepare.py').read_text().replace('MC80Independence', NAME).replace('R_MC80_Independence', 'R_Corellian_Battlecruiser')
# The replacement above also changes the unit name; correct it explicitly.
prepare = prepare.replace('R_CorellianBattlecruiser', 'R_Corellian_Battlecruiser')
exec(prepare, {'__name__': 'source_audit'})
audit = json.loads((TASK/'SourceAudit.json').read_text())
textures = TASK/'Textures'
textures.mkdir(exist_ok=True)
mapping = {}
models = {NAME: model}
for index, (key, source) in enumerate(list(audit['models'].items())[1:], 1):
    models[NAME+'Turret'+str(index).zfill(2)] = scope['Audit'](Path(source['file']))
normal_refs = {m['properties'].get('NormalTexture', '').upper() for model in models.values() for mesh in model['meshes'] for m in mesh['materials']}
refs = {ref: path for source in audit['models'].values() for ref, path in source['textures'].items()}
for ref, path in refs.items():
    normal = ref.upper() in normal_refs
    stem = NAME+'_'+Path(ref).stem+('_Normal' if normal else '_Albedo')
    image = Image.open(path).convert('RGBA')
    image.save(textures/(stem+'.png'))
    assert Image.open(textures/(stem+'.png')).convert('RGBA').tobytes() == image.tobytes()
    mapping[ref.upper()] = [stem, normal]
    if any(m['properties'].get('BaseTexture','').upper()==ref.upper() and 'Colorize' in m['shader'] for model in models.values() for mesh in model['meshes'] for m in mesh['materials']):
        mask = image.getchannel('A')
        mask.save(textures/(stem+'_TeamMask.png'))
        print('Team mask', ref, mask.getextrema())
assigned, definitions = {}, []
for mesh in [mesh for model in models.values() for mesh in model['meshes']]:
    for material in mesh['materials']:
        signature = json.dumps([material['shader'], material['properties']], sort_keys=True)
        if signature not in assigned:
            name = NAME+'_Material'+str(len(definitions)).zfill(2)
            assigned[signature] = name
            base = material['properties'].get('BaseTexture','').upper()
            normal = material['properties'].get('NormalTexture','').upper()
            definitions.append(dict(name=name, shader=material['shader'], properties=material['properties'], base=mapping[base][0] if base else None, normal=mapping[normal][0] if normal else None))
        material['materialName'] = assigned[signature]
(TASK/'BinaryMaterialAudit.json').write_text(json.dumps(dict(models=models, materials=definitions), indent=2))
(TASK/'TextureMapping.json').write_text(json.dumps(mapping, indent=2))
print('BINARY', {name: [(m['name'], m['hidden'], [(s['shader'],s['triangleCount']) for s in m['materials']]) for m in model['meshes']] for name,model in models.items()})
print('STATS', {k:v for k,v in audit['unit'].items() if k in ('Shield_Points','Tactical_Health','Max_Speed','Tactical_Build_Cost_Multiplayer','Tactical_Build_Time_Seconds','Starting_Spawned_Units_Tech_0','Reserve_Spawned_Units_Tech_0')})
