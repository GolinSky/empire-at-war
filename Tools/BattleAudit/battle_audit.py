"""Battle performance audit helpers.

Commands:
  csv <capture.csv> [<baseline.csv> ...]       Compare battle CSV captures and list spike frames of the first.
  profiler <profiler-summary.json>             Frame range, CPU/GPU medians and top main-thread self markers.
  decoder allocations <first> <last> <out.cs> <result.json>
                                               Write a Unity eval script: GC.Alloc bytes per frame and by first
                                               resolved managed method (needs a capture with GC.Alloc call stacks).
  decoder callers <marker> <first> <last> <out.cs>
                                               Write a Unity eval script: count and time of <marker> per frame,
                                               grouped by its two enclosing markers.

Run the eval scripts with the Unity MCP `eval_file` tool while the frames are still in the Profiler.
"""
import csv
import json
import statistics
import sys

CSV_COLUMNS = [
    'frame_duration_ms', 'main_thread_ms', 'gpu_frame_ms',
    'target_batch_capture_ms', 'projectile_turret_update_ms', 'projectile_turret_update_sample_count',
    'squadron_order_ms', 'ship_order_tick_ms', 'due_batch_ms', 'radar_scan_ms',
    'navigation_plan_ms', 'navigation_plan_sample_count', 'gc_allocated_bytes',
    'target_requests', 'target_candidates', 'pending_impacts',
]
SPIKES = [
    ('frame_duration_ms', 50.0),
    ('navigation_plan_ms', 10.0),
    ('squadron_order_ms', 6.0),
    ('ship_order_tick_ms', 6.0),
    ('gc_allocated_bytes', 1_000_000.0),
]


def read_csv(path):
    with open(path, encoding='utf-8-sig') as handle:
        return list(csv.DictReader(handle))


def value(row, column):
    text = row.get(column)
    return float(text) if text not in (None, '', 'unavailable') else None


def describe(values):
    ordered = sorted(values)
    p95 = ordered[max(0, int(0.95 * len(ordered)) - 1)]
    return f'{statistics.median(values):.2f}/{statistics.mean(values):.2f}/{p95:.2f}/{ordered[-1]:.1f}'


def compare_csv(paths):
    runs = [(path.replace('\\', '/').split('/')[-1], read_csv(path)) for path in paths]
    print('median/mean/p95/max per column')
    print('metric'.ljust(40) + ''.join(f'{name[:30]} ({len(rows)}f)'.rjust(36) for name, rows in runs))
    for column in CSV_COLUMNS:
        cells = []
        for _, rows in runs:
            values = [v for v in (value(row, column) for row in rows) if v is not None]
            cells.append((describe(values) if values else 'n/a').rjust(36))
        print(column.ljust(40) + ''.join(cells))
    rows = runs[0][1]
    print(f'\nspikes in {runs[0][0]} (last frame includes the capture writing its CSV):')
    for column, threshold in SPIKES:
        hits = [(row['frame'], round(value(row, column), 1)) for row in rows
                if value(row, column) is not None and value(row, column) > threshold]
        print(f'  {column} > {threshold:g}: {len(hits)} frames {hits[:25]}')


def summarize_profiler(path):
    with open(path, encoding='utf-8') as handle:
        frames = json.load(handle)['profiler_frame_summaries']
    indices = [frame['profiler_frame_index'] for frame in frames]
    print(f'frames {min(indices)}-{max(indices)} ({len(frames)})')
    for counter in ('CPU Main Thread Frame Time', 'GPU Frame Time'):
        values = [frame['rendering_counters'][counter] / 1e6 for frame in frames
                  if frame['rendering_counters'].get(counter)]
        if values:
            print(f'{counter}: median {statistics.median(values):.2f} ms, p95 {sorted(values)[int(0.95 * len(values)) - 1]:.2f} ms')
    totals = {}
    for frame in frames:
        main = next(thread for thread in frame['threads'] if thread['thread_name'] == 'Main Thread')
        for hotspot in main['cpu_hotspots']:
            entry = totals.setdefault(hotspot['marker'], [0.0, 0, 0])
            entry[0] += hotspot['self_ms']
            entry[1] += hotspot['calls']
            entry[2] += 1
    print('self ms/frame | frames in top-40 | calls/frame | marker')
    for marker, (self_ms, calls, seen) in sorted(totals.items(), key=lambda item: -item[1][0])[:20]:
        print(f'{self_ms / len(frames):8.3f} | {seen:3d} | {calls / len(frames):8.1f} | {marker}')


