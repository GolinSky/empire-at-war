"""Audit RaW Republic stations and adapt the established station conversion/build workflow."""
import hashlib
import json
import sys
import struct
import xml.etree.ElementTree as ET
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
REFERENCE = ROOT / 'Tools/Blender/RebelSpaceStation'
sys.path.insert(0, str(REFERENCE))
from Prepare import Audit, Chunks

OUTPUT = ROOT / 'Temp/RepublicStationImport'
SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data')
PORT = 9897


def Adapt(text):
    return (text.replace('RebelStationImport', 'RepublicStationImport')
            .replace('RebelSpaceStation', 'RepublicSpaceStation')
            .replace('RebelStationSlot', 'RepublicStationSlot')
            .replace('RebelStations', 'RepublicStations')
            .replace('RebelStation', 'RepublicStation')
            .replace('RebellionSpaceStationView', 'RepublicSpaceStationView')
            .replace('Rebellion-only', 'Republic-only')
            .replace('9885', str(PORT)))


def ReadNormals(model):
    normals = {}
    for kind, payload in Chunks(Path(model['file']).read_bytes()):
        if kind != 0x400: continue
        fields = list(Chunks(payload))
        name = next(v.rstrip(bytes([0])).decode('ascii') for k, v in fields if k == 0x401)
        if 'Shadow' in next(m for m in model['meshes'] if m['name'] == name)['materials'][0]['shader'] or 'Collision' in next(m for m in model['meshes'] if m['name'] == name)['materials'][0]['shader']: continue
        values = bytearray()
        for kind, payload in fields:
            if kind != 0x10000: continue
            geometry = dict(Chunks(payload))
            count = struct.unpack_from('<I', geometry[0x10001])[0]
            stride = 144 if 0x10007 in geometry else 128
            vertices = geometry[0x10007 if stride == 144 else 0x10005]
            for i in range(count): values.extend(vertices[i * stride + 12:i * stride + 24])
        normals[name] = values.hex()
    model['normals'] = normals


