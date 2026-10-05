from pathlib import Path
import hashlib, itertools, json, math

root = Path('Temp/XWingImport/Output')
source = json.loads((root/'SourceGeometry.json').read_text())
unity = {m['name']: m['triangles'] for m in json.loads((root/'UnityGeometry.json').read_text())}
reports = []
for mesh in source:
    original = [[[-p[0]*.02,p[2]*.02,-p[1]*.02,*p[3:]] for p in tri] for tri in mesh['triangles']]
    copied = unity[mesh['name']]
    def key(tri):
        return tuple(math.floor(sum(p[i] for p in tri)/3/.001) for i in range(3))
    buckets = {}
    for i, tri in enumerate(original):
        buckets.setdefault(key(tri), []).append(i)
    used = set()
    worst = 0
    uv_error = 0
    shadow = 'shadow' in mesh['name'].lower() or mesh['name']=='X_Wing_LOD0'
    for tri in copied:
        k = key(tri)
        candidates = [i for delta in itertools.product((-1,0,1), repeat=3) for i in buckets.get(tuple(k[j]+delta[j] for j in range(3)), [])]
        error, selected = min((max(min(math.dist(p[:3],q[:3]) if shadow or math.dist(p[3:],q[3:]) < .00001 else 1000 for q in original[i]) for p in tri), i) for i in candidates)
        assert error < .00001, (mesh['name'],error)
        used.add(selected)
        worst = max(worst,error)
        uv_error = max(uv_error, max(math.dist(p[3:], min(original[selected],key=lambda q:math.dist(p[:3],q[:3]))[3:]) for p in tri))
    assert len(original)==len(copied),mesh['name']
    reports.append(dict(mesh=mesh['name'],triangles=len(copied),max_position_error=worst,max_uv_error=uv_error,uv_verified=not shadow))
hashes=json.loads((root/'SourceHashes.json').read_text())
assert all(hashlib.sha256(Path(p).read_bytes()).hexdigest()==h for p,h in hashes.items())
report=dict(meshes=reports,source_hashes_unchanged=True,bones=json.loads((root/'UnityBoneError.json').read_text()))
(root/'GeometryVerification.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report,indent=2))
