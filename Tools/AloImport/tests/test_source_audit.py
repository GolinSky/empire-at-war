import json
import struct
import sys
import tempfile
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from source_audit import SourceError, audit_alo, chunks, resolve_source, read_xml_definitions, resolve_xml_unit


def chunk(kind, data):
    return struct.pack('<II', kind, len(data)) + data


def model(connected_objects=(0, 1)):
    bone = chunk(0x203, b'Root\0') + chunk(0x206, struct.pack('<III12f', 0xffffffff, 1, 0, 1,0,0,0,0,1,0,0,0,0,1,0))
    skeleton = chunk(0x200, chunk(0x201, b'\0'*128) + chunk(0x202, bone))
    mesh = chunk(0x401, b'Duplicate\0') + chunk(0x402, b'\0'*40)
    for texture in (b'one.dds\0', b'two.dds\0'):
        parameter = bytes([1,12])+b'BaseTexture\0'+bytes([2,len(texture)])+texture
        mesh += chunk(0x10100, chunk(0x10101,b'Opaque\0')+chunk(0x10105,parameter))
        mesh += chunk(0x10000, chunk(0x10001,struct.pack('<II',3,1)))
    connections = b''.join(chunk(0x602, struct.pack('<BBIBBI', 1, 4, index, 2, 4, 0))
                           for index in connected_objects)
    return skeleton + chunk(0x400, mesh) + chunk(0x400, mesh) + chunk(0x600, connections)


class SourceAuditTests(unittest.TestCase):
    def test_original_indices_and_material_signatures(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)/'test.alo'
            path.write_bytes(model())
            audit = audit_alo(path,'hull')
            self.assertEqual([m['index'] for m in audit['meshes']], [0,1])
            self.assertEqual([m['name'] for m in audit['meshes']], ['Duplicate']*2)
            materials = audit['meshes'][0]['materials']
            self.assertEqual(len(materials),2)
            self.assertNotEqual(materials[0]['signature'],materials[1]['signature'])
            self.assertEqual(audit['bones'][0]['parent_index'],0xffffffff)

    def test_mesh_requires_one_bone_connection(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)/'test.alo'
            for connected_objects in ((), (0, 0, 1)):
                with self.subTest(connected_objects=connected_objects):
                    path.write_bytes(model(connected_objects))
                    with self.assertRaisesRegex(SourceError, 'exactly one source bone connection'):
                        audit_alo(path, 'hull')

    def test_truncated_chunks_fail(self):
        for data in (b'x',chunk(1,b'abc')[:-1]):
            with self.assertRaises(SourceError):
                list(chunks(data))

    def test_resolution_is_case_insensitive_and_root_confined(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory)/'Models'
            path.mkdir()
            (path/'Hull.ALO').write_bytes(b'data')
            self.assertEqual(resolve_source(directory,'models/hull.alo'),path/'Hull.ALO')
            for relative in ('../secret','C:/outside','/outside'):
                with self.assertRaises(SourceError):
                    resolve_source(directory,relative)
            with self.assertRaises(SourceError) as error:
                resolve_source(directory,'Models/missing.alo')
            self.assertEqual(error.exception.code,'SOURCE_MISSING')

    def test_xml_inheritance_repeats_and_cycle(self):
        with tempfile.TemporaryDirectory() as directory:
            path=Path(directory)/'units.xml'
            path.write_text('<Root><Unit Name="Base"><Health>1</Health><Spawn>A</Spawn><Spawn>B</Spawn></Unit><Unit Name="Variant"><Variant_Of_Existing_Type>Base</Variant_Of_Existing_Type><Health>2</Health></Unit></Root>')
            definitions=read_xml_definitions([path])
            result=resolve_xml_unit(definitions,'Variant')
            self.assertEqual(result['values']['Health'],['2'])
            self.assertEqual(result['values']['Spawn'],['A','B'])
            with self.assertRaises(SourceError):
                read_xml_definitions([path,path])
            definitions['Base'][0].append(__import__('xml.etree.ElementTree',fromlist=['Element']).fromstring('<Variant_Of_Existing_Type>Variant</Variant_Of_Existing_Type>'))
            with self.assertRaises(SourceError):
                resolve_xml_unit(definitions,'Variant')


if __name__=='__main__':
    unittest.main()
