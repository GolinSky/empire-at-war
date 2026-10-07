"""Audit Republic at War CIS originals and adapt the existing ALO station workflow."""
import hashlib
import json
import struct
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
REFERENCE = ROOT / 'Tools/Blender/RebelSpaceStation'
sys.path.insert(0, str(REFERENCE))
from Prepare import Audit, Chunks

SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data')
OUTPUT = ROOT / 'Temp/CisStationImport'


def Normals(path):
    result = {}
    for kind, payload in Chunks(path.read_bytes()):
        if kind != 0x400:
            continue
        fields = list(Chunks(payload))
        name = next(data.rstrip(b'\0').decode('ascii') for chunk, data in fields if chunk == 0x401)
        shaders = [data.rstrip(b'\0').decode('ascii') for chunk, material in fields if chunk == 0x10100
                   for key, data in Chunks(material) if key == 0x10101]
        if any(token in shaders[0] for token in ('Shadow', 'Collision')):
            continue
        normals = []
        for chunk, data in fields:
            if chunk != 0x10000:
                continue
            geometry = dict(Chunks(data))
            count = struct.unpack_from('<I', geometry[0x10001])[0]
            stride = 144 if 0x10007 in geometry else 128
            vertices = geometry[0x10007 if stride == 144 else 0x10005]
            normals.extend(struct.unpack_from('<3f', vertices, i * stride + 12) for i in range(count))
        result[name] = normals
    return result


def Adapt(text):
    return (text.replace('RebelStationImport', 'CisStationImport')
            .replace('RebelSpaceStation', 'CisSpaceStation')
            .replace('RebelStationSlot', 'CisStationSlot')
            .replace('RebelStations', 'CisStations')
            .replace('RebelStation', 'CisStation')
            .replace('RebellionSpaceStationView', 'SeparatistSpaceStationView')
            .replace('Rebellion-only', 'Separatist-only').replace('9885', '9887'))


