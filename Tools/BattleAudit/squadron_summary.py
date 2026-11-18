"""Summarize a battle folder's editor_telemetry/Loading_*_squadrons.csv files.

Live population is distinct sampled entities per minute, not pooled start rows.
These window counts are not instantaneous peaks: a dead squadron and its
replacement can both appear in the same minute.
"""
import csv, sys, pathlib, collections, math
root = pathlib.Path(sys.argv[1]) / "editor_telemetry"
out = []
for f in sorted(root.glob("Loading_*_squadrons.csv")):
    rows = list(csv.DictReader(f.open(encoding="utf-8")))
    if not rows: continue
    sampled_minutes = collections.defaultdict(dict)
    per_player_launch = collections.Counter(); per_unit = collections.Counter(); deaths = collections.Counter()
    samples = collections.defaultdict(list); orders = collections.Counter(); lifetimes = []
    born = {}
    for r in rows:
        k = r["kind"]; key = (r["player"], r["entityId"]); t = float(r["time"])
        if k == "launch":
            born[key] = t
            per_player_launch[r["player"]] += 1; per_unit[r["unit"]] += 1
        elif k == "destroyed":
            deaths[r["player"]] += 1
            if key in born: lifetimes.append(t - born.pop(key))
        elif k == "sample":
            samples[r["time"]].append(r); orders[r["order"]] += 1
            sampled_minutes[int(t // 60)][key] = r["unit"]
    dur = float(rows[-1]["time"]) - float(rows[0]["time"])
    out.append(f"===== {f.name}: duration {dur:.0f}s rows {len(rows)}")
    if sampled_minutes:
        peak_minute, peak_entities = max(sampled_minutes.items(), key=lambda item: len(item[1]))
        peak_by_player = collections.Counter(p for p, _ in peak_entities)
        out.append(f" peak sampled squadrons/minute {len(peak_entities)} in minute {peak_minute} by player {dict(peak_by_player)} (not an instantaneous peak)")
        for minute, entities in sorted(sampled_minutes.items()):
            by_player = collections.Counter(p for p, _ in entities)
            out.append(f" minute {minute}: sampled squadrons {len(entities)} by player {dict(by_player)}")
    else:
        out.append(" live population unavailable: no sample rows (start rows include pooled inactive squadrons)")
    out.append(f" launches by player {dict(per_player_launch)} (total {sum(per_player_launch.values())}, {60*sum(per_player_launch.values())/max(dur,1):.1f}/min)")
    out.append(f" destroyed by owner {dict(deaths)}; median lifetime {sorted(lifetimes)[len(lifetimes)//2] if lifetimes else 0:.0f}s")
    out.append(f" launches by unit {dict(per_unit.most_common(12))}")
    out.append(f" sample orders {dict(orders)}")
    # stacking: pairs of same-player squadrons whose centres are within R in one sample tick
    for R in (10.0, 25.0):
        stacked = total = 0; worst = 0
        for t, rs in samples.items():
            pts = [(r["player"], float(r["x"]), float(r["z"])) for r in rs]
            n = 0
            for i in range(len(pts)):
                for j in range(i + 1, len(pts)):
                    if pts[i][0] == pts[j][0] and math.hypot(pts[i][1]-pts[j][1], pts[i][2]-pts[j][2]) < R: n += 1
            stacked += n; total += 1; worst = max(worst, n)
        out.append(f" same-owner squadron pairs closer than {R:.0f}: avg {stacked/max(total,1):.1f}/sample, worst {worst}")
    # idle/None squadrons far from anything
    none_far = sum(1 for rs in samples.values() for r in rs if r["order"] in ("None", "Idle") and float(r["targetDistance"]) < 0)
    out.append(f" samples with no order and no target: {none_far} of {sum(orders.values())}")
text = "\n".join(out)
(pathlib.Path(sys.argv[1]) / "squadron_summary.txt").write_text(text, encoding="utf-8")
print(text)
