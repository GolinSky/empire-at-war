"""Use the established FBX binary normal restorer with Empress's per-vertex normal arrays."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
code = (ROOT / 'Tools/Blender/RepublicSpaceStation/RestoreNormals.py').read_text()
code = code.replace('Temp/RepublicStationImport','Temp/AotrEmpressStationImport')
code = code.replace("        normals = array.array('f')\n        normals.frombytes(bytes.fromhex(model['normals'][mesh_name]))", "        normals = array.array('f', [v for normal in model['normals'][mesh_name] for v in normal])")
# Shadow/collision helper topology is simplified by ALAMO and has no authored normal restoration.
code = code.replace("    path = TASK / 'Output'", "    model['normals'] = {r['key']: model['normals'][r['key']] for r in model['meshes'] if not any(t in r['materials'][0]['shader'] for t in ('Shadow','Collision'))}\n    path = TASK / 'Output'")
exec(compile(code,str(ROOT / 'Tools/Blender/RepublicSpaceStation/RestoreNormals.py'),'exec'))
