"""Audit installed Remake Tartan assets without modifying the source."""
import ast
import hashlib
import json
import struct
import xml.etree.ElementTree as ET
from pathlib import Path

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1770851727/Data')
TASK = Path('Temp/TartanImport')
TASK.mkdir(parents=True, exist_ok=True)
(TASK / 'Output/Textures').mkdir(parents=True, exist_ok=True)

# Reuse the independently verified binary readers, without their task entry points.
def LoadFunctions(path, names):
    tree = ast.parse(Path(path).read_text())
    tree.body = [node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name in names]
    exec(compile(tree, path, 'exec'), globals())

LoadFunctions('Tools/Blender/RaiderCorvette/Prepare.py', {'Bones', 'Values'})
LoadFunctions('Tools/Blender/MC80Independence/AuditMaterials.py', {'Chunks', 'MicroChunks', 'Audit'})
unit_path = SOURCE / 'XML/Units/Space/Units_Space_Empire_Tartan_Gunship.xml'
hp_path = SOURCE / 'XML/Units/Space/Hardpoints/Hardpoints_All_Snubfighters_Rework.xml'
unit = next(e for e in ET.parse(unit_path).getroot() if e.attrib.get('Name') == 'Tartan_Gunship')
names = {n.strip() for n in unit.findtext('HardPoints').split(',')}
hardpoints = [dict(name=e.attrib['Name'], **Values(e)) for e in ET.parse(hp_path).getroot() if e.attrib.get('Name') in names]
models = {}
hashes = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in (unit_path, hp_path)}
for name, filename in [('Tartan', 'Empire_Tartan.alo'), ('TartanTurret', 'Empire_Tartan_Turret.ALO')]:
    path = SOURCE / 'Art/Models' / filename
    audit = Audit(path)
    textures = {}
    for mesh in audit['meshes']:
        for mat in mesh['materials']:
            for key, value in mat['properties'].items():
                if 'Texture' not in key or value == 'None':
                    continue
                texture = SOURCE / 'Art/Textures' / value
                if not texture.exists():
                    texture = texture.with_suffix('.dds')
                if not texture.exists():
                    raise FileNotFoundError(texture)
                textures[value] = str(texture)
                hashes[str(texture)] = hashlib.sha256(texture.read_bytes()).hexdigest()
    hashes[str(path)] = audit['sha256']
    models[name] = dict(file=str(path), bones=Bones(path), textures=textures, binary=audit)
report = dict(models=models, hardpoints=hardpoints, unit=Values(unit), source_hashes=hashes)
(TASK / 'SourceAudit.json').write_text(json.dumps(report, indent=2))
for name, model in models.items():
    print(name, len(model['bones']), 'bones', [(m['name'], m['hidden'], [(s['shader'], s['properties'].get('BaseTexture'), s['triangleCount']) for s in m['materials']]) for m in model['binary']['meshes']])
