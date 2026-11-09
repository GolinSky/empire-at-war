"""Immutable inputs, plans, output reservations and durable stage operations."""
import os
from pathlib import Path
import time
import uuid

from storage import (PipelineError, canonical, digest, exclusive_lock, file_hash,
                     is_alive, owner, read_json, write_json)

TERMINAL = {'succeeded', 'failed', 'cancelled'}
UNITY_STAGES = {'apply', 'verify', 'render'}


def overlaps(left, right):
    left, right = canonical(left), canonical(right)
    return left == right or left.startswith(right + os.sep) or right.startswith(left + os.sep)


class Jobs:
    def __init__(self, project):
        self.project = Path(project).resolve()
        self.layout = read_json(Path(__file__).with_name('layout.json'))
        self.root = self.project / self.layout['stateRoot']
        self.coordinator = self.root / 'Coordinator'

    def job_dir(self, job_id):
        try:
            parsed = uuid.UUID(job_id)
        except (ValueError, AttributeError):
            raise PipelineError('INVALID_JOB', 'Job ID must be a UUID', 2)
        if str(parsed) != job_id:
            raise PipelineError('INVALID_JOB', 'Job ID must use canonical UUID form', 2)
        path = self.root / 'Jobs' / job_id
        if not path.is_dir():
            raise PipelineError('JOB_NOT_FOUND', f'Unknown job: {job_id}', 4)
        return path

    def metadata_lock(self):
        return exclusive_lock(self.coordinator / 'metadata.lock')

    def state(self):
        path = self.coordinator / 'State.json'
        return read_json(path) if path.exists() else {'claims': {}, 'queue': []}

    def save_state(self, state):
        write_json(self.coordinator / 'State.json', state)

    def fingerprint(self):
        folder = Path(__file__).resolve().parent
        files = [p for p in folder.iterdir() if p.suffix in {'.py', '.cs', '.json', '.txt'} and p.name != 'local.json']
        files.extend((self.project / 'Assets/Scripts/Editor/AloImport').rglob('*.cs'))
        return digest({str(p.resolve()): file_hash(p) for p in sorted(files)})

    def inspect(self, recipe, config, audit, policy=None):
        job_id = str(uuid.uuid4())
        folder = self.root / 'Jobs' / job_id
        folder.mkdir(parents=True)
        if policy is None:
            policy = {'schemaVersion': 1, 'allowAssetMutation': False}
        if set(policy) - {'schemaVersion', 'allowAssetMutation', 'reason'} or policy.get('schemaVersion') != 1 or type(policy.get('allowAssetMutation')) is not bool:
            raise PipelineError('INVALID_POLICY', 'Expected a version 1 invocation policy with allowAssetMutation boolean', 2)
        frozen = {'schemaVersion': 1, 'jobId': job_id, 'recipe': recipe, 'policy': policy,
                  'config': config, 'auditHash': digest(audit),
                  'pipelineHash': self.fingerprint(), 'project': canonical(self.project)}
        write_json(folder / 'SourceAudit.json', audit)
        write_json(folder / 'Input.json', frozen)
        write_json(folder / 'InputHash.json', {'sha256': digest(frozen), 'fileSha256': file_hash(folder / 'Input.json')})
        write_json(folder / 'Journal.json', {'jobId': job_id, 'events': [], 'operations': {}})
        return job_id

    def inputs(self, job_id):
        folder = self.job_dir(job_id)
        frozen = read_json(folder / 'Input.json')
        hashes = read_json(folder / 'InputHash.json')
        if digest(frozen) != hashes['sha256'] or file_hash(folder / 'Input.json') != hashes['fileSha256']:
            raise PipelineError('INPUT_CHANGED', 'Frozen job input was edited; inspect a new job')
        if frozen['pipelineHash'] != self.fingerprint():
            raise PipelineError('INPUT_CHANGED', 'Pipeline changed; inspect a new job')
        if frozen['project'] != canonical(self.project):
            raise PipelineError('INPUT_CHANGED', 'Job belongs to a different physical checkout')
        audit = read_json(folder / 'SourceAudit.json')
        if digest(audit) != frozen['auditHash']:
            raise PipelineError('INPUT_CHANGED', 'Frozen source audit changed')
        for source in audit['files']:
            path = Path(source['path'])
            if not path.is_file() or file_hash(path) != source['sha256']:
                raise PipelineError('INPUT_CHANGED', f'Source changed: {path}')
        return frozen

    def plan(self, job_id):
        with exclusive_lock(self.coordinator / 'editor.lock'):
            marker = self.coordinator / 'UnityUnresolved.json'
            if marker.exists():
                raise PipelineError('OPERATION_UNRESOLVED', 'Reconcile the prior Editor operation before reading asset baselines', 6)
            return self._plan_locked(job_id)

    def _plan_locked(self, job_id):
        if (self.job_dir(job_id) / 'BuildPlan.json').exists():
            return self.checked_plan(job_id)
        frozen = self.inputs(job_id)
        recipe = frozen['recipe']
        staging = self.project / self.layout['candidateRoot'] / job_id
        if staging.exists():
            raise PipelineError('OUTPUT_CONFLICT', f'Candidate staging already exists: {staging}')
        writes = [canonical(staging), canonical(str(staging) + '.meta')]
        baseline = {}
        for output in recipe['integration']['outputs']:
            path = (self.project / output['path']).resolve()
            if not path.is_relative_to(self.project / 'Assets'):
                raise PipelineError('OUTPUT_CONFLICT', f'Output escapes Assets: {path}')
            if 'AddressableAssetsData' in path.parts:
                raise PipelineError('OUTPUT_CONFLICT', 'Addressables layout cannot be published by Python')
            writes.extend([canonical(path), canonical(str(path) + '.meta')])
            baseline[output['path']] = file_hash(path) if path.is_file() else None
            meta = Path(str(path) + '.meta')
            baseline[output['path'] + '.meta'] = file_hash(meta) if meta.is_file() else None
            if path.is_dir() or (output['guid'] is None and (path.exists() or meta.exists())):
                raise PipelineError('OUTPUT_CONFLICT', f'New output already exists: {path}')
            if output['guid'] is not None:
                if not meta.exists() or ('guid: ' + output['guid']) not in meta.read_text():
                    raise PipelineError('INPUT_CHANGED', f'Output GUID baseline does not match: {path}')
        dependencies = {}
        output_guids = {output['guid'] for output in recipe['integration']['outputs']}
        guids = set()
        def references(value, key=''):
            if isinstance(value, dict):
                for child_key, child in value.items():
                    references(child, child_key)
            elif isinstance(value, list):
                for child in value:
                    references(child, key)
            elif isinstance(value, str) and key.lower().endswith(('guid', 'guids')) and value not in output_guids:
                guids.add(value)
        references(recipe)
        # Resolve GUIDs from current .meta data, never names or stale historical paths.
        found = {}
        metas = {canonical(meta): meta for root in ('Assets', 'Packages', 'Library/PackageCache')
                 for meta in (self.project / root).rglob('*.meta')}
        for meta in metas.values():
            for line in meta.read_text(encoding='utf-8-sig').splitlines()[:5]:
                if line.startswith('guid: ') and line[6:] in guids:
                    guid = line[6:]
                    if guid in found:
                        raise PipelineError('INPUT_CHANGED', f'Duplicate GUID: {guid}')
                    found[guid] = meta
        merge_guids = set(recipe.get('registrations', {}).get(field) for field in
                          ('factionGuid', 'shipsDataGuid', 'assetMappingGuid', 'shipUiGuid', 'tooltipGuid',
                           'reinforcementGuid', 'viewGroupGuid', 'dataGroupGuid', 'addressableSettingsGuid')) - {None}
        resolved = {}
        for guid in sorted(guids):
            if guid not in found:
                raise PipelineError('INPUT_CHANGED', f'Dependency GUID not found: {guid}')
            meta = found[guid]
            asset = Path(str(meta)[:-5])
            if not asset.is_file():
                raise PipelineError('INVALID_RECIPE', f'Dependency is not an asset file: {asset}', 2)
            resolved[guid] = asset
            if guid not in merge_guids:
                dependencies[str(asset.relative_to(self.project))] = file_hash(asset)
            dependencies[str(meta.relative_to(self.project))] = file_hash(meta)
        from registry_plan import registration_entries
        registry_entries, merge_writes, registry_keys = registration_entries(self.project, recipe, resolved)
        settings = list((self.project / 'ProjectSettings').glob('*'))
        settings.extend([self.project / 'Packages/manifest.json', self.project / 'Packages/packages-lock.json'])
        for path in settings:
            if path.is_file():
                dependencies[str(path.relative_to(self.project))] = file_hash(path)
        unit = recipe['identity']['unit']
        keys = [f"{unit['enum']}:name:{unit['name']}", f"{unit['enum']}:value:{unit['value']}"]
        plan = {'schemaVersion': 1, 'jobId': job_id, 'inputHash': digest(frozen),
                'modelId': recipe['modelId'], 'writes': sorted(writes),
                'registryKeys': sorted(keys + registry_keys), 'baseline': baseline, 'dependencies': dependencies,
                'registryEntries': registry_entries, 'mergeWrites': merge_writes,
                'candidateStaging': str(staging.relative_to(self.project)).replace('\\', '/')}
        plan['planHash'] = digest(plan)
        write_json(self.job_dir(job_id) / 'BuildPlan.json', plan)
        return plan

    def read_plan(self, job_id, plan_hash=None):
        plan = read_json(self.job_dir(job_id) / 'BuildPlan.json')
        expected = plan.pop('planHash')
        if digest(plan) != expected or (plan_hash is not None and plan_hash != expected):
            raise PipelineError('INPUT_CHANGED', 'Build plan hash mismatch')
        plan['planHash'] = expected
        if plan['jobId'] != job_id:
            raise PipelineError('INPUT_CHANGED', 'Build plan belongs to a different job')
        return plan

    def checked_plan(self, job_id, plan_hash=None, use_applied=False):
        frozen = self.inputs(job_id)
        plan = self.read_plan(job_id, plan_hash)
        if plan['inputHash'] != digest(frozen):
            raise PipelineError('INPUT_CHANGED', 'Build plan belongs to different frozen inputs')
        from publication import applied_state
        applied = applied_state(self, job_id, plan) if use_applied else None
        state = applied if applied is not None else plan
        for relative, expected_hash in {**state['baseline'], **plan['dependencies']}.items():
            path = self.project / relative
            actual = file_hash(path) if path.is_file() else None
            if actual != expected_hash:
                raise PipelineError('INPUT_CHANGED', f'Planned asset changed: {relative}')
        from registry_plan import check_entries
        check_entries(self.project, state['registryEntries'])
        return plan

    def reserve(self, state, job_id, plan, operation_id):
        for other_id, claim in state['claims'].items():
            if other_id == job_id:
                continue
            collision = set(plan['registryKeys']) & set(claim['registryKeys'])
            if claim['modelId'].casefold() == plan['modelId'].casefold() or collision:
                raise PipelineError('OUTPUT_CONFLICT', f'Identity is reserved by job {other_id}')
            if any(overlaps(left, right) for left in plan['writes'] for right in claim['writes']):
                raise PipelineError('OUTPUT_CONFLICT', f'Output is reserved by job {other_id}')
        state['claims'][job_id] = {key: plan[key] for key in ('modelId', 'writes', 'registryKeys')}
        state['claims'][job_id].update(jobId=job_id, operationId=operation_id, submitter=owner())

    def submit(self, job_id, stage, plan_hash=None):
        operation = self._submit(job_id, stage, plan_hash)
        if stage in UNITY_STAGES and operation['state'] == 'succeeded':
            with exclusive_lock(self.coordinator / 'editor.lock'):
                self.checked_plan(job_id, plan_hash, use_applied=True)
        return operation

    def _submit(self, job_id, stage, plan_hash=None):
        frozen = self.inputs(job_id)
        folder = self.job_dir(job_id)
        operation_id = str(uuid.uuid5(uuid.UUID(job_id), stage + ':' + digest(frozen)))
        if stage == 'apply' and plan_hash is not None:
            plan = read_json(folder / 'BuildPlan.json')
            if plan_hash != plan['planHash']:
                raise PipelineError('INPUT_CHANGED', 'Build plan hash mismatch')
        with self.metadata_lock():
            journal = read_json(folder / 'Journal.json')
            existing = journal['operations'].get(operation_id)
            if existing:
                if existing['state'] == 'succeeded':
                    evidence = Path(existing['evidencePath'])
                    if not evidence.is_file() or file_hash(evidence) != existing['evidenceHash']:
                        raise PipelineError('INPUT_CHANGED', 'Stage completion evidence is missing or changed', 5)
                    if stage == 'convert':
                        self.require_conversion(job_id)
                return existing
            state = self.state()
            if stage in UNITY_STAGES:
                plan = self.read_plan(job_id, plan_hash)
                self.reserve(state, job_id, plan, operation_id)
            operation = {'operationId': operation_id, 'jobId': job_id, 'stage': stage,
                         'state': 'queued', 'submitted': time.time(), 'owner': None,
                         'inputHash': digest(frozen), 'code': 'QUEUED'}
            if stage in UNITY_STAGES:
                operation['planHash'] = plan['planHash']
            journal['operations'][operation_id] = operation
            journal['events'].append({'state': 'queued', 'operationId': operation_id, 'time': time.time()})
            write_json(folder / 'Journal.json', journal)
            state['queue'].append({'jobId': job_id, 'operationId': operation_id})
            self.save_state(state)
            return operation

    def require_stage(self, job_id, stage):
        journal = read_json(self.job_dir(job_id) / 'Journal.json')
        prerequisite = {'apply': 'convert', 'render': 'verify'}.get(stage)
        if prerequisite and not any(op['stage'] == prerequisite and op['state'] == 'succeeded'
                                    for op in journal['operations'].values()):
            raise PipelineError('STAGE_REQUIRED', f'{stage} requires completed {prerequisite}', 5)
        if stage in UNITY_STAGES:
            path = self.job_dir(job_id) / 'BuildPlan.json'
            if not path.exists():
                raise PipelineError('STAGE_REQUIRED', f'{stage} requires a build plan', 5)
        if stage == 'apply' or (stage == 'verify' and (self.job_dir(job_id) / 'ConversionManifest.json').exists()):
            self.require_conversion(job_id)

    def require_conversion(self, job_id):
        manifest = self.job_dir(job_id) / 'ConversionManifest.json'
        if not manifest.exists():
            raise PipelineError('STAGE_REQUIRED', 'apply requires a complete conversion manifest', 5)
        evidence = read_json(manifest)
        if evidence.get('state') != 'succeeded' or not evidence.get('outputs'):
            raise PipelineError('VERIFICATION_FAILED', 'Conversion manifest is incomplete', 5)
        if evidence.get('jobId') != job_id or evidence.get('inputHash') != digest(self.inputs(job_id)):
            raise PipelineError('INPUT_CHANGED', 'Conversion manifest belongs to different frozen inputs', 5)
        for output in evidence['outputs']:
            path = (self.job_dir(job_id) / output['path']).resolve()
            if not path.is_relative_to(self.job_dir(job_id) / 'staging') or not path.is_file() or file_hash(path) != output['sha256']:
                raise PipelineError('INPUT_CHANGED', 'Conversion output changed: ' + str(path), 5)

    def update(self, operation, **fields):
        with self.metadata_lock():
            path = self.job_dir(operation['jobId']) / 'Journal.json'
            journal = read_json(path)
            current = journal['operations'][operation['operationId']]
            current.update(fields)
            journal['events'].append({'operationId': operation['operationId'],
                                      'state': current['state'], 'time': time.time()})
            write_json(path, journal)
            return current

    def status(self, job_id):
        with self.metadata_lock():
            journal = read_json(self.job_dir(job_id) / 'Journal.json')
            for operation in journal['operations'].values():
                if operation['state'] == 'running' and not is_alive(operation['owner']):
                    operation['state'] = 'needs-recovery' if operation['stage'] in UNITY_STAGES else 'interrupted'
                    operation['code'] = 'OPERATION_UNRESOLVED'
            write_json(self.job_dir(job_id) / 'Journal.json', journal)
            return journal

    def cancel(self, job_id):
        with self.metadata_lock():
            path = self.job_dir(job_id) / 'Journal.json'
            journal = read_json(path)
            active = False
            for operation in journal['operations'].values():
                if operation['state'] == 'queued':
                    operation.update(state='cancelled', code='CANCELLED')
                elif operation['state'] not in TERMINAL:
                    operation['cancelRequested'] = True
                    active = True
            write_json(path, journal)
            state = self.state()
            state['queue'] = [item for item in state['queue'] if item['jobId'] != job_id]
            if not active:
                state['claims'].pop(job_id, None)
            self.save_state(state)
            return journal
