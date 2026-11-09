"""Hash owned registry entries, allowing unrelated queued imports to merge."""
from pathlib import Path

import yaml

from storage import PipelineError, digest


def asset_data(path):
    # BaseLoader only constructs maps/lists/strings, including Unity-tagged YAML.
    with Path(path).open(encoding='utf-8-sig') as stream:
        documents = list(yaml.load_all(stream, Loader=yaml.BaseLoader))
    if len(documents) != 1 or 'MonoBehaviour' not in documents[0]:
        raise PipelineError('UNSUPPORTED_CAPABILITY', f'Expected one serialized registry: {path}', 4)
    return documents[0]['MonoBehaviour']


def entry_hash(path, array_path, key_field, key):
    value = asset_data(path)
    for field in array_path.split('.'):
        value = value[field]
    if not isinstance(value, list):
        raise PipelineError('INPUT_CHANGED', f'Registry array is invalid: {path}/{array_path}')
    rows = [row for row in value if row[key_field] == str(key)]
    if len(rows) > 1:
        raise PipelineError('INPUT_CHANGED', f'Duplicate registry key: {path}/{array_path}/{key}')
    return digest(rows[0] if rows else None)


def registration_entries(project, recipe, resolved):
    contract = recipe.get('registrations')
    if contract is None:
        return [], [], []
    outputs = {output['role']: output for output in recipe['integration']['outputs']}
    unit_id = recipe['identity']['unit']['value']
    icon = outputs[contract['iconRole']]
    if icon['guid'] is None:
        raise PipelineError('UNSUPPORTED_CAPABILITY', 'New icon GUID registration needs a create adapter', 4)
    icon_key = icon['guid'] + ':' + str(contract['iconFileId'])
    records, merge_paths, keys = [], set(), []
    merge_paths.add(str(resolved[contract['addressableSettingsGuid']].relative_to(project)).replace('\\', '/'))

    def add(guid, array_path, key_field, key, path=None):
        path = Path(path) if path else resolved[guid]
        relative = str(path.relative_to(project)).replace('\\', '/')
        descriptor = {'guid': guid, 'path': relative, 'array': array_path, 'keyField': key_field, 'key': str(key)}
        if any(all(record[field] == descriptor[field] for field in descriptor) for record in records):
            return
        descriptor['sha256'] = entry_hash(path, array_path, key_field, key)
        records.append(descriptor)
        keys.append(guid + ':' + array_path + ':' + str(key))
        merge_paths.add(relative)

    for field, array in [('factionGuid', 'ships.keyValue'), ('shipsDataGuid', 'shipsData.keyValue'),
                         ('shipUiGuid', 'shipIconWrapper.keyValue'), ('reinforcementGuid', 'spawnShipWrapper.keyValue')]:
        add(contract[field], array, 'key', unit_id)
    add(contract['tooltipGuid'], 'icons', 'key', icon_key)
    for field in ('viewRole', 'dataRole', 'placementRole'):
        output = outputs[contract[field]]
        if field != 'placementRole':
            add(contract['assetMappingGuid'], 'assetMappings.keyValue', 'key', Path(output['path']).stem)
        expected_group = contract['dataGroupGuid' if field == 'dataRole' else 'viewGroupGuid']
        add(expected_group, 'm_SerializeEntries', 'm_GUID', output['guid'])
        # A current entry may live in another group; moving it also owns that key.
        for group_path in (project / 'Assets/AddressableAssetsData/AssetGroups').rglob('*.asset'):
            group = asset_data(group_path)
            if any(row['m_GUID'] == output['guid'] for row in group.get('m_SerializeEntries', [])):
                meta = group_path.with_name(group_path.name + '.meta').read_text(encoding='utf-8-sig')
                guid = next(line[6:] for line in meta.splitlines() if line.startswith('guid: '))
                add(guid, 'm_SerializeEntries', 'm_GUID', output['guid'], group_path)
    return records, sorted(merge_paths), sorted(keys)


def check_entries(project, records):
    for record in records:
        if entry_hash(project / record['path'], record['array'], record['keyField'], record['key']) != record['sha256']:
            raise PipelineError('INPUT_CHANGED', 'Owned registry entry changed: ' + record['path'] + '/' + record['key'])
