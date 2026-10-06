"""Verify persisted registrations and explicit prefab member/weapon bindings."""
import hashlib
import json
import math
import re
from pathlib import Path
from PIL import Image

ROOT = Path('F:/Private/empire-at-war')


def Read(path):
    return (ROOT/path).read_text(encoding='utf-8-sig')


def Guid(path):
    return re.search(r'^guid: (\w+)',Read(path+'.meta'),re.M).group(1)


def Property(text,name):
    return float(re.search(r'<'+name+r'>k__BackingField: ([\d.]+)',text).group(1))


view = Read('Assets/Prefabs/Models/Squadrons/YWingBomberSquadronView.prefab')
data = Read('Assets/Settings/Data/Squadron/YWingBomberSquadronData.asset')
members = view.count('Assembly-CSharp::EmpireAtWar.ViewComponents.Squadrons.FighterView')
weapons = view.count('Assembly-CSharp::EmpireAtWar.ViewComponents.Health.WeaponHardPoint')
assert members == 6 and weapons == 24
for field,value in {'MemberHull':30,'MemberShields':30,'CruiseSpeed':30,'CombatSpeed':30,'ShieldRegenerateValue':0.15,'LaserShieldDamageMultiplier':0.7,'HullRepairPerSecond':0}.items():
    assert Property(data,field) == value,(field,Property(data,field))
types = re.findall(r'<WeaponType>k__BackingField: (\d+)',view)
assert types.count('46') == 6 and types.count('47') == 6 and types.count('48') == 12
gun_ids = []
member_ids = []
for block in view.split('--- !u!'):
    if 'Assembly-CSharp::EmpireAtWar.ViewComponents.Health.WeaponHardPoint' in block:
        gun_ids.append(int(re.search(r'<Id>k__BackingField: (\d+)',block).group(1)))
    if 'Assembly-CSharp::EmpireAtWar.ViewComponents.Squadrons.FighterView' in block:
        member_ids.append(int(re.search(r'^  id: (\d+)',block,re.M).group(1)))
assert sorted(gun_ids) == list(range(24))
assert sorted(member_ids) == list(range(6))
donors = ['Assets/Prefabs/Models/Squadrons/YWing.prefab','Assets/Prefabs/Ui/Reinforcement/YWingReinforcementView.prefab']
preview = Read('Assets/Prefabs/Ui/Reinforcement/YWingBomberReinforcementView.prefab')
assert all(Guid(path) not in view+preview for path in donors)
assert 'key: 301' in Read('Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset')
assert 'key: 301' not in Read('Assets/Settings/Data/Factions/Empire/EmpireFaction.asset')
assert 'key: 301' not in Read('Assets/Settings/Data/Factions/Republic/RepublicFaction.asset')
for path in ['Assets/Prefabs/Models/Squadrons/YWingBomberSquadronView.prefab','Assets/Settings/Data/Squadron/YWingBomberSquadronData.asset']:
    guid = Guid(path)
    assert guid in Read('Assets/Settings/AssetMappingData.asset')
    groups = [p for p in (ROOT/'Assets/AddressableAssetsData/AssetGroups').glob('*.asset') if guid in p.read_text(encoding='utf-8-sig')]
    assert len(groups) == 1,(path,groups)
icon_path = 'Assets/Art/Textures/Ui/Icons/ShipIcon/YWingBomberIcon.png'
icon_guid = Guid(icon_path)
for path in ['Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset','Assets/Settings/Data/Models/ShipUi/ShipUiData.asset','Assets/Settings/Data/Tooltip/TooltipIconData.asset']:
    assert icon_guid in Read(path)
assert Guid('Assets/Prefabs/Ui/Reinforcement/YWingBomberReinforcementView.prefab') in Read('Assets/Settings/Data/Reinforcement/ReinforcementData.asset')
with Image.open(ROOT/icon_path) as icon:
    assert icon.size == (512,512)
    alpha = icon.getchannel('A')
    crop = alpha.getbbox()
    assert crop and min(crop[:2]) > 0 and max(crop[2:]) < 512
audit = json.loads(Read('Temp/YWingBomberImport/Output/SourceAudit.json'))
assert all(hashlib.sha256(Path(path).read_bytes()).hexdigest() == checksum for path,checksum in audit['source_hashes'].items())
unity_geometry = json.loads(Read('Temp/YWingBomberImport/UnityGeometry.json'))
maximum_unity_bone_error = 0
for imported, variant in zip(unity_geometry, ['Hull', 'Turret']):
    expected = json.loads(Read('Temp/YWingBomberImport/Output/'+variant+'Conversion.json'))['before']
    assert {m['name']:m['triangles'] for m in imported['meshes']} == {name:m['triangles'] for name,m in expected['meshes'].items()}
    assert all(m['uv'] == m['vertices'] for m in imported['meshes'])
    for name, bone in expected['bones'].items():
        parent = bone['parent'] or Path(imported['path']).stem
        matches = [t for t in imported['model'] if t['name'] == name and t['parent'] == parent]
        assert len(matches) == 1,(name,matches)
        x,y,z = bone['head']
        error = math.dist(matches[0]['position'],[-x,z,-y])
        maximum_unity_bone_error = max(maximum_unity_bone_error,error)
        assert error < 0.001,(name,error)
team_files = [ROOT/'Temp/YWingBomberImport/Previews'/('Team'+str(i)+'.png') for i in range(8)]
assert len({hashlib.sha256(path.read_bytes()).hexdigest() for path in team_files}) == 8
assert (ROOT/'Temp/YWingBomberImport/Previews/Placement.png').is_file()
live = json.loads(Read('Temp/YWingBomberImport/Verification.json'))
assert live['fighters'] == 6 and live['uniqueIds'] == 24
assert live['missingScripts'] == 0 and not live['brokenReferences']
assert live['healthMembers'] == live['flightMembers'] == 6
assert live['weaponBindings'] == live['fogWeapons'] == 24
assert live['teamRenderers'] == 24 and live['existingRepublicYWingHull'] == 60
report = {'members':members,'weapon_count':weapons,'weapons_per_craft':4,'total_hull':180,'total_shields':180,
    'unique_weapon_ids':len(set(gun_ids)),'faction':'Rebellion only','icon_crop':crop,'registrations_verified':True,
    'donor_visuals_absent':True,'source_hashes_unchanged':True,'live_final_readback':'passed',
    'unity_mesh_triangle_uv_and_bone_hierarchy':'passed','maximum_unity_bone_error':maximum_unity_bone_error,
    'team_palette_and_placement_renders':'eight distinct team renders; blue/green and six-craft placement visually checked',
    'play_mode_or_automated_tests':'not run by this task'}
(ROOT/'Temp/YWingBomberImport/SavedVerification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report))
