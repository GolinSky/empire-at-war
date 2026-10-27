"""Compare Recalcitrant source/FBX triangle corners, UVs and raw source bone hierarchy."""
import json
from pathlib import Path
TASK = Path(__file__).resolve().parents[3] / 'Temp/RecalcitrantImport'
audit = json.loads((TASK / 'SourceAudit.json').read_text())
conversions = json.loads((TASK / 'ConversionReport.json').read_text())['models']
reports = {}
for model_name, conversion in conversions.items():
    rows = conversion['geometry_checks']
    assert all(row['position_error'] < .001 and row['uv_error'] < .00001 for row in rows if row['verified'])
    exceptions = [row for row in rows if not row['verified']]
    assert not exceptions or model_name == 'Recalcitrant' and len(exceptions) == 1 and exceptions[0]['name'] == 'Col'
    names = conversion['source_bone_names']
    world = []
    bone_error = 0
    for index, record in enumerate(audit['models'][model_name]['model']['bones']):
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

    reports[model_name] = dict(meshes=rows, rawSourceBoneCount=len(names), rawSourceBonePositionError=bone_error, rawSourceParentsMatch=True, transportExceptions=exceptions)
(TASK / 'GeometryReport.json').write_text(json.dumps(dict(success=True, models=reports), indent=2))
print(json.dumps(reports, indent=2))
