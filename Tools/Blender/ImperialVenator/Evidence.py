"""Persist source provenance and verify Republic Venator records are unchanged."""
import hashlib
import json
import subprocess
from pathlib import Path
import yaml

ROOT = Path('Temp/ImperialVenatorImport')
audit = json.loads((ROOT / 'SourceAudit.json').read_text())
for filename, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(filename).read_bytes()).hexdigest() == expected, filename


def ReadAsset(text):
    return yaml.safe_load(text[text.index('MonoBehaviour:'):])['MonoBehaviour']


def Record(asset, field, key):
    rows = asset
    for part in field.split('.'):
        rows = rows[part]
    return next(row['value'] for row in rows if row['key'] == key)


records = [
    ('Assets/Settings/Data/Ship/ShipsData.asset', 'shipsData.keyValue', 0),
    ('Assets/Settings/AssetMappingData.asset', 'assetMappings.keyValue', 'VenatorShipView'),
    ('Assets/Settings/AssetMappingData.asset', 'assetMappings.keyValue', 'VenatorShipData'),
    ('Assets/Settings/Data/Models/ShipUi/ShipUiData.asset', 'shipIconWrapper.keyValue', 0),
    ('Assets/Settings/Data/Reinforcement/ReinforcementData.asset', 'spawnShipWrapper.keyValue', 0),
    ('Assets/Settings/Data/Factions/Republic/RepublicFaction.asset', 'ships.keyValue', 0),
]
for filename, field, key in records:
    baseline = subprocess.check_output(['git', 'show', 'HEAD:' + filename], text=True)
    assert Record(ReadAsset(baseline), field, key) == Record(ReadAsset(Path(filename).read_text()), field, key), filename

donors = ['Assets/Prefabs/Models/Ships/VenatorShipView.prefab', 'Assets/Settings/Data/Ship/VenatorShipData.asset',
          'Assets/Prefabs/Ui/Reinforcement/VenatorReinforcementView.prefab', 'Assets/Settings/Data/Ship/Wreck/VenatorWreckData.asset',
          'Assets/Prefabs/Models/Wrecks/VenatorWreckView.prefab']
for filename in donors:
    baseline = subprocess.check_output(['git', 'show', 'HEAD:' + filename])
    assert baseline.replace(b'\r\n', b'\n') == Path(filename).read_bytes().replace(b'\r\n', b'\n'), filename
    assert subprocess.check_output(['git', 'show', 'HEAD:' + filename + '.meta']).replace(b'\r\n', b'\n') == Path(filename + '.meta').read_bytes().replace(b'\r\n', b'\n'), filename + '.meta'

conversions = []
for name, variant in audit['variants'].items():
    report = json.loads((ROOT / name / 'ConversionReport.json').read_text())
    assert report['geometry_error'] < .001 and report['bone_error'] < .001
    conversions.append(dict(name=name, source=variant['file'], meshes=len(report['before']['meshes']), bones=len(variant['bones']),
                            geometry_error=report['geometry_error'], bone_error=report['bone_error']))
evidence = dict(date='2026-10-09', unit='Venator_Empire', inheritance=audit['unit']['inheritance'], source_values=audit['unit']['resolved'],
                source_garrison=audit['garrison'], source_abilities=audit['abilities'],
                model_textures={name: {ref: data['source'] for ref, data in variant['textures'].items()} for name, variant in audit['variants'].items()},
                attachments=[dict(name=hp['name'], model=hp['resolved']['Model_To_Attach'], bone=hp['resolved']['Attachment_Bone'])
                             for hp in audit['hardpoints'] if hp['resolved'].get('Model_To_Attach')],
                death_clone=audit['death_clone'], source_hashes=audit['source_hashes'], source_files_unchanged=True,
                republic_records_unchanged=records, republic_assets_unchanged=donors, conversions=conversions,
                unity_geometry=json.loads((ROOT / 'UnityGeometry.json').read_text()),
                engines=json.loads((ROOT / 'EngineInspection.json').read_text()),
                mounts=json.loads((ROOT / 'GameplayMounts.json').read_text()),
                verification=json.loads((ROOT / 'Verification.json').read_text()))
Path('Tools/Blender/ImperialVenator/Evidence.json').write_text(json.dumps(evidence, indent=2) + '\n')
print(len(audit['source_hashes']), 'source hashes unchanged;', len(records), 'Republic records and', len(donors), 'donor assets/metas unchanged;', len(conversions), 'conversion reports saved.')
