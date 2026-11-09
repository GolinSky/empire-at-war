"""Queue execution. Each invocation owns at most one job stage."""
import json
from pathlib import Path
import subprocess
import time

from jobs import TERMINAL, UNITY_STAGES
from storage import PipelineError, exclusive_lock, file_hash, owner, read_json, write_json


def checked_result(value):
    """CLI success must not hide a nested Editor failure."""
    if isinstance(value, dict):
        if value.get('type') == 'text' and isinstance(value.get('text'), str):
            try:
                nested = json.loads(value['text'])
            except json.JSONDecodeError:
                nested = None
            if nested is not None:
                checked_result(nested)
        if value.get('success') is False or value.get('isError') is True:
            raise PipelineError('UNITY_FAILED', str(value.get('error', value.get('message', value))), 5)
        if value.get('state') in {'failed', 'needs-recovery', 'interrupted'}:
            raise PipelineError(value.get('code', 'UNITY_FAILED'), value.get('summary', str(value)), 5)
        for item in value.values():
            checked_result(item)
    elif isinstance(value, list):
        for item in value:
            checked_result(item)
    return value


def unity_stage(jobs, operation):
    folder = jobs.job_dir(operation['jobId'])
    bridge = Path(__file__).with_name('UnityStage.cs')
    if not bridge.exists():
        raise PipelineError('UNSUPPORTED_CAPABILITY', 'Unity stage bridge is not installed', 4)
    capabilities = read_json(Path(__file__).with_name('capabilities.json'))
    if operation['stage'] not in capabilities['supportedUnityStages']:
        raise PipelineError('UNSUPPORTED_CAPABILITY',
                            operation['stage'] + ' has not passed its migration gate; no Unity mutation dispatched', 4)
    unresolved = jobs.coordinator / 'UnityUnresolved.json'
    if unresolved.exists():
        raise PipelineError('OPERATION_UNRESOLVED', 'A previous Unity operation needs reconciliation', 6)
    editor = jobs.coordinator / 'EditorOperation.json'
    if editor.exists() and read_json(editor).get('state') not in TERMINAL | {'idle'}:
        raise PipelineError('OPERATION_UNRESOLVED', 'Editor has an unresolved operation', 6)
    jobs.inputs(operation['jobId'])
    jobs.checked_plan(operation['jobId'], operation.get('planHash'), use_applied=operation['stage'] != 'apply')
    # Persistent marker exists before dispatch: coordinator death never admits a second writer.
    write_json(unresolved, operation)
    arguments = [operation['jobId'], operation['operationId'], operation['inputHash'],
                 operation['stage'], str(folder)]
    command = ['unity', 'command', 'run_script', '--file', str(bridge),
               '--entry', 'AloUnityStage.Main', '--args', json.dumps(arguments),
               '--timeout_ms', '120000', '--json']
    result = subprocess.run(command, cwd=jobs.project, capture_output=True, text=True,
                            encoding='utf-8', timeout=150)
    (folder / (operation['stage'] + '.stdout.log')).write_text(result.stdout, encoding='utf-8')
    (folder / (operation['stage'] + '.stderr.log')).write_text(result.stderr, encoding='utf-8')
    # Only an Editor completion record matching this exact operation proves quiescence.
    record = completion_record(jobs, operation)
    if record is None:
        raise PipelineError('OPERATION_UNRESOLVED', 'Unity completion identity is unresolved', 6)
    if result.returncode:
        raise PipelineError('UNITY_FAILED', f'Unity exited {result.returncode}; see stage logs', 5)
    checked_result(record)
    return checked_result(json.loads(result.stdout))


def completion_record(jobs, operation):
    paths = [jobs.coordinator / 'Operations' / (operation['operationId'] + '.json'),
             jobs.job_dir(operation['jobId']) / 'OperationResult.json',
             jobs.coordinator / 'EditorOperation.json']
    for path in paths:
        if path.exists():
            record = read_json(path)
            if (all(record.get(key) == operation[key] for key in ('jobId', 'operationId', 'inputHash'))
                    and record.get('state') in TERMINAL):
                return record
    return None