ALLOCATIONS_TEMPLATE = '''// GC.Alloc on the main thread, frames {first}-{last}: bytes per frame and by first resolved managed method.
var perFrame = new System.Collections.Generic.List<string>();
var byMethod = new System.Collections.Generic.Dictionary<string, long[]>();
var methods = new System.Collections.Generic.Dictionary<ulong, string>();
var stack = new System.Collections.Generic.List<ulong>();
long totalBytes = 0, totalCalls = 0;
for (int frame = {first}; frame <= {last}; frame++)
{{
    using (var view = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frame, 0))
    {{
        if (!view.valid) continue;
        long frameBytes = 0;
        for (int i = 0; i < view.sampleCount; i++)
        {{
            if (view.GetSampleName(i) != "GC.Alloc") continue;
            long size = view.GetSampleMetadataAsLong(i, 0);
            frameBytes += size;
            totalCalls++;
            stack.Clear();
            view.GetSampleCallstack(i, stack);
            string key = "<no resolved stack>";
            foreach (ulong address in stack)
            {{
                if (!methods.TryGetValue(address, out string name))
                {{
                    var info = view.ResolveMethodInfo(address);
                    name = string.IsNullOrEmpty(info.methodName) ? null : info.methodName + ":" + info.sourceFileLine;
                    methods[address] = name;
                }}
                if (name != null) {{ key = name; break; }}
            }}
            if (!byMethod.TryGetValue(key, out long[] total)) byMethod[key] = total = new long[2];
            total[0] += size;
            total[1]++;
        }}
        totalBytes += frameBytes;
        perFrame.Add(frame + ":" + frameBytes / 1024 + "KB");
    }}
}}
var top = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(System.Linq.Enumerable.Select(
    System.Linq.Enumerable.OrderByDescending(byMethod, p => p.Value[0]),
    p => new {{ method = p.Key, bytes = p.Value[0], calls = p.Value[1] }}), 30));
var result = new {{ first = {first}, last = {last}, totalBytes, totalCalls, perFrame, top }};
System.IO.File.WriteAllText("{result}", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
return new {{ totalBytes, totalCalls, top = System.Linq.Enumerable.Take(top, 15) }};
'''

CALLERS_TEMPLATE = '''// "{marker}" samples on the main thread, frames {first}-{last}: per-frame count/time and enclosing markers.
var callers = new System.Collections.Generic.Dictionary<string, double[]>();
var frames = new System.Collections.Generic.List<string>();
for (int frame = {first}; frame <= {last}; frame++)
{{
    using (var view = UnityEditorInternal.ProfilerDriver.GetRawFrameDataView(frame, 0))
    {{
        if (!view.valid) continue;
        var parents = new System.Collections.Generic.List<(int End, string Name)>();
        int count = 0;
        double ms = 0;
        for (int i = 0; i < view.sampleCount; i++)
        {{
            while (parents.Count > 0 && parents[parents.Count - 1].End < i) parents.RemoveAt(parents.Count - 1);
            string name = view.GetSampleName(i);
            if (name == "{marker}")
            {{
                count++;
                double sampleMs = view.GetSampleTimeMs(i);
                ms += sampleMs;
                string key = (parents.Count > 0 ? parents[parents.Count - 1].Name : "<root>") + " <- " +
                    (parents.Count > 1 ? parents[parents.Count - 2].Name : "<root>");
                if (!callers.TryGetValue(key, out double[] total)) callers[key] = total = new double[2];
                total[0]++;
                total[1] += sampleMs;
            }}
            int descendants = view.GetSampleChildrenCountRecursive(i);
            if (descendants > 0) parents.Add((i + descendants, name));
        }}
        if (count > 0) frames.Add(frame + ":" + count + "/" + ms.ToString("F1") + "ms");
    }}
}}
return new
{{
    callers = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(
        System.Linq.Enumerable.OrderByDescending(callers, p => p.Value[1]),
        p => p.Value[0] + " calls, " + p.Value[1].ToString("F1") + " ms : " + p.Key)),
    frames
}};
'''


def write_decoder(arguments):
    kind = arguments[0]
    if kind == 'allocations':
        first, last, output, result = arguments[1:5]
        text = ALLOCATIONS_TEMPLATE.format(first=int(first), last=int(last), result=result.replace('\\', '/'))
    elif kind == 'callers':
        marker, first, last, output = arguments[1:5]
        text = CALLERS_TEMPLATE.format(marker=marker, first=int(first), last=int(last))
    else:
        raise SystemExit(f'unknown decoder {kind}')
    with open(output, 'w', encoding='utf-8') as handle:
        handle.write(text)
    print(f'wrote {output}')


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    command, arguments = sys.argv[1], sys.argv[2:]
    if command == 'csv':
        compare_csv(arguments)
    elif command == 'profiler':
        summarize_profiler(arguments[0])
    elif command == 'decoder':
        write_decoder(arguments)
    else:
        raise SystemExit(__doc__)


if __name__ == '__main__':
    main()
