"""Preserve editable conversion output and verify unchanged sources and texture pixels."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

TASK = Path('Temp/ISDIIIImport')
DESTINATION = Path('output/aotr-empire-units/ISDIII-Converted')
SOURCE_TEXTURES = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/TEXTURES')
audit = json.loads((TASK/'SourceAudit.json').read_text())
for path, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == expected, path
reports = json.loads((TASK/'ConversionReport.json').read_text())
texture_count = 0
for report in reports:
    for material in report['materials']:
        for key in ('base', 'normal'):
            if key+'Map' not in material:
                continue
            original = Image.open(SOURCE_TEXTURES/material[key]).convert('RGBA')
            converted = Image.open(material[key+'Map']).convert('RGBA')
            assert converted.size == original.size and converted.tobytes() == original.tobytes(), material[key]
            texture_count += 1
for name in ('ISDIIIIcon', 'ISDIIISilhouette'):
    path=Path('Assets/Art/Textures/Ui/Icons/ShipIcon')/(name+'.png')
    image=Image.open(path)
    assert image.size==(512,512) and image.mode=='RGBA'
    alpha=image.getchannel('A')
    assert alpha.getextrema()==(0,255) and alpha.getbbox() is not None
    left,top,right,bottom=alpha.getbbox()
    assert left>0 and top>0 and right<512 and bottom<512, name
DESTINATION.mkdir(parents=True,exist_ok=True)
for source in (TASK/'Output').iterdir():
    if source.suffix.lower() in ('.blend','.fbx'):
        shutil.copy2(source,DESTINATION/source.name)
shutil.copytree(TASK/'Output/Textures',DESTINATION/'Textures',dirs_exist_ok=True)
shutil.copytree(TASK/'Previews',DESTINATION/'Previews',dirs_exist_ok=True)
for name in ('SourceAudit','ConversionReport','GeometryReport','UnityGeometry','Verification','GameplayBounds','HardpointMapping','ArtBounds'):
    shutil.copy2(TASK/(name+'.json'),DESTINATION/(name+'.json'))
for source in TASK.glob('*SourceGeometry.json'):
    shutil.copy2(source,DESTINATION/source.name)
for name in ('ISDIIIIcon','ISDIIISilhouette'):
    shutil.copy2(Path('Assets/Art/Textures/Ui/Icons/ShipIcon')/(name+'.png'),DESTINATION/(name+'.png'))
shutil.copy2('Tools/Blender/ISDIII/README.md',DESTINATION/'README.md')
(DESTINATION/'SourceCredits.txt').write_text('AOTR Workshop item 1397421866. Source unit E_Imperial_Star_Destroyer_3; model EV_ISD3.ALO.\nOriginal model/texture paths and SHA-256 fingerprints are recorded in SourceAudit.json.\nConverted assets retain the source artwork; no claim of original authorship is made.\n')
report=dict(sourceFilesUnchanged=len(audit['source_hashes']),exactTextureComparisons=texture_count,models=len(reports),transparentIcons=2,liveTeamPreviews=8,wreckTeamPreviews=8,playMode=False,automatedTests=False)
(DESTINATION/'PackagingVerification.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
