"""Audit Remake source and prepare lossless textures for the Assault Frigate Mk II."""
import json
from pathlib import Path
from PIL import Image

NAME = 'AssaultFrigateMkII'
TASK = Path('Temp/AssaultFrigateMkIIImport')
source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', TASK.as_posix())
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Assault_Frigate_Mark_II')")
source = source.replace("{'ISDI':", "{NAME:")
source = source.replace("'ISDIPart'", "NAME+'Part'")
source = source.replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Retain source death-clone provenance; derive the project wreck from the live hull.")
exec(compile(source, __file__, 'exec'))

audit = json.loads((TASK/'SourceAudit.json').read_text())
models = {}
mapping = {}
texture_directory = TASK/'Textures'
texture_directory.mkdir(exist_ok=True)
for key, record in audit['variants'].items():
    model = record['material_audit']
    model['file'] = record['file']
    models[key] = model
    normals = {m['properties'].get('NormalTexture', '').upper() for mesh in model['meshes'] for m in mesh['materials']}
    for ref, texture in record['textures'].items():
        normal = ref.upper() in normals
        stem = NAME+'_'+Path(ref).stem+('_Normal' if normal else '_Albedo')
        pixels = Image.open(texture['source']).convert('RGBA')
        pixels.save(texture_directory/(stem+'.png'))
        assert Image.open(texture_directory/(stem+'.png')).convert('RGBA').tobytes() == pixels.tobytes()
        mapping[ref.upper()] = [stem, normal]
assigned, definitions = {}, []
for model in models.values():
    for mesh in model['meshes']:
        for material in mesh['materials']:
            signature = json.dumps([material['shader'], material['properties']], sort_keys=True)
            if signature not in assigned:
                name = NAME+'_Material'+str(len(definitions)).zfill(2)
                assigned[signature] = name
                base = material['properties'].get('BaseTexture', '').upper()
                normal = material['properties'].get('NormalTexture', '').upper()
                definitions.append(dict(name=name, shader=material['shader'], properties=material['properties'], base=mapping[base][0] if base else None, normal=mapping[normal][0] if normal else None))
            material['materialName'] = assigned[signature]
(TASK/'BinaryMaterialAudit.json').write_text(json.dumps(dict(models=models, materials=definitions), indent=2))
print('Audited', len(models), 'models,', len(definitions), 'material definitions and', len(mapping), 'textures')
