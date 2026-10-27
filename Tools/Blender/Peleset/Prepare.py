"""Audit the RaW Peleset hull, attached turrets and source texture assignments."""
import ast
import hashlib
import json
from pathlib import Path
import shutil
import xml.etree.ElementTree as ET
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
MOD = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data')
TASK = ROOT / 'Temp/PelesetImport'


def Functions(path):
    tree = ast.parse(path.read_text(encoding='utf-8-sig'))
    tree.body = [node for node in tree.body if isinstance(node, (ast.Import, ast.ImportFrom, ast.FunctionDef))]
    scope = {}
    exec(compile(tree, str(path), 'exec'), scope)
    return scope


def Main():
    source, textures = TASK / 'Source', TASK / 'Output/Textures'
    source.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    available = {p.name.lower(): p for p in (MOD / 'Art/Textures').iterdir() if p.is_file()}
    audit_binary = Functions(ROOT / 'Tools/Blender/prepare_isd_i.py')['Audit']
    audit_materials = Functions(ROOT / 'Tools/Blender/MC80Independence/AuditMaterials.py')['Audit']
    models, info, files = {}, {}, []
    for name, suffix in [('Peleset', ''), ('PelesetTurret01', '_Turret_01'), ('PelesetTurret02', '_Turret_02')]:
        model = MOD / ('Art/Models/SeV_Techno_Union_Ord_Cruiser' + suffix + '.ALO')
        files.append(model)
        binary, materials = audit_binary(model), audit_materials(model)
        models[name] = dict(source_model=str(model), model=binary, materials=materials)
        references = sorted({v for mesh in materials['meshes'] for row in mesh['materials'] for v in row['properties'].values()
                             if isinstance(v, str) and v.lower().endswith(('.dds', '.tga'))})
        for reference in references:
            if reference in info:
                continue
            key = reference.lower()
            if key not in available:
                key = str(Path(key).with_suffix('.dds'))
            path = available[key]
            files.append(path)
            stem = 'Peleset_' + path.stem
            image = Image.open(path).convert('RGBA')
            target = textures / (stem + '.png')
            image.save(target)
            assert Image.open(target).convert('RGBA').tobytes() == image.tobytes()
            info[reference] = dict(source=str(path), stem=stem, size=list(image.size), alpha=list(image.getchannel('A').getextrema()))
    unit_xml = MOD / 'XML/Units_Space_cis_technounion_cruiser.xml'
    hardpoint_xml = MOD / 'XML/Hardpoints_CIS_Space.xml'
    files.extend([unit_xml, hardpoint_xml])
    # Unit XML contains a malformed death-clone Text_ID; the live unit is parsed separately.
    content = unit_xml.read_text()
    start = content.index('<SpaceUnit Name="TU_Ordance_Cruiser">')
    unit = ET.fromstring(content[start:content.index('</SpaceUnit>', start) + len('</SpaceUnit>')])
    names = {name.strip() for name in unit.findtext('HardPoints').split(',') if name.strip()}
    hardpoints = [dict(name=e.attrib['Name'], **{c.tag: (c.text or '').strip() for c in e})
                  for e in ET.parse(hardpoint_xml).getroot() if e.attrib.get('Name') in names]
    for path in files:
        shutil.copy2(path, source / path.name)
    report = dict(source_unit='TU_Ordance_Cruiser', models=models, textures=info, hardpoints=hardpoints,
                  unit={c.tag: (c.text or '').strip() for c in unit},
                  source_hashes={str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in files})
    (TASK / 'SourceAudit.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(json.dumps(dict(models={name: dict(bones=[b['name'] for b in r['model']['bones']], meshes=r['materials']['meshes']) for name,r in models.items()}, textures=info), indent=2))


if __name__ == '__main__':
    Main()
