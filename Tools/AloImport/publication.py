"""Resolve converted transport to the exact reviewed Unity output roles."""
from pathlib import Path

from storage import PipelineError, digest, file_hash, read_json, write_json


def create_manifest(job_directory, frozen, plan):
    job = Path(job_directory).resolve()
    staging = job / 'staging'
    assembly = read_json(staging / 'AssemblyManifest.json')
    recipe = frozen['recipe']
    outputs = recipe['integration']['outputs']
    by_role = {output['role']: output for output in outputs}
    by_guid = {output['guid']: output for output in outputs if output['guid'] is not None}
    files = {}

    def add(output, relative, remaps):
        path = (job / relative).resolve()
        if not path.is_relative_to(staging) or not path.is_file():
            raise PipelineError('OUTPUT_CONFLICT', 'Publication source must be an owned staging file: ' + str(path))
        row = {'outputRole': output['role'], 'source': path.relative_to(staging).as_posix(),
               'sha256': file_hash(path), 'baselineSha256': plan['baseline'][output['path']],
               'materialRemaps': remaps}
        existing = files.get(output['role'])
        if existing is not None and existing != row:
            raise PipelineError('SOURCE_AMBIGUOUS', 'Multiple converted files target output: ' + output['role'])
        files[output['role']] = row

    for piece in assembly['pieces']:
        role = 'model-' + piece['id']
        if role not in by_role:
            raise PipelineError('INVALID_RECIPE', 'Missing model output role: ' + role, 2)
        remaps = {}
        for mesh in piece['meshes']:
            for slot in mesh['slots']:
                guid = slot['policy']['targetGuid']
                name = slot['sourceName']
                if guid not in by_guid or Path(by_guid[guid]['path']).suffix.lower() != '.mat':
                    raise PipelineError('INVALID_RECIPE', 'Material target must be an explicit output GUID: ' + str(guid), 2)
                if name in remaps and remaps[name] != guid:
                    raise PipelineError('SOURCE_AMBIGUOUS', 'Transport material name selects different targets: ' + name)
                remaps[name] = guid
        add(by_role[role], piece['fbx'], [{'sourceName': name, 'materialGuid': guid} for name, guid in sorted(remaps.items())])
    textures = {row['source'].casefold(): row for row in assembly['textures']}
    for material in recipe['materials']:
        for texture in material['textures']:
            guid = texture.get('targetGuid')
            if guid not in by_guid:
                raise PipelineError('INVALID_RECIPE', 'Texture requires a reviewed output targetGuid: ' + texture['source'], 2)
            source = textures[texture['source'].casefold()]
            if file_hash(job / source['path']) != source['sha256']:
                raise PipelineError('INPUT_CHANGED', 'Decoded texture changed: ' + source['path'])
            add(by_guid[guid], source['path'], [])
    expected = {output['role'] for output in outputs if Path(output['path']).suffix.lower() in {'.fbx', '.png', '.tga'}
                and output['role'] != recipe.get('registrations', {}).get('iconRole')}
    if set(files) != expected:
        raise PipelineError('INVALID_RECIPE', 'Art output coverage differs: ' + str(sorted(set(files) ^ expected)), 2)
    manifest = {'schemaVersion': 1, 'jobId': frozen['jobId'], 'inputHash': digest(frozen),
                'planHash': plan['planHash'], 'files': [files[key] for key in sorted(files)],
                'derivatives': 'retained-existing-not-regenerated'}
    path = staging / 'PublicationManifest.json'
    write_json(path, manifest)
    conversion_path = job / 'ConversionManifest.json'
    conversion = read_json(conversion_path)
    conversion['outputs'] = [row for row in conversion['outputs'] if row['path'] != 'staging/PublicationManifest.json']
    conversion['outputs'].append({'path': 'staging/PublicationManifest.json', 'sha256': file_hash(path)})
    write_json(conversion_path, conversion)
    return str(path)


def capture_applied(jobs, job_id, plan):
    from registry_plan import entry_hash
    baseline = {relative: file_hash(jobs.project / relative) if (jobs.project / relative).is_file() else None
                for relative in plan['baseline']}
    entries = []
    for original in plan['registryEntries']:
        row = dict(original)
        row['sha256'] = entry_hash(jobs.project / row['path'], row['array'], row['keyField'], row['key'])
        entries.append(row)
    value = {'jobId': job_id, 'inputHash': plan['inputHash'], 'planHash': plan['planHash'],
             'baseline': baseline, 'registryEntries': entries}
    path = jobs.job_dir(job_id) / 'AppliedState.json'
    write_json(path, value)
    return {'path': str(path), 'sha256': file_hash(path)}


def check_saved_journal(jobs, job_id):
    path = jobs.job_dir(job_id) / 'PublicationJournal.json'
    if not path.is_file():
        raise PipelineError('RECOVERY_CONFLICT', 'Missing saved publication journal', 6)
    journal = read_json(path)
    expected = {row['path']: (row['asset'].get('sha256'), row['meta'].get('sha256')) for row in journal['preimages']}
    for checkpoint in journal['checkpoints']:
        for row in checkpoint['postimages']:
            expected[row['path']] = (row['assetSha256'], row['metaSha256'])
    for relative, hashes in expected.items():
        asset = jobs.project / relative
        meta = Path(str(asset) + '.meta')
        current = (file_hash(asset) if asset.is_file() else None, file_hash(meta) if meta.is_file() else None)
        if current != hashes:
            raise PipelineError('RECOVERY_CONFLICT', 'Publication changed after its saved checkpoint: ' + relative, 6)


def applied_state(jobs, job_id, plan):
    journal = read_json(jobs.job_dir(job_id) / 'Journal.json')
    completed = [operation for operation in journal['operations'].values()
                 if operation['stage'] == 'apply' and operation['state'] == 'succeeded']
    if not completed:
        return None
    operation = completed[0]
    result_path = Path(operation['evidencePath'])
    if not result_path.is_file() or file_hash(result_path) != operation['evidenceHash']:
        raise PipelineError('INPUT_CHANGED', 'Apply completion evidence changed', 5)
    descriptor = read_json(result_path)['appliedState']
    path = Path(descriptor['path']).resolve()
    if path != jobs.job_dir(job_id) / 'AppliedState.json' or not path.is_file() or file_hash(path) != descriptor['sha256']:
        raise PipelineError('INPUT_CHANGED', 'Applied-state manifest changed', 5)
    state = read_json(path)
    if any(state[key] != plan[key] for key in ('jobId', 'inputHash', 'planHash')):
        raise PipelineError('INPUT_CHANGED', 'Applied state belongs to a different plan', 5)
    return state
