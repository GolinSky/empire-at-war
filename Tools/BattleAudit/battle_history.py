"""Export the automatic FPS windows and older detailed captures as one chronological CSV."""
import csv
import math
import re
from datetime import datetime
from pathlib import Path


def nearest_rank(ordered, percentile):
    return ordered[math.ceil(len(ordered) * percentile) - 1]


def detailed_row(path):
    summary_path = path.with_name(path.stem + '_summary.txt')
    metadata = {}
    if summary_path.exists():
        for line in summary_path.read_text(encoding='utf-8-sig').splitlines():
            key, _, text = line.partition('=')
            metadata[key] = text
    with path.open(encoding='utf-8-sig', newline='') as handle:
        durations = [float(row['frame_duration_ms']) for row in csv.DictReader(handle)]
    if not durations or sum(durations) <= 0:
        raise ValueError(f'{path}: no measurable frame durations')
    ordered = sorted(durations)
    seconds = sum(durations) / 1000
    timestamp = datetime.strptime(path.stem, 'battle_%Y%m%d_%H%M%S_%f').isoformat() + 'Z'
    return dict(started_utc=metadata.get('started_utc', timestamp), run_id=path.stem, kind='detail_capture',
                end_running_s='', window_s=f'{seconds:.6f}', frames=len(ordered),
                avg_fps=f'{len(ordered) / seconds:.3f}', p95_ms=f'{nearest_rank(ordered, .95):.3f}',
                p99_ms=f'{nearest_rank(ordered, .99):.3f}', max_ms=f'{ordered[-1]:.3f}',
                over_50ms=sum(ms > 50 for ms in ordered),
                worst_at_running_s='', quality=metadata.get('quality'), resolution=metadata.get('resolution'),
                vsync=metadata.get('vsync_count'), target_fps=metadata.get('target_frame_rate'),
                time_scale=metadata.get('time_scale'), runtime=metadata.get('runtime'), unity=metadata.get('unity'),
                gpu=metadata.get('graphics_device'), processor=metadata.get('processor'),
                source_revision=metadata.get('source_revision'), source_state=metadata.get('source_state'),
                build_guid=metadata.get('build_guid'), file=str(path))


def write_history(directories, output):
    rows = {}
    for directory in map(Path, directories):
        if not directory.is_dir():
            raise ValueError(f'not a capture directory: {directory}')
        for path in sorted(directory.rglob('*.csv')):
            if path.name in ('fps_history_editor.csv', 'fps_history_player.csv'):
                with path.open(encoding='utf-8-sig', newline='') as handle:
                    for row in csv.DictReader(handle):
                        rows.setdefault((row['run_id'], row['end_running_s']), dict(row, file=str(path)))
            elif re.fullmatch(r'battle_\d{8}_\d{6}_\d{3}', path.stem) and (path.stem, '') not in rows:
                rows[(path.stem, '')] = detailed_row(path)
    if not rows:
        raise ValueError('no battle captures or automatic FPS history found')
    ordered = sorted(rows.values(), key=lambda r: (r['started_utc'], r['run_id'], float(r['end_running_s'] or 0)))
    fields = dict.fromkeys(key for row in ordered for key in row)
    writer = csv.DictWriter(output, fieldnames=fields)
    writer.writeheader()
    writer.writerows(ordered)
