"""Retain the editable conversion, evidence, scripts and upstream credits."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image, ImageChops

TASK = Path('Temp/MC80IndependenceImport')
OUTPUT = Path('output/aotr-rebel-units/MC80Independence-Converted')
audit = json.loads((TASK/'SourceAudit.json').read_text())
for source, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(source).read_bytes()).hexdigest()==expected, source
report = json.loads((TASK/'UnityInspection.json').read_text())
assert all(report['registrations'].values())
for prefab in report['prefabs']:
    assert prefab['missingScripts']==0 and not prefab['brokenReferences']
    assert Path(prefab['path']+'.meta').is_file()
icon=Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/MC80IndependenceIcon.png').convert('RGBA')
bounds=icon.getchannel('A').getbbox()
assert icon.size==(512,512) and bounds and min(bounds[:2])>0 and max(bounds[2:])<512
differences={}
for prefix in ('Team','WreckTeam'):
    blue=Image.open(TASK/f'Previews/{prefix}0.png').convert('RGB')
    green=Image.open(TASK/f'Previews/{prefix}2.png').convert('RGB')
    delta=ImageChops.difference(blue,green)
    changed=sum(any(pixel) for pixel in delta.getdata())
    assert changed>100
    differences[prefix]=changed
coordinated = json.loads((TASK/'CoordinatedEditorVerification.json').read_text(encoding='utf-8-sig'))
assert len(coordinated['relevantTests'])==11 and all(test['Status']=='Passed' for test in coordinated['relevantTests'])
surface = json.loads((TASK/'UnitySurfaceInspection.json').read_text(encoding='utf-8-sig'))
assert surface['visibleMeshes']==38 and surface['originalMaterialAssignmentsMatch'] and surface['uvRangeError']<.00001
stripes = json.loads((TASK/'TeamStripeReport.json').read_text(encoding='utf-8-sig'))
assert stripes['savedLiveAndWreckAssignmentsVerified'] and stripes['ownershipRenderers']==8
assert len(stripes['masks'])==4 and len(stripes['materials'])==8
for mask in stripes['masks']:
    assert .01 < mask['coverage'] < .2
    assert Image.open(mask['mask']).size==(2048,2048)
checks=dict(source_hash_count=len(audit['source_hashes']),icon_bounds=bounds,
            team_color_changed_pixels=differences,registrations=report['registrations'],
            automated_tests_initiated_by_this_import=False,combat_play_mode_run=False,
            coordinated_relevant_editor_tests_passed_before_surface_repair=len(coordinated['relevantTests']),
            original_surface_assignments_verified=True,uv_range_error=surface['uvRangeError'],
            team_stripe_masks=4,live_and_wreck_stripe_material_pairs=8)
(TASK/'PackageVerification.json').write_text(json.dumps(checks,indent=2))
OUTPUT.mkdir(parents=True,exist_ok=True)
for filename in ('MC80Independence.blend','MC80Independence.fbx','SourceAudit.json','ConversionReport.json','UnityInspection.json','HardpointMapping.json','ArtReport.json','TextureMapping.json','PrefabFinalization.json','CoordinatedEditorVerification.json','PackageVerification.json','BinaryMaterialAudit.json','SourceSurfaceAudit.json','SurfaceRepairReport.json','UnitySurfaceInspection.json','TeamStripeReport.json'):
    shutil.copy2(TASK/filename,OUTPUT/filename)
for folder in ('Textures','Previews'):
    shutil.copytree(TASK/folder,OUTPUT/folder,dirs_exist_ok=True)
shutil.copytree(TASK/'PreviewsBeforeSurfaceRepair',OUTPUT/'PreviewsBeforeSurfaceRepair',dirs_exist_ok=True)
shutil.copytree('Tools/Blender/MC80Independence',OUTPUT/'Scripts',dirs_exist_ok=True)
credits=Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Credits_and_Permissions.pdf')
shutil.copy2(credits,OUTPUT/credits.name)
(OUTPUT/'SOURCE_CREDITS.txt').write_text(
    'Source: installed Awakening of the Rebellion, Workshop 1397421866.\n'
    'Unit: R_MC80_Independence; model: RV_MC80_Independence.ALO.\n'
    'The complete supplied Credits_and_Permissions.pdf is retained.\n'
    'Individual model/texture authors have not been independently identified.\n'
    'Original ALO and DDS files were not modified; SHA-256 evidence is in SourceAudit.json.\n')
print(json.dumps(checks,indent=2))
