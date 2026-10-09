"""Run the audited exporter through the isolated, safe-mode Blender MCP."""
import json
import re
import subprocess
from pathlib import Path

TASK = Path('Temp/ISDIRemakeImport')
audits = json.loads((TASK/'SourceAudit.json').read_text())['variants']
exporter = Path('Tools/Blender/export_isd_i.py').read_text()
exporter = exporter.replace("scene = bpy.context.scene", "for collection in (bpy.data.meshes, bpy.data.armatures, bpy.data.materials):\n    for item in list(collection):\n        if item.users == 0:\n            collection.remove(item)\nscene = bpy.context.scene", 1)
repair = '''
for material in list(bpy.data.materials):
    if material.users == 0:
        bpy.data.materials.remove(material)
name_counts = {}
for record in AUDIT['material_audit']['meshes']:
    name = record['name']
    index = name_counts.get(name, 0)
    name_counts[name] = index + 1
    imported_name = name if index == 0 else name + '.' + str(index).zfill(3)
    obj = next(o for o in scene.objects if o.type == 'MESH' and o.name == imported_name)
    if not obj.hide_render:
        assert len(obj.data.polygons) == sum(m['triangleCount'] for m in record['materials']), obj.name
    obj.data.materials.clear()
    offset = 0
    for slot, definition in enumerate(record['materials']):
        material = bpy.data.materials.new(VARIANT + '_M' + str(len(bpy.data.materials)).zfill(3))
        material.shaderList.shaderList = definition['shader']
        material.BaseTexture = definition['properties'].get('BaseTexture', 'None')
        if 'Bump' in definition['shader']:
            material.NormalTexture = definition['properties']['NormalTexture']
        obj.data.materials.append(material)
        for polygon in obj.data.polygons[offset:offset + definition['triangleCount']]:
            polygon.material_index = slot
        offset += definition['triangleCount']
    if not obj.hide_render:
        assert [sum(len(p.vertices)-2 for p in obj.data.polygons if p.material_index == i) for i in range(len(record['materials']))] == [m['triangleCount'] for m in record['materials']]
'''
exporter = exporter.replace("rig = next(o for o in scene.objects if o.type == 'ARMATURE')", repair + "\nrig = next(o for o in scene.objects if o.type == 'ARMATURE')")
exporter = exporter.replace("shader, base = material.shaderList.shaderList, material.BaseTexture", "shader, base = material.shaderList.shaderList, material.BaseTexture\n    if base not in AUDIT['textures'] and base != 'None':\n        base = next(key for key in AUDIT['textures'] if key.lower() == base.lower())")
for name, audit in audits.items():
    if (TASK/name/'ConversionReport.json').exists():
        print(name, 'already converted', flush=True)
        continue
    output = (TASK/name).resolve().as_posix()
    code = "import bpy\nassert bpy.types.blendermcp_server.port == 9886\n"
    code += f'VARIANT={name!r}\nSOURCE={audit["file"]!r}\nAUDIT={audit!r}\nOUTPUT={output!r}\n' + exporter
    request = TASK/(name+'.request.json')
    request.write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(code=code, user_prompt='Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.'))))
    result = subprocess.run(['uvx','--from','mcp-for-blender==2.1.3','python','Tools/Blender/ISDIRemake/Call.py',str(request)], capture_output=True, text=True, encoding='utf-8')
    (TASK/(name+'.log')).write_text(result.stdout+'\n'+result.stderr, encoding='utf-8')
    if result.returncode:
        raise RuntimeError(result.stdout[-3000:] + result.stderr[-1000:])
    match = re.search(r'REPORT_JSON (.+)', result.stdout)
    if match is None:
        raise RuntimeError(result.stdout[-3000:])
    report = json.loads(match.group(1))
    (TASK/name/'ConversionReport.json').write_text(json.dumps(report, indent=2))
    print(name, 'geometry error', report['geometry_error'], 'bone error', report['bone_error'], flush=True)
