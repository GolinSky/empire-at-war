import json, shutil
from pathlib import Path
root=Path('Temp/ISDIImport')
manifest={}
for report_path in root.glob('*/ConversionReport.json'):
 report=json.loads(report_path.read_text());variant=report['variant']
 ship=variant.startswith('ISDI')
 folder='ISDI' if ship else variant
 model=Path('Assets/Art/Models/EmpireShips')/folder/(variant+'.fbx');model.parent.mkdir(parents=True,exist_ok=True)
 shutil.copyfile(report_path.parent/(variant+'.fbx'),model)
 textures=Path('Assets/Art/Textures/Models/EmpireShips')/folder;textures.mkdir(parents=True,exist_ok=True)
 used=set()
 for material in report['materials']:
  for field in ['base','normal']:
   old=material[field]
   if old is None:continue
   new=old.replace(variant+'_',folder+'_',1)
   shutil.copyfile(report_path.parent/'Textures'/(old+'.png'),textures/(new+'.png'))
   used.add(new)
   if field=='base' and 'Colorize' in material['shader']:
    shutil.copyfile(report_path.parent/'Textures'/(old+'_TeamMask.png'),textures/(new+'_TeamMask.png'))
   material[field]=new
 report['model_path']=model.as_posix();report['folder']=folder
 manifest[variant]=report
(root/'ArtManifest.json').write_text(json.dumps(manifest,indent=2))
print('Staged',len(manifest),'FBXs and',len(list(Path('Assets/Art/Textures/Models/EmpireShips/ISDI').glob('*.png'))),'shared ship textures')
