"""Extract original scenery and referenced textures for audited Blender transport."""
import json
import sys
from pathlib import Path
from PIL import Image

PROJECT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(PROJECT / 'Tools/AloImport'))
from source_audit import audit_alo, meg_index, read_meg_member

SOURCE = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data')
OUTPUT = PROJECT / 'Temp/PlanetDecorationsImport'
OUTPUT.mkdir(parents=True, exist_ok=True)
models = meg_index(SOURCE / 'models.meg')
textures_index = meg_index(SOURCE / 'textures.meg')
specs = []
for name in ['EB_DOCK00', 'EB_DOCK_BREAK', 'W_SPACEJUNK', 'W_SPACEJUNK_MED', 'W_SPACEJUNK_SML', 'W_ORBITAL_RESOURCE_CONTAINER']:
    output = OUTPUT / name
    output.mkdir(exist_ok=True)
    raw, provenance = read_meg_member(models, 'DATA/ART/MODELS/' + name + '.ALO')
    alo = output / (name + '.ALO')
    alo.write_bytes(raw)
    audit = audit_alo(alo, name)
    textures, materials, exceptions = {}, [], []
    for mesh in audit['meshes']:
        if all(s['shader'] in {'MeshShadowVolume.fx', 'MeshCollision.fx'} for s in mesh['materials']):
            exceptions.append({'ruleId':'helper-normal-representability','sourceIds':[name + ':mesh:' + str(mesh['index'])]})
        for slot in mesh['materials']:
            refs = []
            for key, semantic in [('BaseTexture','albedo'),('NormalTexture','normal')]:
                source = slot['properties'].get(key)
                if not source:
                    continue
                matches = [k for k in textures_index['members'] if Path(k).stem == Path(source).stem.lower()]
                assert len(matches) == 1, (source, matches)
                data, evidence = read_meg_member(textures_index, matches[0])
                original = output / Path(matches[0]).name
                original.write_bytes(data)
                png = output / (name + '_' + Path(source).stem + '_' + semantic + '.png')
                image = Image.open(original).convert('RGBA')
                image.save(png)
                textures[source.casefold()] = {'path':str(png),'size':image.size,'source':evidence}
                refs.append({'source':source,'semantic':semantic})
            materials.append({'selector':{'piece':name,'meshIndex':mesh['index'],'slot':slot['slot'],'shader':slot['shader'],'parameters':[{'name':k,'value':v} for k,v in slot['properties'].items()]},'surface':'alpha' if slot['shader']=='MeshAlpha.fx' else 'opaque','normalConvention':'directx','textures':refs})
    spec = {'output':str(output),'textures':textures,'recipe':{'pieces':[{'id':name,'transportRigName':name+'Rig'}],'materials':materials,'exceptions':exceptions},'piece':audit,'provenance':provenance}
    (output / 'Spec.json').write_text(json.dumps(spec, indent=2))
    specs.append(str(output / 'Spec.json'))
    print(name, len(audit['meshes']), flush=True)
(OUTPUT / 'Specs.json').write_text(json.dumps(specs))
