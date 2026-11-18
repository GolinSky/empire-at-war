"""Offline attribution reports. Inputs remain evidence, never automatic leak diagnoses."""
import contextlib
import io
import json
import statistics
import csv
import shutil
from datetime import datetime, timezone
from pathlib import Path


BUILTIN_PHASES = (
    'gc_collect_ms', 'initialization_ms', 'early_update_ms', 'fixed_update_ms',
    'pre_update_ms', 'update_ms', 'pre_late_update_ms', 'post_late_update_ms',
    'physics_fixed_update_ms', 'script_update_ms', 'script_late_update_ms',
    'loading_read_ms', 'loading_awake_ms', 'present_wait_ms', 'vsync_wait_ms',
    'canvas_rebuild_ms',
)


def write_json(path, data):
    Path(path).write_text(json.dumps(data, indent=2) + '\n', encoding='utf-8')


def load_json(path):
    return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def game_caller(methods):
    """Keep unresolved/framework attribution visible instead of inventing a caller."""
    return next((method for method in methods
                 if 'EmpireAtWar.' in method or 'Assembly-CSharp' in method),
                '<no resolved game caller>')


def merge_allocations(output, paths):
    totals, captures, total_frames = {}, [], 0
    for path in dict.fromkeys(str(Path(p).resolve()) for p in paths):
        data = load_json(path)
        frames = data.get('frames', data.get('perFrame', []))
        frame_count = data.get('frameCount', len(frames))
        if frame_count <= 0:
            raise ValueError(f'{path}: no valid frames; cannot compute bytes/frame')
        total_frames += frame_count
        captures.append({'path': path, 'frames': frame_count})
        # Stacks are exhaustive; a truncated top-method list cannot establish totals.
        stacks = data.get('stacks')
        if stacks is None:
            raise ValueError(f'{path}: missing exhaustive stacks (regenerate with decoder allocations)')
        for stack in stacks:
            method = game_caller(stack['methods'])
            entry = totals.setdefault(method, {'method': method, 'bytes': 0, 'calls': 0})
            entry['bytes'] += stack['bytes']
            entry['calls'] += stack['calls']
    ranking = sorted(totals.values(), key=lambda item: -item['bytes'])
    for entry in ranking:
        entry['frames'] = total_frames
        entry['bytes_per_frame'] = entry['bytes'] / total_frames
        entry['calls_per_frame'] = entry['calls'] / total_frames
    result = {'sampled_frames': total_frames, 'captures': captures, 'ranking': ranking,
              'scope': 'Sampled main-thread GC.Alloc stacks; unsampled intervals excluded.'}
    write_json(output, result)
    return result


def memory_diff(before, after, output):
    def index(path):
        result = {}
        for row in load_json(path)['types']:
            key = row['type'], row['owner']
            count, size = result.get(key, (0, 0))
            result[key] = count + row['count'], size + row['bytes']
        return result
    left, right = index(before), index(after)
    rows = []
    for key in left.keys() | right.keys():
        old_count, old_bytes = left.get(key, (0, 0))
        new_count, new_bytes = right.get(key, (0, 0))
        rows.append({'type': key[0], 'owner': key[1], 'before_bytes': old_bytes,
                     'after_bytes': new_bytes, 'delta_bytes': new_bytes - old_bytes,
                     'delta_count': new_count - old_count})
    result = {'before': str(before), 'after': str(after),
              'types': sorted(rows, key=lambda item: (-item['delta_bytes'], item['type'], item['owner'])),
              'interpretation': 'Growth alone does not distinguish leaks, caches or pools; inspect retained owners.'}
    write_json(output, result)
    return result


def hitch_period(rows, number):
    # Collapse consecutive slow frames into episodes, retaining per-frame counts.
    slow = sorted((row for row in rows if (number(row, 'frame_duration_ms') or 0) > 200),
                  key=lambda row: int(row['frame']))
    episodes = [row for i, row in enumerate(slow)
                if i == 0 or int(row['frame']) > int(slow[i - 1]['frame']) + 1]
    gaps = [number(b, 'seconds') - number(a, 'seconds')
            for a, b in zip(episodes, episodes[1:])
            if number(a, 'seconds') is not None and number(b, 'seconds') is not None]
    return {'frames_over_200ms': len(slow), 'episodes': len(episodes), 'gaps_seconds': gaps,
            'median_gap_seconds': statistics.median(gaps) if gaps else None}


def compare_runs(editor, player, output):
    from battle_audit import read_csv, value

    def collect(folder):
        # Copied CSVs are deduplicated by run/frame; selected hitch windows are not FPS samples.
        rows = {}
        sources = sorted(Path(folder).rglob('hitch_*.csv'))
        for path in sources:
            for row in read_csv(path):
                rows[(row.get('run_id', path.stem), row['frame'])] = row
        if not sources:
            raise ValueError(f'{folder}: no hitch CSVs')
        triggers = [row for row in rows.values() if row.get('is_hitch') == '1']
        durations = [value(row, 'frame_duration_ms') for row in triggers]
        return {'files': [str(path) for path in sources], 'triggers': len(triggers),
                'median_hitch_ms': statistics.median(durations) if durations else None,
                'max_hitch_ms': max(durations) if durations else None,
                'period': hitch_period(list(rows.values()), value)}
    result = {'editor': collect(editor), 'player': collect(player),
              'classification': 'unverified',
              'limitation': 'Match save, workload, resolution and running duration before tagging a cause editor-only. '
                            'Absence in a short Player sample is insufficient evidence.'}
    write_json(output, result)
    return result


