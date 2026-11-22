"""Convert each Viscount ALO to a verified FBX in its own windowed Blender 3.6.23 process.

Run from the repository root after Prepare.py: python Tools/Blender/Viscount/Convert.py
Rebuilds per-mesh materials from the binary audit (ALAMO merges different shaders that share a texture),
then runs the shared exporter Tools/Blender/export_isd_i.py, which round-trips the FBX and asserts geometry.
"""
import json
import os
import subprocess
import sys
from pathlib import Path

TASK = Path('Temp/ViscountImport').resolve()
BLENDER = Path(os.environ['LOCALAPPDATA']) / 'AI-Tools/Blender/blender-3.6.23-windows-x64/blender.exe'

REPAIR = '''
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
        shader = definition['shader']
        known = [item.identifier for item in material.shaderList.bl_rna.properties['shaderList'].enum_items]
        if shader not in known:
            # ALAMO lacks e.g. MeshAdditive2x.fxo; keep the source name and use the nearest known blend.
            material['EaWSourceShader'] = shader
            shader = 'MeshAdditive.fx' if 'Additive' in shader else 'alDefault.fx'
        material.shaderList.shaderList = shader
        material.BaseTexture = definition['properties'].get('BaseTexture', 'None')
        if 'Bump' in definition['shader']:
            material.NormalTexture = definition['properties']['NormalTexture']
        obj.data.materials.append(material)
        for polygon in obj.data.polygons[offset:offset + definition['triangleCount']]:
            polygon.material_index = slot
        offset += definition['triangleCount']
'''

WRAPPER = '''
import bpy, sys, traceback
try:
    exec(compile(open(CODE_PATH, encoding='utf-8').read(), CODE_PATH, 'exec'), {'__name__': '__viscount__'})
    print('CONVERT_OK', flush=True)
except Exception:
    traceback.print_exc()
    print('CONVERT_FAILED', flush=True)
finally:
    sys.stdout.flush()
    bpy.ops.wm.quit_blender()
'''


def Main():
    audits = json.loads((TASK / 'SourceAudit.json').read_text(encoding='utf-8'))['variants']
    exporter = Path('Tools/Blender/export_isd_i.py').read_text(encoding='utf-8')
    exporter = exporter.replace("rig = next(o for o in scene.objects if o.type == 'ARMATURE')",
                                REPAIR + "\nrig = next(o for o in scene.objects if o.type == 'ARMATURE')", 1)
    exporter = exporter.replace(
        "shader, base = material.shaderList.shaderList, material.BaseTexture",
        "shader, base = material.shaderList.shaderList, material.BaseTexture\n"
        "    if base not in AUDIT['textures'] and base != 'None':\n"
        "        base = next(key for key in AUDIT['textures'] if key.lower() == base.lower())", 1)
    # The shared check uses an absolute 0.001 limit; float32 spacing near 8,000 units is ~0.001,
    # so the 7,864-unit Dreadnought needs a size-scaled limit (prototype keeps 0.001).
    exporter = exporter.replace("assert error < .001, (obj.name, error)", "assert error < GEOMETRY_TOLERANCE, (obj.name, error)", 1)
    exporter = exporter.replace("for p, uv in triangle) < .001 for i in candidates), obj.name",
                                "for p, uv in triangle) < GEOMETRY_TOLERANCE for i in candidates), obj.name", 1)
    assert exporter.count('GEOMETRY_TOLERANCE') == 2
    tolerances = {'ViscountPrototype': 0.001, 'ViscountDreadnought': 0.005}
    variants = sys.argv[1:] or list(audits)
    for name in variants:
        output = (TASK / name)
        code = (f'VARIANT={name!r}\nSOURCE={audits[name]["file"]!r}\nAUDIT={audits[name]!r}\nOUTPUT={output.as_posix()!r}\n'
                f'GEOMETRY_TOLERANCE={tolerances[name]!r}\n'
                + exporter
                + "\nimport json as _json\nopen(OUTPUT + '/Report.json', 'w').write(_json.dumps(dict(variant=VARIANT, materials=materials, bone_error=bone_error, geometry_error=geometry_error, before=before), indent=1))\n")
        code_path = output / 'ConvertBody.py'
        code_path.write_text(code, encoding='utf-8')
        wrapper_path = output / 'ConvertRun.py'
        wrapper_path.write_text(f'CODE_PATH = {code_path.as_posix()!r}\n' + WRAPPER, encoding='utf-8')
        log = output / 'Conversion.log'
        with log.open('w', encoding='utf-8') as stream:
            subprocess.run([str(BLENDER), '--python', str(wrapper_path)], stdout=stream, stderr=subprocess.STDOUT, timeout=1800)
        text = log.read_text(encoding='utf-8', errors='replace')
        status = 'OK' if 'CONVERT_OK' in text else 'FAILED'
        print(name, status, log)
        if status != 'OK':
            print(text[-3000:])
            raise SystemExit(1)


if __name__ == '__main__':
    Main()
