"""Convert audited models through the configured Blender 3.6 MCP connection."""
import json
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path('Temp/ImperialVenatorImport')
variants = json.loads((ROOT / 'SourceAudit.json').read_text())['variants']
exporter = Path('Tools/Blender/export_isd_i.py').read_text()
repair = '''
for record in AUDIT['binary']['meshes']:
    obj = next(o for o in bpy.context.scene.objects if o.type == 'MESH' and o.name.split('.')[0] == record['name'])
    shadow = all(m['shader'] in ('MeshShadowVolume.fx', 'RSkinShadowVolume.fx') for m in record['materials'])
    if not shadow:
        assert len(obj.data.polygons) == sum(m['triangleCount'] for m in record['materials']), record['name']
    obj.hide_render = record['hidden'] or shadow
    obj.hide_set(obj.hide_render)
    obj.data.materials.clear()
    offset = 0
    for slot, definition in enumerate(record['materials']):
        material = bpy.data.materials.new(VARIANT + '_' + record['name'] + '_Slot' + str(slot).zfill(2))
        material.shaderList.shaderList = definition['shader']
        material.BaseTexture = definition['properties'].get('BaseTexture', 'None')
        material.NormalTexture = definition['properties'].get('NormalTexture', 'None')
        for key, value in definition['properties'].items():
            material[key] = value
        obj.data.materials.append(material)
        for polygon in obj.data.polygons[offset:offset + definition['triangleCount']]:
            polygon.material_index = slot
        offset += definition['triangleCount']
'''
exporter = exporter.replace("assert len(rig.data.bones) == len(AUDIT['bones'])", "assert len(rig.data.bones) == len(AUDIT['bones'])\n" + repair)
exporter = exporter.replace("scene = bpy.context.scene", "bpy.ops.outliner.orphans_purge(do_recursive=True)\nscene = bpy.context.scene")
# Binary helper meshes can have no UVs; preserve that authored state.
exporter = exporter.replace("obj.data.uv_layers.active.data[l].uv.copy()", "obj.data.uv_layers.active.data[l].uv.copy() if obj.data.uv_layers.active else Vector((0, 0))")
exporter = exporter.replace("assert all(m['uv_layers'] == 1 for m in after['meshes'].values())", "assert all(before['meshes'][n]['uv_layers'] == after['meshes'][n]['uv_layers'] for n in before['meshes'])")
for name, audit in variants.items():
    if sys.argv[1:] and name not in sys.argv[1:]:
        continue
    output = (ROOT / name).resolve().as_posix()
    code = f"VARIANT={name!r}\nSOURCE={audit['file']!r}\nAUDIT={audit!r}\nOUTPUT={output!r}\n" + exporter
    request = ROOT / (name + '.json')
    request.write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(code=code, user_prompt='Integrate the Imperial Venator into the Empire faction using Steam Workshop mod 1770851727.'))), encoding='utf-8')
    result = subprocess.run(['uvx', '--from', 'mcp-for-blender==2.1.3', 'python', 'Tools/Blender/mcp_call.py', str(request)], capture_output=True, text=True)
    (ROOT / (name + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
    match = re.search(r'REPORT_JSON (.+)', result.stdout)
    if result.returncode or match is None:
        raise RuntimeError(result.stdout + result.stderr)
    report = json.loads(match.group(1))
    (ROOT / name / 'ConversionReport.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print(name, 'geometry error', report['geometry_error'], 'bone error', report['bone_error'], flush=True)
