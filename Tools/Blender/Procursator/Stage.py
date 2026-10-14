import json, shutil
from pathlib import Path
TASK=Path('Temp/ProcursatorImport')
models={}
for path in TASK.glob('*/ConversionReport.json'):
 report=json.loads(path.read_text()); name=report['variant']; models[name]=report
 for kind in ('Models','Textures/Models','Materials/Models'):
  Path(f'Assets/Art/{kind}/EmpireShips/Procursator').mkdir(parents=True,exist_ok=True)
 shutil.copy2(path.parent/(name+'.fbx'),f'Assets/Art/Models/EmpireShips/Procursator/{name}.fbx')
 for texture in path.parent.joinpath('Textures').glob('*.png'):
  shutil.copy2(texture,f'Assets/Art/Textures/Models/EmpireShips/Procursator/{texture.name}')
Path('Assets/Art/Materials/Wrecks/Procursator').mkdir(parents=True,exist_ok=True)
(TASK/'ConversionReport.json').write_text(json.dumps(dict(models=models,materials=[m for r in models.values() for m in r['materials']]),indent=2))
print('Staged',len(models),'models')