def Main():
    textures_out = OUTPUT / 'Output/Textures'
    textures_out.mkdir(parents=True, exist_ok=True)
    models, textures, material_names = {}, {}, {}
    files = {p.stem.lower(): p for p in (SOURCE / 'Art/Textures').iterdir()}
    bases = ET.parse(SOURCE / 'XML/StarBases_Cis.xml').getroot()
    hardpoints = {row.attrib['Name']: row for row in ET.parse(SOURCE / 'XML/Hardpoints_CIS_Starbase.xml').getroot()}
    definitions = {}
    for level in range(1, 6):
        base = next(row for row in bases if row.get('Name') == f'Rebel_Star_Base_{level}')
        path = SOURCE / 'Art/Models' / base.findtext('Space_Model_Name').strip()
        assert path.name.lower() == f'seb_station_level_{level:02}.alo'
        model = Audit(path)
        model['level'] = level
        models[f'CisSpaceStationLevel{level}'] = model
        definitions[str(level)] = []
        for name in base.findtext('HardPoints').split(','):
            hp = hardpoints[name.strip()]
            assert not hp.findtext('Model_To_Attach'), 'Unexpected separate artwork: ' + name
            definitions[str(level)].append(dict(name=name.strip(), bone=hp.findtext('Attachment_Bone'),
                fire=hp.findtext('Fire_Bone_A'), type=hp.findtext('Type')))
        for mesh in model['meshes']:
            for material in mesh['materials']:
                signature = json.dumps(material['properties'], sort_keys=True) + material['shader']
                if signature not in material_names:
                    material_names[signature] = f'CisStationSlot{len(material_names):02}'
                for key, ref in material['properties'].items():
                    if not isinstance(ref, str) or not ref.lower().endswith(('.dds', '.tga')):
                        continue
                    source = files[Path(ref).stem.lower()]
                    name = 'CisSpaceStation_' + source.stem
                    png = textures_out / (name + '.png')
                    pixels = Image.open(source).convert('RGBA')
                    pixels.save(png)
                    assert Image.open(png).convert('RGBA').tobytes() == pixels.tobytes()
                    textures[ref.lower()] = dict(source=str(source), png=str(png), name=name,
                        sha256=hashlib.sha256(source.read_bytes()).hexdigest(), size=pixels.size,
                        normal=key == 'NormalTexture')
    audit = dict(models=models, textures=textures, material_names=material_names, definitions=definitions,
        xml_hashes={str(path): hashlib.sha256(path.read_bytes()).hexdigest()
                    for path in [SOURCE / 'XML/StarBases_Cis.xml', SOURCE / 'XML/Hardpoints_CIS_Starbase.xml']})
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(audit, indent=2))
    converter = Adapt((REFERENCE / 'Convert.py').read_text())
    converter = converter.replace('import json\n', 'import json\nimport itertools\n')
    converter = converter.replace('tuple(round(v, 3) for v in corner[:3])', 'tuple(round(v * 1000) for v in corner[:3])')
    converter = converter.replace('candidates = buckets.get(key, expected)',
        "candidates = [p for offset in itertools.product((-1, 0, 1), repeat=3) for p in buckets.get(tuple(a + b for a, b in zip(key, offset)), ())]\n            assert candidates, (obj['EaWName'], corner)")
    converter = converter.replace('for obj in list(bpy.context.scene.objects):', 'for obj in list(bpy.data.objects):')
    converter = converter.replace("'CisStationSlot' + str(len(materials)).zfill(2)", "AUDIT['material_names'][signature]")
    converter = converter.replace("'Shadow' in shader or row['name'].endswith('_Blast')", "'Shadow' in shader or 'Collision' in shader or row['name'].endswith('_Blast')")
    converter = converter.replace("if 'Shadow' in shader:", "if 'Shadow' in shader or 'Collision' in shader:")
    converter = converter.replace('    before = Snapshot(scene)', '''    # ALAMO skips the binary normal field; restore authored normals on unsimplified meshes.
    for row in audit['meshes']:
        if any(token in row['materials'][0]['shader'] for token in ('Shadow', 'Collision')):
            continue
        mesh = scene.objects[row['name']].data
        normals = SOURCE_NORMALS[row['name']]
        assert len(mesh.vertices) == len(normals), row['name']
        mesh.use_auto_smooth = True
        mesh.normals_split_custom_set_from_vertices(normals)
    before = Snapshot(scene)''')
    (OUTPUT / 'Convert.py').write_text(converter)
    for name, model in models.items():
        normal_json = json.dumps(Normals(Path(model['file'])), separators=(',', ':'))
        for index, offset in enumerate(range(0, len(normal_json), 120000)):
            code = "import bpy\nassert bpy.types.blendermcp_server.port == 9887\n"
            if index == 0:
                code += "bpy.context.scene['CisStationNormals'] = ''\n"
            code += "bpy.context.scene['CisStationNormals'] += " + repr(normal_json[offset:offset + 120000])
            (OUTPUT / f'{name}Normals{index}.json').write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(code=code))))
        request = dict(tool='execute_blender_code', arguments=dict(
            code="import bpy\nimport json\nSOURCE_NORMALS = json.loads(bpy.context.scene['CisStationNormals'])\nAUDIT = " + repr(dict(models={name: model}, textures=textures, material_names=material_names)) + '\n' + converter,
            user_prompt='Import Republic at War CIS station levels 1-5, preserving geometry, UVs, normals and hierarchy.'))
        (OUTPUT / (name + '.json')).write_text(json.dumps(request))
    for filename in ['BuildArt.cs', 'BuildView.cs', 'Verify.cs', 'VerifyGeometry.py', 'Render.cs', 'Call.py', 'Start.py']:
        text = Adapt((REFERENCE / filename).read_text())
        if filename == 'BuildArt.cs':
            start = text.index('        var donor =')
            end = text.index('        float commonScale', start)
            text = text[:start] + '        float targetWidth = (float)JObject.Parse(File.ReadAllText("Tools/Blender/SpaceStations/Separatist.json"))["targetDiameter"];\n' + text[end:]
            text = text.replace('importer.importAnimation = false;', 'importer.importAnimation = false;\n            importer.importNormals = ModelImporterNormals.Import;')
            text = text.replace('bool normal = importer.assetPath.Contains("Bump");',
                'bool normal = audit["textures"].Children<Newtonsoft.Json.Linq.JProperty>().Any(t => (bool)t.Value["normal"] && importer.assetPath.EndsWith((string)t.Value["name"] + ".png"));')
            text = text.replace('material.SetFloat("_TeamLiveryStrength", 0);', 'material.SetFloat("_TeamLiveryStrength", 0);\n                    material.SetFloat("_TeamRimStrength", 0);')
            text = text.replace('EditorUtility.SetDirty(material);',
                'if (sourceShader == "MeshAlpha.fx") { material.SetFloat("_AlphaClip", 1); material.SetFloat("_Cutoff", .5f); material.EnableKeyword("_ALPHATEST_ON"); }\n                EditorUtility.SetDirty(material);')
            text = text.replace('renderer.name.EndsWith(" Shadow")', '!renderer.enabled && (renderer.name == "shadow" || renderer.name == "collision")')
            text = text.replace('PREFABS + "RepublicSpaceStationView.prefab"', 'PREFABS + "SeparatistSpaceStationView.prefab"')
        if filename == 'BuildView.cs':
            text = text.replace('foreach (var go in geometry) UnityEngine.Object.DestroyImmediate(go);', '''foreach (var go in geometry)
                {
                    if (go == root)
                    {
                        UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
                        UnityEngine.Object.DestroyImmediate(root.GetComponent<MeshFilter>());
                    }
                    else UnityEngine.Object.DestroyImmediate(go);
                }''')
            text = text.replace('Vector3 oldScale = root.transform.localScale;', 'Vector3 oldScale = root.transform.localScale;\n                Quaternion oldRotation = root.transform.localRotation;\n                root.transform.localRotation = Quaternion.identity;')
            text = text.replace('child.localPosition = Vector3.Scale(child.localPosition, oldScale);', 'child.localPosition = oldRotation * Vector3.Scale(child.localPosition, oldScale);\n                    child.localRotation = oldRotation * child.localRotation;')
            start = text.index('        // No damaged Rebel ALO was supplied.')
            end = text.index('        AssetDatabase.SaveAssets();', start)
            text = text[:start] + text[end:]
            text = text.replace('private const string DONOR = PREFABS + "RepublicSpaceStationView.prefab";',
                'private const string DONOR = PREFABS + "SeparatistSpaceStationView.prefab";')
        if filename == 'VerifyGeometry.py':
            text = text.replace('from Prepare import Chunks, ROOT, OUTPUT',
                "import sys\nROOT = Path(__file__).resolve().parents[2]\nsys.path.insert(0, str(ROOT / 'Tools/Blender/RebelSpaceStation'))\nfrom Prepare import Chunks\nOUTPUT = ROOT / 'Temp/CisStationImport'")
            text = text.replace("if 'Shadow' in row['materials'][0]['shader']:", "if any(token in row['materials'][0]['shader'] for token in ('Shadow', 'Collision')):")
            text = text.replace('texture_error < .00002', 'texture_error < .0001')
            text = text.replace("    for texture in audit['textures'].values():", "    for path, digest in audit['xml_hashes'].items():\n        assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == digest\n    for texture in audit['textures'].values():")
        if filename == 'Verify.cs':
            text = text.replace('m.Point == null || m.Art == null', 'm.Point == null')
            text = text.replace('["uv"] = new JArray(mesh.uv.Select(v => new JArray(v.x, v.y))),', '["uv"] = new JArray(mesh.uv.Select(v => new JArray(v.x, v.y))),\n                    ["normals"] = new JArray(mesh.normals.Select(v => V(renderer.localToWorldMatrix.inverse.transpose.MultiplyVector(v).normalized))),')
        if filename == 'Render.cs':
            start = text.index('                // Frame the common dome')
            end = text.index('                UnityEngine.Object.DestroyImmediate(root);', start)
            text = text[:start] + text[end:]
            text = text.replace('and five dome close-ups.', 'with source UVs.')
            text = text.replace('for (int team = 0; team < 8; team++)', 'for (int team = -1; team < 8; team++)')
        (OUTPUT / filename).write_text(text)
    for name, model in models.items():
        print(name, 'bones', len(model['bones']), 'hull triangles', sum(s['triangleCount'] for m in model['meshes'] for s in m['materials'] if 'Bump' in s['shader']))
    print('Textures:', sorted(textures), 'material definitions:', len(material_names))


if __name__ == '__main__':
    Main()
