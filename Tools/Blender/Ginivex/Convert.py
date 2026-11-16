import bpy, sys, os, json
from pathlib import Path
root=Path('F:/Private/empire-at-war')
sys.path.insert(0,str(root/'Tools/AloImport'))
sys.path.insert(0, os.path.join(os.environ['APPDATA'], 'Blender Foundation/Blender/3.6/scripts/addons'))
import io_alamo_tools
io_alamo_tools.register()
import blender_worker
original_compare=blender_worker.compare
def compare(expected,actual,mesh,spec):
 if all(slot['shader']=='MeshAdditive.fx' for slot in mesh['materials']):
  import math
  from mathutils import Vector
  def visible(corners):
   result=[]
   for i in range(0,len(corners),3):
    a,b,c=[Vector(row[:3]) for row in corners[i:i+3]]
    if (b-a).cross(c-a).length>0.000001: result.extend(corners[i:i+3])
   return result
  expected=[visible(x) for x in expected]
  actual=[visible(x) for x in actual]
  error=max(min(math.dist(c[:5],d[:5]) for d in got) for wanted,got in zip(expected,actual) for c in wanted)
  reverse=max(min(math.dist(c[:5],d[:5]) for d in wanted) for wanted,got in zip(expected,actual) for c in got)
  assert max(error,reverse)<0.001, (mesh['name'],error,reverse)
  return {'duplicateFlashCornersCollapsed':True,'maxPositionUvError':max(error,reverse)}
 return original_compare(expected,actual,mesh,spec)
blender_worker.compare=compare
spec=json.loads((root/'Temp/GinivexImport/Spec.json').read_text())
result=blender_worker.convert_piece(spec,spec['piece'])
(root/'Temp/GinivexImport/ConversionReport.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
