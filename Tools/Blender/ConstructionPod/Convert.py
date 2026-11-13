"""Run in an owned background Blender 3.6 process; conversion clears its scene."""
import json
import sys
from pathlib import Path
import bpy
from mathutils import Vector

PROJECT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(PROJECT / 'Tools/AloImport'))
import blender_worker
from source_audit import audit_alo


def AuditWithRepairedNormals(path, piece_id, include_geometry=False):
    audit = audit_alo(path, piece_id, include_geometry)
    assert audit['sha256'] == '8987c68721f5caada20967d5e36da8f6fc4e49b51ece5ffcbb281e3cfc68a2b7'
    if include_geometry:
        slot = audit['meshes'][0]['materials'][0]
        invalid = [i for i, normal in enumerate(slot['normals']) if Vector(normal).length == 0]
        assert invalid == [580,588,593,780,5390,5409,5430,5493,5498,5518,5533,5627,5628,5683,5684,5781,5782,5785,5786,5806,5821,5822,5837,5838]
        sums = {i: Vector((0, 0, 0)) for i in invalid}
        for start in range(0, len(slot['triangles']), 3):
            indices = slot['triangles'][start:start + 3]
            a, b, c = [Vector(slot['positions'][i]) for i in indices]
            normal = (b - a).cross(c - a)
            for i in indices:
                if i in sums:
                    sums[i] += normal
        for i, normal in sums.items():
            assert normal.length > 0
            slot['normals'][i] = list(normal.normalized())
    return audit

output = PROJECT / 'Temp/ConstructionPodImport'
spec = json.loads((output / 'Spec.json').read_text())
assert bpy.app.version[:2] == (3, 6), bpy.app.version_string
bpy.ops.preferences.addon_enable(module='io_alamo_tools')
bpy.ops.preferences.addon_enable(module='io_scene_fbx')
blender_worker.audit_alo = AuditWithRepairedNormals
result = blender_worker.convert_piece(spec, spec['piece'])
result['repairs'].append({'meshIndex': 0, 'repair': '24 source-zero normals replaced by area-weighted incident face normals; 28 triangle corners affected'})
(output / 'ConversionReport.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result))
