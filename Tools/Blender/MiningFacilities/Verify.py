"""Verify imported triangle positions/UVs directly against original ALO records."""
import json
import math
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/Blender/RebelSpaceStation'))
from Prepare import Chunks

OUTPUT = ROOT / 'Temp/MiningFacilitiesImport'
audit = json.loads((OUTPUT / 'SourceAudit.json').read_text())
log = (OUTPUT / 'ConvertResult.log').read_text()
reports = json.loads(log.split('CONVERSION_JSON ')[1])
for report in reports:
    source_error = uv_error = 0
    model = audit['models'][report['name']]
    for kind, payload in Chunks(Path(model['file']).read_bytes()):
        if kind != 0x400:
            continue
        fields = list(Chunks(payload))
        name = next(d.rstrip(b'\0').decode('ascii') for k, d in fields if k == 0x401)
        row = next(m for m in model['meshes'] if m['name'] == name)
        if 'Shadow' in row['materials'][0]['shader']:
            continue
        expected = []
        for chunk, data in fields:
            if chunk != 0x10000:
                continue
            geometry = dict(Chunks(data))
            vertex_count, triangle_count = struct.unpack_from('<II', geometry[0x10001])
            stride = 144 if 0x10007 in geometry else 128
            vertices = geometry[0x10007 if stride == 144 else 0x10005]
            slot = []
            for i in range(vertex_count):
                position = struct.unpack_from('<3f', vertices, i * stride)
                u, v = struct.unpack_from('<2f', vertices, i * stride + 24)
                slot.append((*position, u, -v))
            indices = struct.unpack('<' + 'H' * triangle_count * 3, geometry[0x10004])
            expected.extend(slot[i] for i in indices)
        actual = report['imported_geometry'][name]
        assert len(actual) == len(expected), name
        buckets = {}
        for corner in expected:
            buckets.setdefault(tuple(round(v, 5) for v in corner[3:]), []).append(corner)
        for corner in actual:
            candidates = buckets.get(tuple(round(v, 5) for v in corner[3:]), expected)
            nearest = min(candidates, key=lambda p: math.dist(p, corner))
            source_error = max(source_error, math.dist(nearest[:3], corner[:3]))
            uv_error = max(uv_error, math.dist(nearest[3:], corner[3:]))
    assert source_error < .002 and uv_error < .00001
    report['source_geometry_error'] = source_error
    report['source_uv_error'] = uv_error
    del report['imported_geometry']
    print(report['name'], 'source position/UV error', source_error, uv_error)
(OUTPUT / 'ConversionReport.json').write_text(json.dumps(reports[:3], indent=2))
(OUTPUT / 'AttachmentConversionReport.json').write_text(json.dumps(reports[3:], indent=2))
evidence = Path(__file__).with_name('Evidence')
evidence.mkdir(exist_ok=True)
for name in ('SourceAudit.json', 'ConversionReport.json', 'AttachmentConversionReport.json', 'Attachments.json'):
    (evidence / name).write_bytes((OUTPUT / name).read_bytes())
