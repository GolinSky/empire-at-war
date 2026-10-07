"""Stage this model's art and texture evidence in the type-first Unity layout."""
import shutil
from pathlib import Path

TASK = Path('Temp/MC80IndependenceImport')
NAME = 'MC80Independence'
model = Path(f'Assets/Art/Models/RebellionShips/{NAME}')
textures = Path(f'Assets/Art/Textures/Models/RebellionShips/{NAME}')
for path in (model, textures, Path(f'Assets/Art/Materials/Models/RebellionShips/{NAME}'), Path(f'Assets/Art/Materials/Wrecks/{NAME}')):
    path.mkdir(parents=True, exist_ok=True)
shutil.copy2(TASK / (NAME+'.fbx'), model / (NAME+'.fbx'))
for path in (TASK / 'Textures').glob('*.png'):
    shutil.copy2(path, textures / path.name)
print('Staged Independence FBX and', len(list(textures.glob('*.png'))), 'textures')