def Main():
    textures_out = OUTPUT / 'Output/Textures'
    textures_out.mkdir(parents=True, exist_ok=True)
    models = {}
    for level in range(1, 6):
        model = Audit(SOURCE / f'Art/Models/ReB_Shipyard_Level_{level:02}.ALO')
        ReadNormals(model)
        model['level'] = level
        models[f'RepublicSpaceStationLevel{level}'] = model
    xmls = ['StarBases_Republic.xml', 'Hardpoints_Republic_Starbase.xml']
    bases = ET.parse(SOURCE / 'XML' / xmls[0]).getroot()
    hardpoints = {row.attrib['Name']: row for row in ET.parse(SOURCE / 'XML' / xmls[1]).getroot() if 'Name' in row.attrib}
    levels = {}
    for level in range(1, 6):
        base = next(row for row in bases if row.attrib.get('Name') == f'Empire_Star_Base_{level}')
        assert Path(base.findtext('Space_Model_Name').strip()).stem.lower() == f'reb_shipyard_level_{level:02}'
        rows = []
        for name in base.findtext('HardPoints').split(','):
            hp = hardpoints[name.strip()]
            model = hp.findtext('Model_To_Attach')
            if model:
                rows.append(dict(model=Path(model.strip()).stem, bone=hp.findtext('Attachment_Bone').strip(), hardpoint=name.strip()))
        levels[str(level)] = rows
    names = sorted({row['model'] for rows in levels.values() for row in rows})
    attachment_models = {}
    assert not names, 'Republic station weapon art is expected to be embedded in the five source hulls.'
    files = {p.stem.lower(): p for p in (SOURCE / 'Art/Textures').iterdir()}
    textures = {}
    for model in list(models.values()) + list(attachment_models.values()):
        for mesh in model['meshes']:
            for material in mesh['materials']:
                for key, ref in material['properties'].items():
                    if not isinstance(ref, str) or not ref.lower().endswith(('.dds', '.tga')):
                        continue
                    source = files[Path(ref).stem.lower()]
                    name = 'RepublicSpaceStation_' + source.stem
                    png = textures_out / (name + '.png')
                    pixels = Image.open(source).convert('RGBA')
                    pixels.save(png)
                    assert Image.open(png).convert('RGBA').tobytes() == pixels.tobytes()
                    textures[ref.lower()] = dict(source=str(source), png=str(png), name=name,
                        sha256=hashlib.sha256(source.read_bytes()).hexdigest(), size=pixels.size, normal=key == 'NormalTexture')
    (OUTPUT / 'SourceAudit.json').write_text(json.dumps(dict(models=models, textures=textures), indent=2))
    (OUTPUT / 'AttachmentAudit.json').write_text(json.dumps(dict(models=attachment_models, textures=textures, levels=levels,
        xml_hashes={name: hashlib.sha256((SOURCE / 'XML' / name).read_bytes()).hexdigest() for name in xmls}), indent=2))
    (OUTPUT / 'Attachments.json').write_text(json.dumps(levels, indent=2))
    converter = Adapt((REFERENCE / 'Convert.py').read_text())
    # This dedicated process contains only this import; stale validation objects break ALAMO's global helper pass.
    converter = converter.replace('list(bpy.context.scene.objects)', 'list(bpy.data.objects)')
    converter = converter.replace("'Shadow' in shader or row['name'].endswith('_Blast')", "'Shadow' in shader or 'Collision' in shader or 'Shield' in shader or row['name'].endswith('_Blast')")
    converter = converter.replace("if 'Shadow' in shader:", "if 'Shadow' in shader or 'Collision' in shader:")
    # Static FBX transport uses common axes to avoid Euler/gimbal drift; Unity restores source rotations.
    converter = converter.replace('        bone.matrix = world_matrices[index]', '        bone.length = 1.0\n        bone.matrix = Matrix.Translation(world_matrices[index].translation)\n        bone["EaWSourceWorldMatrix"] = [value for row in world_matrices[index] for value in row]')
    # ALAMO skips stored normals. The audit supplies them in unchanged source vertex order.
    converter = converter.replace('import json\n', 'import json\nimport array\n')
    converter = converter.replace('    before = Snapshot(scene)', """    for row in audit['meshes']:
        obj = scene.objects[row['name']]
        if 'Shadow' in obj['EaWShader'] or 'Collision' in obj['EaWShader']: continue
        buffer = bytes.fromhex(''.join(bpy.context.scene[key] for key in audit['normal_chunks'][row['name']]))
        values = array.array('f')
        values.frombytes(buffer)
        normals = [tuple(values[offset:offset + 3]) for offset in range(0, len(values), 3)]
        assert len(normals) == len(obj.data.vertices), row['name']
        obj.data.use_auto_smooth = True
        obj.data.auto_smooth_angle = 3.141592653589793
        obj.data.normals_split_custom_set_from_vertices(normals)
    before = Snapshot(scene)""")
    (OUTPUT / 'Convert.py').write_text(converter)
    for name, model in {**models, **attachment_models}.items():
        requests = []
        chunks = {}
        for mesh, payload in model.pop('normals').items():
            keys = []
            for offset in range(0, len(payload), 98304):
                key = name + '_' + mesh + '_' + str(offset)
                keys.append(key)
                request_name = name + '_Normals_' + str(len(requests))
                request = dict(tool='execute_blender_code', arguments=dict(code='import bpy\n' + 'bpy.context.scene[' + repr(key) + '] = ' + repr(payload[offset:offset + 98304]), user_prompt='Import Republic at War’s Republic space stations into the Unity project:\nF:\\Private\\empire-at-war'))
                (OUTPUT / (request_name + '.json')).write_text(json.dumps(request))
                requests.append(request_name)
            chunks[mesh] = keys
        model['normal_chunks'] = chunks
        (OUTPUT / (name + '.json')).write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(
            code='AUDIT = ' + repr(dict(models={name: model}, textures=textures)) + '\n' + converter,
            user_prompt='Import Republic at War’s Republic space stations into the Unity project:\nF:\\Private\\empire-at-war'))))
        (OUTPUT / (name + '_Requests.json')).write_text(json.dumps(requests + [name]))
    for filename in ['BuildArt.cs', 'BuildView.cs', 'Verify.cs', 'VerifyGeometry.py', 'Render.cs', 'Start.py']:
        text = Adapt((REFERENCE / filename).read_text())
        if filename == 'BuildArt.cs':
            text = text.replace('bool normal = importer.assetPath.Contains("Bump");', 'bool normal = audit["textures"].Values().Any(t => (bool)t["normal"] && importer.assetPath.EndsWith((string)t["name"] + ".png"));')
            text = text.replace('material.SetFloat("_TeamLiveryStrength", 0);', 'material.SetFloat("_TeamLiveryStrength", 0);\n                    material.SetFloat("_TeamRimStrength", 0);')
            text = text.replace('renderer.name.EndsWith(" Shadow")', '!renderer.enabled && (renderer.name.ToLowerInvariant().Contains("shadow") || renderer.name.ToLowerInvariant().Contains("collision") || renderer.name.ToLowerInvariant().StartsWith("shield"))')
            # Preserve the original size before replacing the donor geometry, also on rebuilds.
            text = text.replace('float targetWidth = Mathf.Max(donorBounds.size.x, donorBounds.size.z);', 'float targetWidth = (float)JObject.Parse(File.ReadAllText(TASK + "Before.json"))["targetWidth"];')
            text = text.replace('importer.preserveHierarchy = true;', 'importer.preserveHierarchy = true;\n            importer.importNormals = ModelImporterNormals.Import;\n            importer.importTangents = ModelImporterTangents.Import;')
            text = text.replace('                // Unity permits repeated transform names;', '''                var sourceBones = (JArray)audit["models"][name]["bones"];
                var worldMatrices = new Matrix4x4[sourceBones.Count];
                var boneNames = ((JObject)report["source_bone_names"]).Properties().Select(p => p.Name).ToArray();
                var boneTransforms = model.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<Renderer>() == null).ToArray();
                var poses = model.GetComponentsInChildren<MeshRenderer>(true).Select(r => (Transform: r.transform, Position: r.transform.position, Rotation: r.transform.rotation, Scale: r.transform.lossyScale)).ToArray();
                var positions = boneNames.Select(n => boneTransforms.Single(t => t.name == n).position).ToArray();
                var basis = new Matrix4x4(new Vector4(-1, 0, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));
                for (int index = 0; index < sourceBones.Count; index++)
                {
                    var matrix = Matrix4x4.identity;
                    for (int row = 0; row < 3; row++)
                        for (int col = 0; col < 4; col++) matrix[row, col] = (float)sourceBones[index]["matrix"][row * 4 + col];
                    long parent = (long)sourceBones[index]["parent_index"];
                    worldMatrices[index] = parent == uint.MaxValue ? matrix : worldMatrices[parent] * matrix;
                    var bone = boneTransforms.Single(t => t.name == boneNames[index]);
                    bone.SetPositionAndRotation(positions[index], model.transform.rotation * (basis * worldMatrices[index] * basis.inverse).rotation);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(bone);
                }
                foreach (var pose in poses)
                {
                    pose.Transform.SetPositionAndRotation(pose.Position, pose.Rotation);
                    pose.Transform.localScale = new Vector3(pose.Scale.x / pose.Transform.parent.lossyScale.x, pose.Scale.y / pose.Transform.parent.lossyScale.y, pose.Scale.z / pose.Transform.parent.lossyScale.z);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(pose.Transform);
                }
                // Unity permits repeated transform names;''')
        if filename == 'BuildView.cs':
            start = text.index('        // No damaged Rebel ALO was supplied.')
            end = text.index('        AssetDatabase.SaveAssets();', start)
            text = text[:start] + text[end:]
            start = text.index('        var mapping =')
            end = text.index('        AssetDatabase.SaveAssets();', start)
            text = text[:start] + text[end:]
        if filename == 'Verify.cs':
            text = text.replace('m.Point == null || m.Art == null', 'm.Point == null')
            text = text.replace('["uv"] = new JArray(mesh.uv.Select(v => new JArray(v.x, v.y))),', '["uv"] = new JArray(mesh.uv.Select(v => new JArray(v.x, v.y))),\n                    ["normals"] = new JArray(mesh.normals.Select(n => V(renderer.localToWorldMatrix.inverse.transpose.MultiplyVector(n).normalized))),')
            text = text.replace('        var results = new JArray();', '        foreach (var attachment in JArray.Parse(File.ReadAllText(TASK + "AttachmentConversionReport.json"))) reports.Add(attachment);\n        var results = new JArray();')
            text = text.replace('            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);', '            if (!name.StartsWith("RepublicSpaceStationLevel")) modelPath = "Assets/Art/Models/SpaceStations/RepublicSpaceStation/Attachments/" + name + ".fbx";\n            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);')
            text = text.replace('            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");', '            if (!name.StartsWith("RepublicSpaceStationLevel")) { results.Add(new JObject { ["level"] = level, ["bones"] = boneReport, ["meshes"] = meshReport }); continue; }\n            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");')
        if filename == 'VerifyGeometry.py':
            text = text.replace('from Prepare import Chunks, ROOT, OUTPUT', 'import sys\nsys.path.insert(0, "Tools/Blender/RebelSpaceStation")\nfrom Prepare import Chunks\nROOT = Path.cwd()\nOUTPUT = ROOT / "Temp/RepublicStationImport"')
            text = text.replace("if 'Shadow' in row['materials'][0]['shader']:", "if 'Shadow' in row['materials'][0]['shader'] or 'Collision' in row['materials'][0]['shader']:")
            text = text.replace("    imported = json.loads((OUTPUT / 'UnityGeometry.json').read_text())", "    attachments = json.loads((OUTPUT / 'AttachmentAudit.json').read_text())\n    audit['models'].update(attachments['models'])\n    reports.extend(json.loads((OUTPUT / 'AttachmentConversionReport.json').read_text()))\n    imported = json.loads((OUTPUT / 'UnityGeometry.json').read_text())\n    assert len(imported) == len(reports) == len(audit['models'])")
            text = text.replace("dict(level=model['level'], bones=", "dict(name=report['name'], level=model['level'], bones=")
            text = text.replace('geometry_error = uv_error = 0', 'geometry_error = uv_error = normal_error = 0')
            text = text.replace('corners.append((*position, u, -v))', '''normal = struct.unpack_from('<3f', vertices, i * stride + 12)
                    transformed = UnityPosition(matrix, normal)
                    origin = UnityPosition(matrix, (0, 0, 0))
                    transformed = [n - o for n, o in zip(transformed, origin)]
                    length = math.sqrt(sum(n * n for n in transformed))
                    transformed = [n / length for n in transformed]
                    corners.append((*position, u, -v, *transformed))''')
            text = text.replace("corner = (*mesh['vertices'][index], *mesh['uv'][index])", "corner = (*mesh['vertices'][index], *mesh['uv'][index], *mesh['normals'][index])")
            text = text.replace('math.dist(nearest[3:], corner[3:])', 'math.dist(nearest[3:5], corner[3:5])')
            text = text.replace('position_error, texture_error = math.dist', 'normal_error = max(normal_error, math.dist(nearest[5:], corner[5:]))\n                assert normal_error < .0001, (name, normal_error)\n                position_error, texture_error = math.dist')
            text = text.replace('uv_error=uv_error))', 'uv_error=uv_error, normal_error=normal_error))')
            text = text.replace("    for texture in audit['textures'].values():", "    for name, digest in attachments['xml_hashes'].items():\n        assert hashlib.sha256((Path('D:/SteamLibrary/steamapps/workshop/content/32470/1129810972/Data/XML') / name).read_bytes()).hexdigest() == digest\n    for texture in audit['textures'].values():")
        if filename == 'Render.cs':
            start = text.index('                // Frame the common dome')
            end = text.index('                UnityEngine.Object.DestroyImmediate(root);', start)
            text = text[:start] + text[end:]
            text = text.replace('for (int team = 0; team < 8; team++)', 'for (int team = -1; team < 8; team++)')
            text = text.replace('five station levels in all eight team palettes and five dome close-ups.', 'five station levels in eight team palettes plus an unowned reference.')
        if filename == 'VerifyGeometry.py':
            text = text.replace("at the source texture's 512px resolution", "at 512px; under .042 texels at the 2048px hull resolution")
        (OUTPUT / filename).write_text(text)
    for name, model in models.items():
        print(name, 'bones', len(model['bones']), 'meshes', [(m['name'], [(s['shader'], s['triangleCount']) for s in m['materials']]) for m in model['meshes']])
    print('Attachments per level:', {level: len(rows) for level, rows in levels.items()}, 'unique:', len(names))
    print('Textures:', sorted(textures))


if __name__ == '__main__':
    Main()
