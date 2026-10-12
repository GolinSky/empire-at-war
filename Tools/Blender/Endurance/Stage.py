"""Stage the verified Endurance FBXs and referenced textures."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Stage.py').read_text()
source = source.replace('ISDIRemakeImport', 'EnduranceImport').replace('EmpireShips/ISDI', 'RebellionShips/Endurance')
source = source.replace("report['folder'] = 'ISDI'", "report['folder'] = 'Endurance'")
source = source.replace("Image.new('L', (32,32),255).save(textures/'ISDI_TeamStripes_TeamMask.png')", '')
exec(compile(source, __file__, 'exec'))

# Match repaired Blender slots to the original binary parameter definitions.
audit = json.loads((TASK/'SourceAudit.json').read_text())
for variant, report in manifest.items():
    definitions = {}
    counts = {}
    for mesh in audit['variants'][variant]['material_audit']['meshes']:
        name = mesh['name']
        index = counts.get(name, 0)
        counts[name] = index + 1
        imported = report['before']['meshes'][name if index == 0 else name + '.' + str(index).zfill(3)]
        for name, material in zip(imported['materials'], mesh['materials']):
            definitions[name] = material['properties']
    for material in report['materials']:
        material['source_properties'] = definitions[material['name']]
(TASK/'ArtManifest.json').write_text(json.dumps(manifest, indent=2))
