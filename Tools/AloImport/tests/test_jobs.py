"""Failure and ownership checks use temporary checkouts, never the live Editor."""
import copy
import multiprocessing
import random
from pathlib import Path
import sys
import tempfile
import time
import unittest

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from jobs import Jobs, overlaps
from storage import PipelineError, digest, exclusive_lock, file_hash, owner, read_json, write_json
from worker import checked_result, execute
from test_recipe import valid_recipe


def compete(project, job_id, plan_hash, barrier, results):
    jobs = Jobs(project)
    barrier.wait()
    time.sleep(random.random() * 0.02)
    try:
        operation = jobs.submit(job_id, 'apply', plan_hash)
        results.put(operation['state'])
    except PipelineError as error:
        results.put(error.code)


def hold_lock(path, ready):
    with exclusive_lock(path):
        ready.put(owner())
        time.sleep(30)


class JobsTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.project = Path(self.temporary.name)
        self.jobs = Jobs(self.project)
        self.source = self.project / 'Hull.ALO'
        self.source.write_bytes(b'original-source')
        for name, guid in [('Template.prefab', 'a' * 32), ('Data.asset', 'c' * 32),
                           ('Art/Models/Fixture.fbx', 'b' * 32)]:
            path = self.project / 'Assets' / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text('current asset ' + name)
            Path(str(path) + '.meta').write_text('fileFormatVersion: 2\nguid: ' + guid + '\n')

    def job(self, recipe=None):
        return self.jobs.inspect(recipe or valid_recipe(), {'sourceRoots': {}},
                                 {'files': [{'path': str(self.source), 'sha256': file_hash(self.source)}]})

    def test_frozen_recipe_audit_source_and_pipeline_are_checked(self):
        job_id = self.job()
        self.jobs.inputs(job_id)
        path = self.jobs.job_dir(job_id) / 'Input.json'
        frozen = read_json(path)
        frozen['recipe']['dimensions']['targetLength'] = 999
        write_json(path, frozen)
        with self.assertRaisesRegex(PipelineError, 'edited'):
            self.jobs.inputs(job_id)
        job_id = self.job()
        self.source.write_bytes(b'changed-source')
        with self.assertRaisesRegex(PipelineError, 'Source changed'):
            self.jobs.inputs(job_id)

    def test_duplicate_submission_returns_same_operation(self):
        job_id = self.job()
        first = self.jobs.submit(job_id, 'convert')
        second = self.jobs.submit(job_id, 'convert')
        self.assertEqual(first, second)
        self.assertEqual(1, len(self.jobs.state()['queue']))

    def test_two_process_reservation_race_rejects_one_before_writes(self):
        ids = [self.job(), self.job()]
        hashes = [self.jobs.plan(job_id)['planHash'] for job_id in ids]
        context = multiprocessing.get_context('spawn')
        barrier, results = context.Barrier(2), context.Queue()
        processes = [context.Process(target=compete, args=(str(self.project), job_id, plan_hash, barrier, results))
                     for job_id, plan_hash in zip(ids, hashes)]
        for process in processes:
            process.start()
        for process in processes:
            process.join(20)
            self.assertEqual(0, process.exitcode)
        self.assertEqual(['OUTPUT_CONFLICT', 'queued'], sorted([results.get(timeout=2), results.get(timeout=2)]))
        self.assertEqual(1, len(self.jobs.state()['claims']))

    def test_three_independent_claims_ten_randomized_runs(self):
        context = multiprocessing.get_context('spawn')
        for iteration in range(10):
            ids, hashes = [], []
            for number in range(3):
                recipe = valid_recipe()
                recipe['operation'] = 'create'
                recipe['modelId'] = f'Model{iteration}_{number}'
                recipe['identity']['unit'].update(name=recipe['modelId'], value=10000 + iteration * 3 + number)
                recipe['integration']['outputs'][0].update(path=f'Assets/Art/Models/{recipe["modelId"]}.fbx', guid=None)
                job_id = self.job(recipe)
                ids.append(job_id)
                hashes.append(self.jobs.plan(job_id)['planHash'])
            barrier, results = context.Barrier(3), context.Queue()
            processes = [context.Process(target=compete, args=(str(self.project), job_id, plan_hash, barrier, results))
                         for job_id, plan_hash in zip(ids, hashes)]
            for process in processes:
                process.start()
            for process in processes:
                process.join(20)
                self.assertEqual(0, process.exitcode)
            self.assertEqual(['queued'] * 3, [results.get(timeout=2) for _ in range(3)])
            self.assertEqual(set(ids), set(self.jobs.state()['claims']))
            for job_id in ids:
                self.jobs.cancel(job_id)
            self.assertEqual({}, self.jobs.state()['claims'])

    def test_parent_paths_case_and_meta_are_owned(self):
        self.assertTrue(overlaps(self.project / 'Assets', self.project / 'Assets/a.fbx.meta'))
        self.assertFalse(overlaps(self.project / 'Assets/a', self.project / 'Assets/ab'))
        recipe = valid_recipe()
        recipe['modelId'] = 'Other'
        recipe['identity']['unit']['name'] = 'Other'
        recipe['identity']['unit']['value'] += 1
        first, second = self.job(), self.job(recipe)
        self.jobs.submit(first, 'apply', self.jobs.plan(first)['planHash'])
        with self.assertRaisesRegex(PipelineError, 'Output is reserved'):
            self.jobs.submit(second, 'apply', self.jobs.plan(second)['planHash'])

    def test_stale_template_output_and_hash_fail_before_publication(self):
        job_id = self.job()
        plan = self.jobs.plan(job_id)
        with self.assertRaisesRegex(PipelineError, 'hash mismatch'):
            self.jobs.submit(job_id, 'apply', '0' * 64)
        (self.project / 'Assets/Template.prefab').write_text('changed template')
        with self.assertRaisesRegex(PipelineError, 'Planned asset changed'):
            self.jobs.checked_plan(job_id, plan['planHash'])
        with self.assertRaisesRegex(PipelineError, 'Planned asset changed'):
            self.jobs.plan(job_id)
        self.assertEqual({}, self.jobs.state()['claims'])

    def test_failure_after_dispatch_retains_claim_and_needs_recovery(self):
        job_id = self.job()
        conversion = self.jobs.submit(job_id, 'convert')
        execute(self.jobs, conversion, lambda jobs, current: {'success': True})
        artifact = self.jobs.job_dir(job_id) / 'staging/hull.fbx'
        artifact.parent.mkdir()
        artifact.write_bytes(b'converted fixture')
        write_json(self.jobs.job_dir(job_id) / 'ConversionManifest.json',
                   {'state': 'succeeded', 'jobId': job_id, 'inputHash': digest(self.jobs.inputs(job_id)),
                    'outputs': [{'path': 'staging/hull.fbx', 'sha256': file_hash(artifact)}]})
        operation = self.jobs.submit(job_id, 'apply', self.jobs.plan(job_id)['planHash'])
        def crash(jobs, current):
            write_json(jobs.coordinator / 'UnityUnresolved.json', current)
            raise PipelineError('OPERATION_UNRESOLVED', 'injected caller death', 6)
        result = execute(self.jobs, operation, crash)
        self.assertEqual('needs-recovery', result['state'])
        self.assertIn(job_id, self.jobs.state()['claims'])
        self.assertEqual(result['operationId'], self.jobs.submit(job_id, 'apply')['operationId'])
        with self.assertRaisesRegex(PipelineError, 'hash mismatch'):
            self.jobs.submit(job_id, 'apply', 'wrong')

    def test_apply_cannot_overtake_conversion_or_accept_missing_outputs(self):
        job_id = self.job()
        self.jobs.plan(job_id)
        with self.assertRaisesRegex(PipelineError, 'completed convert'):
            self.jobs.require_stage(job_id, 'apply')
        operation = self.jobs.submit(job_id, 'convert')
        execute(self.jobs, operation, lambda jobs, current: {'success': True})
        with self.assertRaisesRegex(PipelineError, 'complete conversion manifest'):
            self.jobs.require_stage(job_id, 'apply')

    def test_queued_cancellation_releases_claim(self):
        job_id = self.job()
        self.jobs.submit(job_id, 'apply', self.jobs.plan(job_id)['planHash'])
        journal = self.jobs.cancel(job_id)
        self.assertTrue(all(op['state'] == 'cancelled' for op in journal['operations'].values()))
        self.assertEqual({}, self.jobs.state()['claims'])

    def test_nested_editor_failure_is_not_cli_success(self):
        for value in [{'success': True, 'result': {'success': False, 'error': 'inner'}},
                      {'result': {'state': 'failed', 'summary': 'bad saved asset'}}]:
            with self.assertRaises(PipelineError):
                checked_result(value)

    def test_os_lock_contention_never_uses_expired_metadata(self):
        path = self.project / 'test.lock'
        with exclusive_lock(path):
            with self.assertRaises(PipelineError):
                with exclusive_lock(path, wait_seconds=0):
                    self.fail('Second writer acquired lock')

    def test_writer_death_releases_os_lock_but_retains_unresolved_marker(self):
        job_id = self.job()
        self.jobs.plan(job_id)
        operation = self.jobs.submit(job_id, 'verify')
        context = multiprocessing.get_context('spawn')
        ready = context.Queue()
        path = self.jobs.coordinator / 'editor.lock'
        process = context.Process(target=hold_lock, args=(str(path), ready))
        process.start()
        try:
            identity = ready.get(timeout=10)
            self.jobs.update(operation, state='running', owner=identity)
            write_json(self.jobs.coordinator / 'UnityUnresolved.json', operation)
        finally:
            process.terminate()
            process.join(10)
            ready.close()
            ready.join_thread()
        self.assertFalse(process.is_alive())
        with exclusive_lock(path, wait_seconds=0):
            status = self.jobs.status(job_id)['operations'][operation['operationId']]
            self.assertEqual('needs-recovery', status['state'])
            self.assertTrue((self.jobs.coordinator / 'UnityUnresolved.json').exists())


if __name__ == '__main__':
    unittest.main()