def write_report(folder):
    from battle_audit import summarize_hitches, summarize_profiler
    root = Path(folder)
    lines = ['# Battle attribution report', '',
             '- Generated from saved evidence. Tests and replay acceptance are separate.',
             '- H1 (blocking GC), H2 (Editor work), H3 (ship order): pending correlated evidence.',
             '- Inclusive marker scopes overlap; do not sum parent and child times.', '']
    csvs = sorted(root.rglob('hitch_*.csv'))
    summaries = sorted({*root.rglob('*summary.json'), *root.rglob('hitch_*.json')})
    for path, summarize in [(p, summarize_hitches) for p in csvs] + [(p, summarize_profiler) for p in summaries
                              if 'profiler_frame_summaries' in load_json(p)]:
        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            summarize(str(path))
        lines.extend([f'## {path.relative_to(root).as_posix()}', '', '```text', output.getvalue().rstrip(), '```', ''])
    ranking = root / 'allocation-ranking.json'
    if ranking.exists():
        data = load_json(ranking)
        lines.extend(['## Sampled allocating methods', '', '| Method | Bytes/frame | Calls/frame |', '|---|---:|---:|'])
        for row in data['ranking'][:10]:
            method = row['method'].replace('|', '\\|')
            lines.append(f"| {method} | {row['bytes_per_frame']:.1f} | {row['calls_per_frame']:.2f} |")
        lines.append('')
    memory = root / 'memory-diff.json'
    if memory.exists():
        data = load_json(memory)
        lines.extend(['## Memory growth by type and owner', '', data['interpretation'], '',
                      '| Type | Owner | Growth bytes | Count delta |', '|---|---|---:|---:|'])
        for row in data['types'][:20]:
            lines.append(f"| {row['type']} | {row['owner']} | {row['delta_bytes']} | {row['delta_count']} |")
        lines.append('')
    lines.extend(['## Evidence inventory', ''])
    for path in sorted(root.rglob('*')):
        if path.is_file() and path.name != 'analysis.md':
            lines.append(f'- `{path.relative_to(root).as_posix()}`')
    if not csvs:
        lines.extend(['', '- No hitch CSV present. Import the matching run from BattleCaptures/Hitches.'])
    (root / 'analysis.md').write_text('\n'.join(lines) + '\n', encoding='utf-8')


def archive_run(source, folder, first, last, started):
    """Read shared CSV snapshots; incomplete writer tails are excluded."""
    source, destination = Path(source), Path(folder)
    first, last = int(first), int(last)
    since = datetime.fromisoformat(started.replace('Z', '+00:00'))
    inventory, run_ids = [], set()

    def save_csv(path, rows, fields):
        if not rows:
            return
        target = destination / path.name
        with target.open('w', newline='', encoding='utf-8') as handle:
            writer = csv.DictWriter(handle, fieldnames=fields)
            writer.writeheader()
            writer.writerows(rows)
        inventory.append(str(path))

    for path in sorted((source / 'Hitches').glob('hitch_*.csv')):
        if datetime.fromtimestamp(path.stat().st_mtime, timezone.utc) < since:
            continue
        # A copy taken during a battle can miss the current unfinished context batch.
        text = path.read_text(encoding='utf-8-sig')
        text = text[:text.rfind('\n') + 1]
        reader = csv.DictReader(io.StringIO(text))
        rows = [row for row in reader if row.get('frame') and first <= int(row['frame']) <= last]
        if rows:
            run_ids.add(path.stem.removeprefix('hitch_'))
        save_csv(path, rows, reader.fieldnames)
    for path in sorted(source.glob('fps_history_*.csv')):
        with path.open(encoding='utf-8-sig') as handle:
            reader = csv.DictReader(handle)
            rows = [row for row in reader if row.get('worst_observed_utc') and
                    datetime.fromisoformat(row['worst_observed_utc'].replace('Z', '+00:00')) >= since and
                    (row.get('run_id') in run_ids or
                     first <= int(row.get('worst_completed_unity_frame') or -1) <= last)]
            save_csv(path, rows, reader.fieldnames)
    for path in sorted(source.glob('battle_*')):
        if path.suffix not in ('.csv', '.txt'):
            continue
        stamp = path.stem.removesuffix('_summary')
        recorded = datetime.strptime(stamp, 'battle_%Y%m%d_%H%M%S_%f').replace(tzinfo=timezone.utc)
        if recorded >= since:
            shutil.copyfile(path, destination / path.name)
            inventory.append(str(path))
    write_json(destination / 'archive.json', {'first_unity_frame': first, 'last_unity_frame': last,
        'files': inventory, 'source': str(source),
        'limitations': 'Live rolling CSV snapshot can omit unfinished writer batches. FPS windows can straddle run boundaries; '
                       'only finished history windows are available. Re-import after battle end for complete context.'})
