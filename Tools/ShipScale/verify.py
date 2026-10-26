"""Verify Unity-captured dimensions and every hierarchy transform after migration."""
import csv
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
LOG = ROOT / 'Logs/ShipScale'
before = json.loads((LOG / 'before.json').read_text())
after = {r['id']: r for r in json.loads((LOG / 'after.json').read_text())}
targets = {r['id']: r for r in json.loads((ROOT / 'Tools/ShipScale/targets.json').read_text())}
factors = {r['id']: r['factor'] for r in json.loads((LOG / 'multipliers.json').read_text())}
reference = next(r['length'] for r in before if r['id'] == 'Venator')
checks = 0
summary = []

def close(actual, expected, label):
    global checks
    assert math.isfinite(actual) and math.isclose(actual, expected, rel_tol=0.0001, abs_tol=0.0001), (label, actual, expected)
    checks += 1

for old in before:
    name = old['id']
    new = after[name]
    factor = factors[name]
    target = old['length'] if targets[name]['preserve'] else reference * targets[name]['meters'] / 1137
    close(new['length'], target, name + ' hull length')
    for field in ('HullBottom', 'HullTop', 'NavigationRadius'):
        close(new[field], old[field] * factor, name + ' ' + field)
    for kind in ('ship', 'wreck'):
        old_nodes = {n['path']: n for n in old[kind]}
        new_nodes = {n['path']: n for n in new[kind]}
        assert old_nodes.keys() == new_nodes.keys(), (name, kind, 'hierarchy changed')
        for path, node in old_nodes.items():
            current = new_nodes[path]
            assert node['components'] == current['components'], (name, path, 'components changed')
            particle_local = node.get('particleMode') == 'Local'
            for i, value in enumerate(node['matrix']):
                # All positions/bases scale once; Local particle bases compensate again.
                multiplier = factor if i % 4 != 3 else 1
                if particle_local and i < 12 and i % 4 != 3:
                    multiplier *= factor
                expected = value * multiplier
                if 12 <= i <= 14:
                    pivot = old_nodes['.']['matrix'][i]
                    expected = pivot + (value - pivot) * factor
                close(current['matrix'][i], expected, f'{name}/{path} matrix[{i}]')
            if 'destroyedVfxScale' in node:
                close(current['destroyedVfxScale'], node['destroyedVfxScale'] * factor, name + ' destruction VFX')
            if 'particleMode' in node:
                assert current['particleMode'] == node['particleMode']
                for actual, expected in zip(current['particleEffectiveScale'], node['particleEffectiveScale']):
                    close(actual, expected * factor, name + ' particle effective scale')
    if targets[name]['preserve']:
        for field in ('path', 'wreckPath', 'dataPath'):
            path = old[field]
            assert (ROOT / path).read_bytes() == (LOG / 'before-files' / path).read_bytes(), name + ' preserved file changed'
    summary.append([name, old['length'], new['length'], targets[name]['meters'] if not targets[name]['preserve'] else 'preserved', factor])

with (ROOT / 'Tools/ShipScale/results.csv').open('w', newline='') as output:
    writer = csv.writer(output)
    writer.writerow(['ship', 'before_units', 'after_units', 'target_meters', 'multiplier'])
    writer.writerows(summary)
result = {'roster_entries': len(before), 'numeric_checks': checks, 'preserved': ['HomeOne', 'MC80Independence'], 'status': 'passed'}
(LOG / 'verification.json').write_text(json.dumps(result, indent=2))
print(json.dumps(result))