def reconcile_editor(jobs, operation):
    folder = jobs.job_dir(operation['jobId'])
    command = ['unity', 'command', 'run_script', '--file', str(Path(__file__).with_name('UnityStage.cs')),
               '--entry', 'AloUnityStage.Main', '--args', json.dumps([operation['jobId'], operation['operationId'],
                 operation['inputHash'], 'recover', str(folder)]), '--timeout_ms', '120000', '--json']
    result = subprocess.run(command, cwd=jobs.project, capture_output=True, text=True, encoding='utf-8', timeout=150)
    (folder / 'recovery.stdout.log').write_text(result.stdout, encoding='utf-8')
    (folder / 'recovery.stderr.log').write_text(result.stderr, encoding='utf-8')
    if result.returncode or completion_record(jobs, operation) is None:
        raise PipelineError('RECOVERY_CONFLICT', 'Editor reconciliation did not establish a terminal outcome; see recovery logs', 6)


def run_stage(jobs, operation):
    if operation['stage'] in UNITY_STAGES:
        return unity_stage(jobs, operation)
    if operation['stage'] == 'convert':
        from converter import convert_job
        from publication import create_manifest
        frozen = jobs.inputs(operation['jobId'])
        plan = jobs.checked_plan(operation['jobId'])
        result = convert_job(jobs.job_dir(operation['jobId']), frozen)
        result['publicationManifest'] = create_manifest(jobs.job_dir(operation['jobId']), frozen, plan)
        return result
    raise PipelineError('UNSUPPORTED_CAPABILITY', f"Stage is not implemented: {operation['stage']}", 4)


def execute(jobs, operation, action=run_stage):
    identity = owner()
    with jobs.metadata_lock():
        path = jobs.job_dir(operation['jobId']) / 'Journal.json'
        journal = read_json(path)
        operation = journal['operations'][operation['operationId']]
        if operation['state'] != 'queued':
            return operation
        operation.update(state='running', code='RUNNING', owner=identity)
        operation.pop('exitCode', None)
        operation.pop('result', None)
        write_json(path, journal)
    try:
        jobs.inputs(operation['jobId'])
        jobs.require_stage(operation['jobId'], operation['stage'])
        result = action(jobs, operation)
        checked_result(result)
        if operation['stage'] == 'apply':
            from publication import capture_applied
            jobs.inputs(operation['jobId'])
            plan = read_json(jobs.job_dir(operation['jobId']) / 'BuildPlan.json')
            result = {'unityResult': result, 'appliedState': capture_applied(jobs, operation['jobId'], plan)}
        evidence = jobs.job_dir(operation['jobId']) / 'Results' / (operation['operationId'] + '.json')
        write_json(evidence, result)
        operation = jobs.update(operation, state='succeeded', code='OK',
                                evidencePath=str(evidence), evidenceHash=file_hash(evidence),
                                checks=result.get('checks', {}) if isinstance(result, dict) else {})
        marker = jobs.coordinator / 'UnityUnresolved.json'
        if operation['stage'] in UNITY_STAGES and marker.exists() and read_json(marker)['operationId'] == operation['operationId']:
            marker.unlink()
    except Exception as error:
        marker = jobs.coordinator / 'UnityUnresolved.json'
        unresolved = (marker.exists() and operation['stage'] in UNITY_STAGES
                      and read_json(marker)['operationId'] == operation['operationId'])
        cancelled = getattr(error, 'code', None) == 'CANCELLED' and not unresolved
        operation = jobs.update(operation, state='needs-recovery' if unresolved else 'cancelled' if cancelled else 'failed',
                                code=error.code if isinstance(error, PipelineError) else 'STAGE_FAILED',
                                summary=str(error), exitCode=(0 if cancelled else 6 if unresolved else error.exit_code
                                             if isinstance(error, PipelineError) else 5))
    finally:
        if operation['state'] in TERMINAL and operation['stage'] in UNITY_STAGES:
            with jobs.metadata_lock():
                state = jobs.state()
                journal = read_json(jobs.job_dir(operation['jobId']) / 'Journal.json')
                active = any(item['stage'] in UNITY_STAGES and item['state'] not in TERMINAL
                             for item in journal['operations'].values())
                if not active:
                    state['claims'].pop(operation['jobId'], None)
                jobs.save_state(state)
    return operation


