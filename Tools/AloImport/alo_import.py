"""Single CLI entry point for the recipe-driven ALO pipeline."""
import argparse
import importlib.metadata
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import time
import uuid

from jobs import Jobs, TERMINAL
from storage import PipelineError, read_json


def envelope(stage, state='succeeded', code='OK', summary='', job_id=None, operation=None, evidence=None):
    return {'schemaVersion': 1, 'jobId': job_id,
            'operationId': operation['operationId'] if operation else None,
            'stage': stage, 'state': state, 'code': code, 'retryable': code in {'EDITOR_BUSY'},
            'summary': summary, 'counts': {}, 'evidencePaths': evidence or [],
            'nextAction': None}


def launch_worker(jobs):
    log = jobs.coordinator / 'worker.log'
    log.parent.mkdir(parents=True, exist_ok=True)
    options = {'creationflags': subprocess.CREATE_NO_WINDOW} if os.name == 'nt' else {'start_new_session': True}
    with log.open('ab') as stream:
        subprocess.Popen([sys.executable, str(Path(__file__).resolve()), '_worker',
                          '--project', str(jobs.project)], cwd=jobs.project,
                         stdin=subprocess.DEVNULL, stdout=stream, stderr=stream, **options)


def operation_response(jobs, operation, wait):
    if operation['state'] == 'queued':
        launch_worker(jobs)
    deadline = time.monotonic() + wait
    while operation['state'] not in TERMINAL | {'needs-recovery', 'interrupted'} and time.monotonic() < deadline:
        time.sleep(0.1)
        operation = jobs.status(operation['jobId'])['operations'][operation['operationId']]
    response = envelope(operation['stage'], operation['state'], operation['code'],
                        operation.get('summary', operation['state']), operation['jobId'], operation,
                        [str(jobs.job_dir(operation['jobId']) / 'Journal.json')])
    response['nextAction'] = 'status' if operation['state'] in {'queued', 'running'} else (
        'recover' if operation['state'] in {'needs-recovery', 'interrupted'} else None)
    return response, operation.get('exitCode', 0)


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('command', choices=['doctor', 'inspect', 'plan', 'convert', 'apply',
                                          'verify', 'render', 'status', 'resume', 'cancel', 'recover', '_worker'])
    parser.add_argument('--project', type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument('--recipe', type=Path)
    parser.add_argument('--config', type=Path)
    parser.add_argument('--policy', type=Path, help='Invocation policy JSON; asset writes are denied by default')
    parser.add_argument('--job')
    parser.add_argument('--plan-hash')
    parser.add_argument('--wait-seconds', type=float, default=0)
    parser.add_argument('--json', action='store_true', help='All commands always return one JSON envelope')
    args = parser.parse_args(argv)
    jobs = Jobs(args.project)
    try:
        if args.wait_seconds < 0:
            raise PipelineError('INVALID_ARGUMENT', '--wait-seconds must be nonnegative', 2)
        if args.command == '_worker':
            from worker import drain
            drain(jobs)
            return 0
        if args.command == 'doctor':
            tools = {name: shutil.which(name) for name in ('python', 'unity')}
            dependencies = {name: importlib.metadata.version(name) if importlib.util.find_spec(module) else None
                            for name, module in [('jsonschema', 'jsonschema'), ('Pillow', 'PIL')]}
            config = read_json(args.config) if args.config else {'sourceRoots': {}}
            sources = {key: Path(value).is_dir() for key, value in config['sourceRoots'].items()}
            result = envelope('doctor', summary='Read-only local tool and source checks; Unity handshake not run')
            result['checks'] = {'tools': tools, 'pythonDependencies': dependencies, 'sourceRoots': sources,
                                'blender': Path(config['blender']).is_file() if 'blender' in config else False,
                                'unityHandshake': 'not-run', 'physicalCheckout': str(jobs.project)}
            if not all(tools.values()) or not all(dependencies.values()) or not all(sources.values()) or (args.config and not result['checks']['blender']):
                result.update(state='failed', code='UNAVAILABLE_TOOL_OR_INPUT', summary='Required local tool or source root unavailable')
                print(json.dumps(result, allow_nan=False))
                return 4
        elif args.command == 'inspect':
            if args.recipe is None or args.config is None:
                raise PipelineError('INVALID_ARGUMENT', 'inspect requires --recipe and --config', 2)
            from recipe import load_recipe
            from source_audit import audit_recipe_sources
            recipe = load_recipe(args.recipe)
            config = read_json(args.config)
            if set(config) - {'sourceRoots', 'blender', 'blenderWorkers', 'alamoAddon'}:
                raise PipelineError('INVALID_CONFIG', 'Unknown invocation configuration field', 2)
            if not isinstance(config.get('sourceRoots'), dict) or any(not Path(value).is_absolute() for value in config['sourceRoots'].values()):
                raise PipelineError('INVALID_CONFIG', 'sourceRoots must map aliases to absolute local paths', 2)
            workspace = jobs.root / 'SourceSnapshots' / str(uuid.uuid4())
            audit = audit_recipe_sources(recipe, config['sourceRoots'], workspace=workspace)
            job_id = jobs.inspect(recipe, config, audit, read_json(args.policy) if args.policy else None)
            result = envelope('inspect', summary='Frozen recipe, source audit and tool fingerprint', job_id=job_id,
                              evidence=[str(jobs.job_dir(job_id) / 'SourceAudit.json')])
        else:
            if not args.job:
                raise PipelineError('INVALID_ARGUMENT', f'{args.command} requires --job', 2)
            if args.command == 'plan':
                plan = jobs.plan(args.job)
                result = envelope('plan', summary='Hashed read/write plan created', job_id=args.job,
                                  evidence=[str(jobs.job_dir(args.job) / 'BuildPlan.json')])
                result['planHash'] = plan['planHash']
            elif args.command == 'status':
                journal = jobs.status(args.job)
                result = envelope('status', summary='Current job operation records', job_id=args.job)
                result['operations'] = list(journal['operations'].values())
            elif args.command == 'cancel':
                journal = jobs.cancel(args.job)
                result = envelope('cancel', summary='Cancellation recorded; active stages stop at safe boundaries', job_id=args.job)
                result['operations'] = list(journal['operations'].values())
            elif args.command in {'recover', 'resume'}:
                from worker import recover
                journal = recover(jobs, args.job)
                if args.command == 'recover':
                    launch_worker(jobs)
                if args.command == 'resume':
                    jobs.inputs(args.job)
                    for operation in journal['operations'].values():
                        if operation['stage'] == 'convert' and operation['state'] == 'succeeded':
                            try:
                                jobs.require_conversion(args.job)
                            except PipelineError:
                                operation.update(state='interrupted', code='CACHE_INVALID')
                                jobs.update(operation, state='interrupted', code='CACHE_INVALID')
                    pending = [op for op in journal['operations'].values() if op['state'] in {'queued', 'interrupted', 'failed'}]
                    if any(op['stage'] in {'apply', 'verify', 'render'} and op['state'] != 'queued' for op in pending):
                        raise PipelineError('OPERATION_UNRESOLVED', 'Unity retry needs a verified replan; no blind replay', 6)
                    for operation in pending:
                        if operation['state'] != 'queued':
                            jobs.update(operation, state='queued', code='QUEUED')
                        with jobs.metadata_lock():
                            state = jobs.state()
                            if not any(item['operationId'] == operation['operationId'] for item in state['queue']):
                                state['queue'].append({'jobId': args.job, 'operationId': operation['operationId']})
                                jobs.save_state(state)
                    launch_worker(jobs)
                result = envelope(args.command, summary='Journal reconciled against writer and Editor identity', job_id=args.job)
                result['operations'] = list(journal['operations'].values())
            else:
                if args.command == 'apply' and not args.plan_hash:
                    raise PipelineError('INVALID_ARGUMENT', 'apply requires --plan-hash', 2)
                operation = jobs.submit(args.job, args.command, args.plan_hash)
                result, exit_code = operation_response(jobs, operation, args.wait_seconds)
                print(json.dumps(result, allow_nan=False))
                return exit_code
        print(json.dumps(result, allow_nan=False))
        return 0
    except Exception as error:
        from recipe import RecipeError
        from source_audit import SourceError
        if isinstance(error, SourceError):
            code = error.code
            exit_code = 3 if code in {'SOURCE_AMBIGUOUS', 'INPUT_CHANGED'} else 4
        elif isinstance(error, (RecipeError, ValueError, KeyError)):
            code, exit_code = 'INVALID_RECIPE', 2
        elif isinstance(error, PipelineError):
            code, exit_code = error.code, error.exit_code
        elif isinstance(error, (OSError, ImportError)):
            code, exit_code = 'UNAVAILABLE_INPUT', 4
        else:
            code, exit_code = 'PIPELINE_FAILED', 5
        print(json.dumps(envelope(args.command, 'failed', code, str(error), args.job)))
        return exit_code


if __name__ == '__main__':
    sys.exit(main())
