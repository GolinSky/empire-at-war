"""Compare original ALO normals to Unity triangle corners, independent of ALAMO."""
import itertools
import json
import math
import struct
import sys
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'Tools/Blender/RebelSpaceStation'))
from Prepare import Chunks
from VerifyGeometry import Multiply, UnityPosition

TASK = ROOT / 'Temp/CisStationImport'


def Main():
    audit = json.loads((TASK / 'SourceAudit.json').read_text())
    imported = json.loads((TASK / 'UnityGeometry.json').read_text())
    results = []
    for actual, model in zip(imported, audit['models'].values()):
        matrices = []
        for bone in model['bones']:
            matrix = [bone['matrix'][i:i + 4] for i in (0, 4, 8)] + [[0, 0, 0, 1]]
            if bone['parent_index'] != 4294967295:
                matrix = Multiply(matrices[bone['parent_index']], matrix)
            matrices.append(matrix)
        checked, degenerate, maximum = 0, 0, 0
        for kind, payload in Chunks(Path(model['file']).read_bytes()):
            if kind != 0x400:
                continue
            fields = list(Chunks(payload))
            name = next(data.rstrip(b'\0').decode('ascii') for chunk, data in fields if chunk == 0x401)
            source = next(m for m in model['meshes'] if m['name'] == name)
            if any(token in source['materials'][0]['shader'] for token in ('Shadow', 'Collision')):
                continue
            connection = next(c for c in model['connections'] if c['mesh'] == source['object_index'])
            matrix = matrices[connection['bone']]
            origin = UnityPosition(matrix, (0, 0, 0))
            corners = []
            for chunk, data in fields:
                if chunk != 0x10000:
                    continue
                geometry = dict(Chunks(data))
                count, triangles = struct.unpack_from('<II', geometry[0x10001])
                stride = 144 if 0x10007 in geometry else 128
                vertices = geometry[0x10007 if stride == 144 else 0x10005]
                values = []
                for index in range(count):
                    position = UnityPosition(matrix, struct.unpack_from('<3f', vertices, index * stride))
                    normal = [n - o for n, o in zip(UnityPosition(matrix, struct.unpack_from('<3f', vertices, index * stride + 12)), origin)]
                    length = math.sqrt(sum(n * n for n in normal))
                    normal = [n / length for n in normal]
                    u, v = struct.unpack_from('<2f', vertices, index * stride + 24)
                    values.append((*position, u, -v, *normal))
                indices = struct.unpack('<' + 'H' * triangles * 3, geometry[0x10004])
                for offset in range(0, len(indices), 3):
                    triangle = [values[index] for index in indices[offset:offset + 3]]
                    a, b, c = triangle
                    ab = [y - x for x, y in zip(a[:3], b[:3])]
                    ac = [y - x for x, y in zip(a[:3], c[:3])]
                    cross = [ab[1] * ac[2] - ab[2] * ac[1], ab[2] * ac[0] - ab[0] * ac[2], ab[0] * ac[1] - ab[1] * ac[0]]
                    is_degenerate = all(value == 0 for value in cross)
                    corners.extend((*corner, is_degenerate) for corner in triangle)
            buckets = defaultdict(list)
            for corner in corners:
                buckets[tuple(round(v * 1000) for v in corner[:3])].append(corner)
            mesh = actual['meshes'][name]
            for index in mesh['triangles']:
                corner = (*mesh['vertices'][index], *mesh['uv'][index], *mesh['normals'][index])
                key = tuple(round(v * 1000) for v in corner[:3])
                candidates = [p for offset in itertools.product((-1, 0, 1), repeat=3)
                              for p in buckets.get(tuple(a + b for a, b in zip(key, offset)), ())]
                nearest = min(candidates, key=lambda p: math.dist(p[:8], corner))
                # Blender cannot encode split normals on a zero-area face. Geometry/UV checks still include it.
                if nearest[8]:
                    degenerate += 1
                    continue
                error = math.dist(nearest[5:8], corner[5:])
                assert error < .001, (model['level'], name, error, nearest, corner)
                maximum = max(maximum, error)
                checked += 1
        results.append(dict(level=model['level'], corners=checked, degenerate_corners=degenerate, normal_error=maximum))
    (TASK / 'VerifiedNormals.json').write_text(json.dumps(results, indent=2))
    print(json.dumps(results, indent=2))


if __name__ == '__main__':
    Main()