def drain(jobs):
    """FIFO for Editor work; conversion workers take independent job locks."""
    while True:
        selected = None
        waiting_for_tests = False
        with jobs.metadata_lock():
            state = jobs.state()
            marker = jobs.coordinator / 'UnityUnresolved.json'
            editor = jobs.coordinator / 'EditorOperation.json'
            editor_blocked = marker.exists() or (editor.exists() and read_json(editor).get('state') not in TERMINAL | {'idle'})
            test_status = jobs.project / 'Temp/pipeline_test_status.json'
            tests_busy = (jobs.project / 'Temp/pipeline_test_request.json').exists() or (
                test_status.exists() and read_json(test_status).get('status') == 'running')
            for item in state['queue']:
                journal = read_json(jobs.job_dir(item['jobId']) / 'Journal.json')
                operation = journal['operations'][item['operationId']]
                if operation['stage'] in UNITY_STAGES and (editor_blocked or tests_busy):
                    waiting_for_tests = waiting_for_tests or (tests_busy and operation['state'] == 'queued' and not editor_blocked)
                    continue
                if operation['state'] == 'queued':
                    selected = operation
                    break
            if selected is None:
                if not waiting_for_tests:
                    return
        if selected is None:
            time.sleep(1)
            continue
        gate_name = 'editor.lock' if selected['stage'] in UNITY_STAGES else selected['operationId'] + '.lock'
        try:
            with exclusive_lock(jobs.coordinator / gate_name, wait_seconds=1):
                # Recheck after acquiring gate; another worker may have completed it.
                with jobs.metadata_lock():
                    state = jobs.state()
                    journal = read_json(jobs.job_dir(selected['jobId']) / 'Journal.json')
                    selected = journal['operations'][selected['operationId']]
                    if selected['state'] != 'queued':
                        continue
                execute(jobs, selected)
                with jobs.metadata_lock():
                    state = jobs.state()
                    state['queue'] = [item for item in state['queue']
                                      if item['operationId'] != selected['operationId']]
                    jobs.save_state(state)
        except PipelineError as error:
            if error.code != 'EDITOR_BUSY':
                raise
            time.sleep(0.1)


def recover(jobs, job_id):
    """Diagnose only after writer quiescence. Never restore stale registries."""
    from storage import is_alive
    with exclusive_lock(jobs.coordinator / 'editor.lock', wait_seconds=0):
        journal = jobs.status(job_id)
        editor_path = jobs.coordinator / 'EditorOperation.json'
        editor = read_json(editor_path) if editor_path.exists() else None
        for operation in journal['operations'].values():
            marker = jobs.coordinator / 'UnityUnresolved.json'
            if operation['state'] == 'succeeded' and marker.exists() and read_json(marker)['operationId'] == operation['operationId']:
                if completion_record(jobs, operation) is None:
                    raise PipelineError('RECOVERY_CONFLICT', 'Completed journal has no matching Editor receipt', 6)
                marker.unlink()
                continue
            if operation['state'] not in {'needs-recovery', 'interrupted', 'running'}:
                continue
            if operation['owner'] and is_alive(operation['owner']):
                raise PipelineError('OPERATION_UNRESOLVED', 'Original writer is still alive', 6)
            if operation['stage'] in UNITY_STAGES:
                if editor and editor.get('state') not in TERMINAL | {'idle'}:
                    reconcile_editor(jobs, operation)
                    editor = read_json(editor_path)
                    if editor.get('state') not in TERMINAL | {'idle'}:
                        raise PipelineError('RECOVERY_CONFLICT', 'Editor operation remains unresolved', 6)
                record = completion_record(jobs, operation)
                if record is None:
                    raise PipelineError('RECOVERY_CONFLICT', 'No matching terminal Editor record; explicit Editor reconciliation required', 6)
                # Forward completion needs saved verification evidence, never a time-based takeover.
                if record['state'] == 'succeeded':
                    checked_result(record)
                result = record
                if operation['stage'] == 'apply' and record['state'] == 'succeeded':
                    from publication import capture_applied, check_saved_journal
                    jobs.inputs(job_id)
                    check_saved_journal(jobs, job_id)
                    plan = read_json(jobs.job_dir(job_id) / 'BuildPlan.json')
                    result = {'unityResult': record, 'appliedState': capture_applied(jobs, job_id, plan)}
                evidence = jobs.job_dir(job_id) / 'Results' / (operation['operationId'] + '.json')
                write_json(evidence, result)
                jobs.update(operation, state=record['state'], code='RECOVERED',
                            evidencePath=str(evidence), evidenceHash=file_hash(evidence))
                marker = jobs.coordinator / 'UnityUnresolved.json'
                if marker.exists() and read_json(marker)['operationId'] == operation['operationId']:
                    marker.unlink()
                with jobs.metadata_lock():
                    state = jobs.state()
                    state['claims'].pop(job_id, None)
                    jobs.save_state(state)
            else:
                jobs.update(operation, state='interrupted', code='RETRY_REQUIRED')
        return jobs.status(job_id)
