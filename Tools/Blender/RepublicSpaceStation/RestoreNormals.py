"""Restore exact ALO normals after Blender quantizes custom/degenerate-face normals."""
import array
import importlib
import json
import os
import sys
import types
from pathlib import Path

TASK = Path('Temp/RepublicStationImport')
FBX_TOOLS = Path(os.environ['LOCALAPPDATA']) / 'AI-Tools/Blender/blender-3.6.23-windows-x64/3.6/scripts/addons/io_scene_fbx'
package = types.ModuleType('station_fbx')
package.__path__ = [str(FBX_TOOLS)]
sys.modules[package.__name__] = package
parse = importlib.import_module('station_fbx.parse_fbx')
encode = importlib.import_module('station_fbx.encode_bin')
data_types = importlib.import_module('station_fbx.data_types')
ADDERS = {getattr(data_types, name): 'add_' + name.lower() for name in (
    'BOOL', 'INT8', 'INT16', 'INT32', 'INT64', 'FLOAT32', 'FLOAT64', 'BYTES', 'STRING',
    'INT32_ARRAY', 'INT64_ARRAY', 'FLOAT32_ARRAY', 'FLOAT64_ARRAY', 'BOOL_ARRAY', 'BYTE_ARRAY')}


def Clone(source):
    target = encode.FBXElem(source.id)
    for kind, value in zip(source.props_type, source.props):
        getattr(target, ADDERS[kind])(value)
    target.elems = [Clone(child) for child in source.elems]
    return target


audit = json.loads((TASK / 'SourceAudit.json').read_text())
for name, model in audit['models'].items():
    path = TASK / 'Output' / (name + '.fbx')
    tree, version = parse.parse(str(path))
    objects = next(e for e in tree.elems if e.id == b'Objects')
    restored = 0
    for geometry in objects.elems:
        if geometry.id != b'Geometry' or geometry.props[2] != b'Mesh': continue
        mesh_name = geometry.props[1].split(b'\0', 1)[0].decode()
        if mesh_name not in model['normals']: continue
        normals = array.array('f')
        normals.frombytes(bytes.fromhex(model['normals'][mesh_name]))
        vertices = next(e for e in geometry.elems if e.id == b'Vertices').props[0]
        assert len(vertices) == len(normals), (name, mesh_name)
        indices = next(e for e in geometry.elems if e.id == b'PolygonVertexIndex').props[0]
        layer = next(e for e in geometry.elems if e.id == b'LayerElementNormal')
        assert next(e for e in layer.elems if e.id == b'MappingInformationType').props == [b'ByPolygonVertex']
        assert next(e for e in layer.elems if e.id == b'ReferenceInformationType').props == [b'Direct']
        values = array.array('d')
        for index in indices:
            index = index if index >= 0 else ~index
            values.extend(list(normals[index * 3:index * 3 + 3]))
        next(e for e in layer.elems if e.id == b'Normals').props[0] = values
        restored += 1
    assert restored == len(model['normals']), name
    encode.write(str(path), Clone(tree), version)
    print(name, 'restored exact normals on', restored, 'meshes')
