import hashlib
import json
import re
from pathlib import Path
from PIL import Image, ImageOps

ROOT = Path('Temp/VictoryIImport')
SOURCE = Path('D:/SteamLibrary/steamapps/workshop/content/32470/1397421866/Data/ART/TEXTURES')
reports = [json.loads(m.group(1)) for m in re.finditer(r'^(?:Code executed successfully: )?CONVERSION_JSON (.+)$', (ROOT/'ConversionLog.txt').read_text(encoding='utf-8-sig'), re.M)]
assert len(reports) == 3
audit = json.loads((ROOT/'SourceAudit.json').read_text(encoding='utf-8'))
for report in reports:
    name = report['name']
    models = Path('Assets/Art/Models/EmpireShips')/name
    textures = Path('Assets/Art/Textures/Models/EmpireShips')/name
    materials = Path('Assets/Art/Materials/Models/EmpireShips')/name
    for folder in (models,textures,materials):
        folder.mkdir(parents=True,exist_ok=True)
    fbx = ROOT/'Output'/f'{name}.fbx'
    (models/f'{name}.fbx').write_bytes(fbx.read_bytes())
    for material in report['materials']:
        for key in ('base','normal'):
            filename = material[key]
            if not filename or filename=='None':
                continue
            source = SOURCE/filename
            assert source.is_file(), source
            audit['source_hashes'][str(source)]=hashlib.sha256(source.read_bytes()).hexdigest()
            token = 'Normal' if key=='normal' else ('Emissive' if 'Additive' in material['shader'] else 'Albedo')
            target = textures/f'{name}_{source.stem}_{token}.png'
            image = Image.open(source).convert('RGBA')
            image.save(target)
            image.save(ROOT/'Output'/'Textures'/f'{source.stem}.png')
            assert Image.open(target).convert('RGBA').tobytes()==image.tobytes()
            material[key+'Map']=str(target).replace('\\','/')
            if key=='base' and 'Colorize' in material['shader']:
                mask=ImageOps.invert(image.getchannel('A'))
                target=textures/f'{name}_{source.stem}_TeamMask.png'
                mask.save(target)
                material['maskMap']=str(target).replace('\\','/')
                material['maskCoverage']=sum(v>0 for v in mask.tobytes())/len(mask.tobytes())
    print(name,'meshes',len(report['source']['meshes']),'bones',len(report['source']['bones']),
          'triangles',sum(m['triangles'] for m in report['source']['meshes'].values()),
          'bone error',report['bone_error'],'bounds error',report['bounds_error'])
(ROOT/'ConversionReport.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
(ROOT/'SourceAudit.json').write_text(json.dumps(audit,indent=2),encoding='utf-8')
