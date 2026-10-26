"""Stage verified MC30c art and aggregate its conversion manifest."""
import json
import shutil
from pathlib import Path

TASK = Path('Temp/MC30cImport')
NAME = 'MC30c'
report = json.loads((TASK/NAME/'ConversionReport.json').read_text())
for kind in ('Models', 'Textures/Models', 'Materials/Models'):
    Path(f'Assets/Art/{kind}/RebellionShips/{NAME}').mkdir(parents=True, exist_ok=True)
Path(f'Assets/Art/Materials/Wrecks/Rebellion/{NAME}').mkdir(parents=True, exist_ok=True)
shutil.copy2(TASK/NAME/(NAME+'.fbx'), f'Assets/Art/Models/RebellionShips/{NAME}/{NAME}.fbx')
for texture in (TASK/NAME/'Textures').glob('*.png'):
    shutil.copy2(texture, f'Assets/Art/Textures/Models/RebellionShips/{NAME}/{texture.name}')
combined = dict(models={NAME: report}, materials=report['materials'])
(TASK/'ConversionReport.json').write_text(json.dumps(combined, indent=2))
print('Staged MC30c model and lossless texture copies')
