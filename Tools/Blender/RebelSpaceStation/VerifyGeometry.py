"""Compare imported Unity geometry, UVs and bone hierarchy directly with the original ALO binaries."""
import hashlib
import itertools
import json
import math
import struct
from collections import defaultdict
from pathlib import Path
from PIL import Image
from Prepare import Chunks, ROOT, OUTPUT


def Multiply(a, b):
    return [[sum(a[r][k] * b[k][c] for k in range(4)) for c in range(4)] for r in range(4)]


def UnityPosition(matrix, vertex):
    point = [sum(matrix[r][c] * (*vertex, 1)[c] for c in range(4)) for r in range(3)]
    return [-point[0] * .02, point[2] * .02, -point[1] * .02]


def Main():
    audit = json.loads((OUTPUT / 'SourceAudit.json').read_text())
    reports = json.loads((OUTPUT / 'ConversionReport.json').read_text())
    imported = json.loads((OUTPUT / 'UnityGeometry.json').read_text())
    results = []
    for actual, report in zip(imported, reports):
        model = audit['models'][report['name']]
        source = Path(model['file'])
        assert hashlib.sha256(source.read_bytes()).hexdigest() == model['sha256']
        matrices, names = [], []
        for row in model['bones']:
            matrix = [row['matrix'][i:i + 4] for i in (0, 4, 8)] + [[0, 0, 0, 1]]
            if row['parent_index'] != 4294967295:
                matrix = Multiply(matrices[row['parent_index']], matrix)
            matrices.append(matrix)
            name = row['name']
            if name in names:
                base, separator, suffix = name.rpartition('.')
                base = base if separator and suffix.isdigit() else name
                number = 1
                while f'{base}.{number:03}' in names:
                    number += 1
                name = f'{base}.{number:03}'
            names.append(name)
        assert set(actual['bones']) == set(names)
        bone_error = 0
        for index, (name, row) in enumerate(zip(names, model['bones'])):
            parent = None if row['parent_index'] == 4294967295 else names[row['parent_index']]
            assert actual['bones'][name]['parent'] == parent, (name, parent)
            error = math.dist(actual['bones'][name]['position'], UnityPosition(matrices[index], (0, 0, 0)))
            assert error < .0001, (name, error)
            bone_error = max(bone_error, error)
        geometry_error = uv_error = 0
        checked = 0
        for kind, payload in Chunks(source.read_bytes()):
            if kind != 0x400:
                continue
            fields = list(Chunks(payload))
            name = next(data.rstrip(b'\0').decode('ascii') for chunk, data in fields if chunk == 0x401)
            row = next(m for m in model['meshes'] if m['name'] == name)
            if 'Shadow' in row['materials'][0]['shader']:
                continue
            connection = next(c for c in model['connections'] if c['mesh'] == row['object_index'])
            matrix = matrices[connection['bone']]
            expected = []
            for chunk, data in fields:
                if chunk != 0x10000:
                    continue
                geometry = dict(Chunks(data))
                vertex_count, triangle_count = struct.unpack_from('<II', geometry[0x10001])
                stride = 144 if 0x10007 in geometry else 128
                vertices = geometry[0x10007 if stride == 144 else 0x10005]
                corners = []
                for i in range(vertex_count):
                    position = UnityPosition(matrix, struct.unpack_from('<3f', vertices, i * stride))
                    u, v = struct.unpack_from('<2f', vertices, i * stride + 24)
                    corners.append((*position, u, -v))
                indices = struct.unpack('<' + 'H' * triangle_count * 3, geometry[0x10004])
                expected.extend(corners[i] for i in indices)
            mesh = actual['meshes'][name]
            assert len(mesh['triangles']) == len(expected), name
            assert all(path.startswith('Assets/Art/Materials/Models/SpaceStations/RebelSpaceStation/') for path in mesh['materials'])
            buckets = defaultdict(list)
            for corner in expected:
                buckets[tuple(round(p * 1000) for p in corner[:3])].append(corner)
            for index in mesh['triangles']:
                corner = (*mesh['vertices'][index], *mesh['uv'][index])
                key = tuple(round(p * 1000) for p in corner[:3])
                candidates = [p for offset in itertools.product((-1, 0, 1), repeat=3)
                              for p in buckets.get(tuple(a + b for a, b in zip(key, offset)), ())]
                nearest = min(candidates, key=lambda p: math.dist(p, corner))
                position_error, texture_error = math.dist(nearest[:3], corner[:3]), math.dist(nearest[3:], corner[3:])
                # UV tolerance is under .011 texels at the source texture's 512px resolution.
                assert position_error < .0001 and texture_error < .00002, (name, position_error, texture_error)
                geometry_error, uv_error = max(geometry_error, position_error), max(uv_error, texture_error)
            checked += len(expected) // 3
        results.append(dict(level=model['level'], bones=len(names), triangles=checked,
                            bone_error=bone_error, geometry_error=geometry_error, uv_error=uv_error))
    for texture in audit['textures'].values():
        source = Path(texture['source'])
        target = ROOT / 'Assets/Art/Textures/Models/SpaceStations/RebelSpaceStation' / (texture['name'] + '.png')
        assert hashlib.sha256(source.read_bytes()).hexdigest() == texture['sha256']
        assert Image.open(source).convert('RGBA').tobytes() == Image.open(target).convert('RGBA').tobytes()
    (OUTPUT / 'VerifiedSourceGeometry.json').write_text(json.dumps(results, indent=2))
    print(json.dumps(results, indent=2))
    print('All original model/texture hashes unchanged; staged texture pixels match decoded DDS exactly.')


if __name__ == '__main__':
    Main()
