import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from converter import convert_job
from fidelity import compare_triangles
from storage import PipelineError, file_hash, owner, read_json, write_json


def corner(x,y,u=0):
    return (x,y,0,u,0,0,0,1)


class FidelityTests(unittest.TestCase):
    def test_triangle_rotation_passes_but_winding_and_rewire_fail(self):
        expected=[corner(0,0),corner(1,0),corner(1,1),corner(0,0),corner(1,1),corner(0,1)]
        compare_triangles(expected,expected[1:3]+expected[:1]+expected[3:])
        for invalid in (expected[:1]+expected[1:3][::-1]+expected[3:],
                        [expected[0],expected[1],expected[5],expected[1],expected[2],expected[5]]):
            with self.assertRaises(ValueError):
                compare_triangles(expected,invalid)

    def test_pixel_uv_and_normal_thresholds(self):
        expected=[corner(0,0),corner(1,0),corner(1,1)]
        wrong=[tuple([*p[:3],.001,*p[4:]]) for p in expected]
        with self.assertRaises(ValueError):
            compare_triangles(expected,wrong,1024)
        wrong=[tuple([*p[:5],0,1,0]) for p in expected]
        with self.assertRaises(ValueError):
            compare_triangles(expected,wrong)
        compare_triangles(expected,wrong,ignore_normals=True)


class ConversionCacheTests(unittest.TestCase):
    def setup_job(self,root,name='job'):
        job=root/'Temp/AloImport/Jobs'/name
        job.mkdir(parents=True)
        blender=root/'blender.exe'
        blender.write_bytes(b'blender')
        addon=root/'io_alamo_tools'
        addon.mkdir(exist_ok=True)
        (addon/'import_alo.py').write_text('pass')
        source=root/'hull.alo'
        source.write_bytes(b'source')
        frozen={'jobId':name,'recipe':{},'config':{'blender':str(blender),'alamoAddon':str(addon),'blenderWorkers':1}}
        write_json(job/'SourceAudit.json',{'files':[{'path':str(source),'sha256':file_hash(source)}]})
        write_json(job/'Journal.json',{'operations':{}})
        return job,frozen,source

    def fake_convert(self,job,*args,**kwargs):
        output=job/'staging/hull.fbx'
        output.parent.mkdir(exist_ok=True)
        output.write_bytes(b'fbx')
        return {'outputs':[{'path':str(output),'sha256':file_hash(output)}],'checks':{'staticMeshGeometry':'passed'}}

    def test_cached_stage_requires_output_hash_and_fresh_source(self):
        with tempfile.TemporaryDirectory() as directory:
            job,frozen,source=self.setup_job(Path(directory))
            with patch('converter.convert',side_effect=self.fake_convert) as convert:
                convert_job(job,frozen)
                convert_job(job,frozen)
                self.assertEqual(convert.call_count,1)
                (job/'staging/hull.fbx').write_bytes(b'corrupt')
                convert_job(job,frozen)
                self.assertEqual(convert.call_count,2)
                manifest=read_json(job/'ConversionManifest.json')
                self.assertEqual(manifest['outputs'][0]['path'],'staging/hull.fbx')
                source.write_bytes(b'changed')
                with self.assertRaises(PipelineError) as error:
                    convert_job(job,frozen)
                self.assertEqual(error.exception.code,'INPUT_CHANGED')
                self.assertEqual(convert.call_count,2)

    def test_capacity_is_checkout_wide(self):
        with tempfile.TemporaryDirectory() as directory:
            job,frozen,_=self.setup_job(Path(directory))
            write_json(job.parents[1]/'Coordinator/Capacity.json',{'blenderWorkers':2})
            with patch('converter.convert',side_effect=self.fake_convert) as convert:
                with self.assertRaises(PipelineError) as error:
                    convert_job(job,frozen)
                self.assertEqual(error.exception.code,'INVALID_CONFIG')
                convert.assert_not_called()

    def test_cache_rejects_cross_job_paths_and_empty_outputs(self):
        with tempfile.TemporaryDirectory() as directory:
            job,frozen,source=self.setup_job(Path(directory))
            with patch('converter.convert',side_effect=self.fake_convert) as convert:
                convert_job(job,frozen)
                for outputs in ([{'path':str(source),'sha256':file_hash(source)}], []):
                    cached=read_json(job/'ConversionComplete.json')
                    cached['report']['outputs']=outputs
                    write_json(job/'ConversionComplete.json',cached)
                    convert_job(job,frozen)
                self.assertEqual(convert.call_count,3)

    def test_live_orphan_process_blocks_same_job_and_other_slot_user(self):
        with tempfile.TemporaryDirectory() as directory:
            job,frozen,_=self.setup_job(Path(directory))
            record={'state':'running','nonce':'owned','owner':owner()}
            write_json(job/'BlenderProcess.json',record)
            with patch('converter.convert',side_effect=self.fake_convert) as convert:
                with self.assertRaises(PipelineError) as error:
                    convert_job(job,frozen)
                self.assertEqual(error.exception.code,'OPERATION_UNRESOLVED')
                convert.assert_not_called()
            (job/'BlenderProcess.json').unlink()
            record['state']='starting'
            record['owner']={'pid':99999999,'processStart':'stopped','nonce':'nonce'}
            write_json(job.parents[1]/'Coordinator/blender-0.json',record)
            with patch('converter.convert',side_effect=self.fake_convert) as convert:
                with self.assertRaises(PipelineError) as error:
                    convert_job(job,frozen)
                self.assertEqual(error.exception.code,'OPERATION_UNRESOLVED')
                convert.assert_not_called()


if __name__=='__main__':
    unittest.main()
