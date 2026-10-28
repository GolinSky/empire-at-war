"""Build one safe-mode Blender MCP request from the source audit."""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'Temp/DefenseSatellitesImport'
name = sys.argv[1]
audit = json.loads((TASK / 'SourceAudit.json').read_text())
model = audit['models'][name]
audit['models'] = {}
for mesh, normals in model['normals'].items():
    values, indices, lookup = [], [], {}
    for normal in normals:
        key = tuple(round(value, 5) for value in normal)
        if key not in lookup:
            lookup[key] = len(values)
            values.append(key)
        indices.append(lookup[key])
    model['normals'][mesh] = dict(values=values, indices=','.join(map(str, indices)))
code = 'import bpy\nimport json\n'
code += 'AUDIT = json.loads(' + repr(json.dumps(audit, separators=(',', ':'))) + ')\n'
code += "bpy.context.window.scene = bpy.data.scenes.new('Defense Satellites Source')\n"
code += (Path(__file__).with_name('Convert.py')).read_text()
code += '\nmodel = json.loads(' + repr(json.dumps(model, separators=(',', ':'))) + ')\n'
code += 'report = Convert(' + repr(name) + ', model)\n'
code += "print('CONVERSION_JSON ' + json.dumps(report))\n"
code += 'for obj in list(bpy.context.scene.objects):\n    bpy.data.objects.remove(obj, do_unlink=True)\n'
assert len(code.encode()) <= 200000
print(json.dumps(dict(tool='execute_blender_code', arguments=dict(code=code))))
