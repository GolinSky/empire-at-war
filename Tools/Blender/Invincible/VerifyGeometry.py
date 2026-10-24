"""Compare every triangle corner and UV of the converted Invincible transport FBX."""
import json
from pathlib import Path

TASK = Path(__file__).resolve().parents[3] / 'Temp/InvincibleImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
conversion = json.loads((TASK / 'ConversionReport.json').read_text())
before = json.loads((TASK / 'SourceGeometry.json').read_text())
after = json.loads((TASK / 'RoundtripGeometry.json').read_text())
assert set(before) == set(after)
rows = []
for name, corners in before.items():
    assert len(corners) == len(after[name]), name
    lookup = {}
    for corner in after[name]:
        lookup.setdefault(tuple(round(v, 5) for v in corner[3:]), []).append(corner)
    position_error = uv_error = 0
    for a in corners:
        candidates = lookup[tuple(round(v, 5) for v in a[3:])]
        b = min(candidates, key=lambda p: sum((a[i] - p[i]) ** 2 for i in range(3)))
        position_error = max(position_error, max(abs(a[i] - b[i]) for i in range(3)))
        uv_error = max(uv_error, max(abs(a[i] - b[i]) for i in range(3, 5)))
    assert position_error < .001 and uv_error < .00001, (name, position_error, uv_error)
    rows.append(dict(name=name, corners=len(corners), position_error=position_error, uv_error=uv_error))
names = conversion['source_bone_names']
world = []
bone_error = 0
for index, record in enumerate(audit['model']['bones']):
    parent = record['parent_index']
    expected_parent = names[parent] if parent != 4294967295 else None
    actual = conversion['before']['bones'][names[index]]
    assert actual['parent'] == expected_parent, (names[index], actual['parent'], expected_parent)
    local = [record['matrix'][row * 4:row * 4 + 4] for row in range(3)] + [[0, 0, 0, 1]]
    if parent != 4294967295:
        local = [[sum(world[parent][row][k] * local[k][column] for k in range(4)) for column in range(4)] for row in range(4)]
    world.append(local)
    bone_error = max(bone_error, max(abs(local[row][3] - actual['head'][row]) for row in range(3)))
assert bone_error < .001, bone_error
(TASK / 'GeometryReport.json').write_text(json.dumps(dict(success=True, meshes=rows, rawSourceBoneCount=len(names), rawSourceBonePositionError=bone_error, rawSourceParentsMatch=True), indent=2))
print(json.dumps(rows, indent=2))
print('Raw source hierarchy and 85 bone positions verified; maximum position error', bone_error)
