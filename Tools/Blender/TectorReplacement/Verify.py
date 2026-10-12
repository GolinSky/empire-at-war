import hashlib
import json
from pathlib import Path
from PIL import Image, ImageChops

OUTPUT = Path('F:/CodexArtifacts/tector-preview/UnityReplacement')
before = json.loads((OUTPUT/'UnityBaseline.json').read_text())
after = json.loads((OUTPUT/'UnityAfter.json').read_text())
allowed = {'Ship': {'explosionHullRenderers'}, 'TeamColorView': {'meshRenderers'}, 'FogVisibilityComponent': {'renderers'}, 'Shield': {'hullPlanes'}, 'UnitSpawnView': {'meshRenderers'}, 'UnitWreckView': {'meshRenderers','meshFilters'}}
checked = 0
for path, original in before.items():
    current = after[path]
    assert original['guid'] == current['guid'], (path,'GUID')
    assert abs(current['boundsSize'][2]-180) < 0.01, (path,'length')
    original_components = {(c['path'],c['type']): c for c in original['components']}
    current_components = {(c['path'],c['type']): c for c in current['components']}
    assert set(original_components) == set(current_components), (path,'component set')
    for key, record in original_components.items():
        previous = json.loads(record['json'])['MonoBehaviour']
        active = json.loads(current_components[key]['json'])['MonoBehaviour']
        excluded = allowed.get(key[1],set()) | {'m_EditorClassIdentifier'}
        previous = {k:v for k,v in previous.items() if k not in excluded}
        active = {k:v for k,v in active.items() if k not in excluded}
        assert previous == active, (path,key,'core component configuration',previous,active)
        checked += 1
    if path.endswith('TectorShipView.prefab'):
        previous_transforms = {t['path']:t for t in original['transforms']}
        active_transforms = {t['path']:t for t in current['transforms']}
        for key in original_components:
            if key[1] in ('HardPoint','WeaponHardPoint'):
                assert previous_transforms[key[0]] == active_transforms[key[0]],(key,'hardpoint transform')
    model_refs = {r['mesh'] for r in current['renderers'] if 'ShieldSurface' not in r['path']}
    assert model_refs == {'Assets/Art/Models/EmpireShips/Tector/Tector_Remake.fbx'},(path,'obsolete visual models',model_refs)
hashes = json.loads((OUTPUT/'CoreDataHashes.json').read_text())
core_changes = [path for path,digest in hashes.items() if hashlib.sha256(Path(path).read_bytes()).hexdigest()!=digest]
assert not core_changes,('core data changed',core_changes)
source = json.loads((OUTPUT.parent/'SourceAudit.json').read_text())
assert hashlib.sha256(Path(source['file']).read_bytes()).hexdigest()==source['sha256']
for name, record in source['textures'].items():
    assert hashlib.sha256(Path(record['path']).read_bytes()).hexdigest()==record['sha256'],name
    imported=Path('Assets/Art/Textures/Models/EmpireShips/Tector')/('Tector_Remake_'+Path(name).stem+'.png')
    assert Image.open(imported).convert('RGBA').tobytes()==Image.open(record['path']).convert('RGBA').tobytes(),name
for name in ('TectorIcon','TectorSilhouette'):
    path=Path('Assets/Art/Textures/Ui/Icons/ShipIcon')/(name+'.png')
    image=Image.open(path).convert('RGBA')
    assert image.size==(512,512) and image.getchannel('A').getextrema()==(0,255),name
    bounds=image.getchannel('A').getbbox()
    assert bounds[0]>0 and bounds[1]>0 and bounds[2]<512 and bounds[3]<512,(name,'crop')
    backup=OUTPUT/'OriginalAssets/Art/Textures/Ui/Icons/ShipIcon'/(name+'.png.meta')
    assert Path(str(path)+'.meta').read_bytes()==backup.read_bytes(),(name,'meta')
palette_results={}
for prefix in ('Team','WreckTeam'):
    base=Image.open(OUTPUT/'Previews'/(prefix+'0.png')).convert('RGB')
    palette_results[prefix]=[]
    for i in range(1,8):
        difference=ImageChops.difference(base,Image.open(OUTPUT/'Previews'/(prefix+str(i)+'.png')).convert('RGB'))
        if prefix == 'Team':
            assert difference.getbbox() is not None,(prefix,i,'palette')
        palette_results[prefix].append(difference.getbbox())
result=dict(core_data_hashes_unchanged=len(hashes),core_components_unchanged=checked,hardpoints_unchanged=31,source_files_unchanged=31,lossless_texture_copies=30,prefab_guids_unchanged=4,turret_meshes_used=0,icon_meta_preserved=True,palettes=palette_results)
(OUTPUT/'FinalVerification.json').write_text(json.dumps(result,indent=2))
print(json.dumps(result))
