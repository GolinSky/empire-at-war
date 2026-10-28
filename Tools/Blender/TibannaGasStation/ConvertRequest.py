"""Expand reviewed code locally and stage audit data in bounded MCP requests."""
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[3]
TASK = ROOT / 'output/TibannaGasStationImport'
wrapper = Path(__file__).with_name('Convert.py').read_text()
wrapper = wrapper[:wrapper.index('exec(compile')]
wrapper = '\n'.join(line for line in wrapper.splitlines() if not line.startswith(('import bpy', 'assert bpy.', 'original_scene =', 'bpy.context.window.scene =')))
namespace = {}
exec(wrapper, namespace)
for model in namespace['AUDIT']['models'].values():
    model['normals'] = {k:v for k,v in model['normals'].items() if k in ('Hull', 'lights')}
data = json.dumps(namespace['AUDIT'])
for i, offset in enumerate(range(0, len(data), 100000)):
    prefix = "import bpy\nbpy.data.scenes.new('Tibanna Audit')\n" if i == 0 else "import bpy\n"
    code = prefix + "bpy.data.scenes['Tibanna Audit']['chunk" + str(i) + "'] = " + repr(data[offset:offset+100000])
    (TASK / f'AuditRequest{i:02}.json').write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(code=code,user_prompt='Add Tibanna gas station unit'))))
code = namespace['converter'].replace("Path(TASK / 'RawConversionReport.json').write_text(json.dumps(reports))", "print('CONVERSION_JSON ' + json.dumps(reports))")
code = "import bpy, json\nAUDIT = json.loads(''.join(bpy.data.scenes['Tibanna Audit']['chunk'+str(i)] for i in range(CHUNK_COUNT)))\noriginal_scene = bpy.context.scene\nbpy.context.window.scene = bpy.data.scenes.new('Tibanna Conversion')\n" + code + "\nbpy.context.window.scene = original_scene\nbpy.data.scenes.remove(bpy.data.scenes['Tibanna Audit'])\n"
code = code.replace('CHUNK_COUNT', str((len(data)+99999)//100000))
(TASK / 'ConvertRequest.json').write_text(json.dumps(dict(tool='execute_blender_code', arguments=dict(code=code,user_prompt='Add Tibanna gas station unit'))))
