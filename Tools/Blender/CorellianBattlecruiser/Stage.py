"""Stage only this import's art; retain existing Unity metadata on reruns."""
import shutil
from pathlib import Path

NAME = 'CorellianBattlecruiser'
TASK = Path('Temp/CorellianBattlecruiserImport')
model = Path(f'Assets/Art/Models/RebellionShips/{NAME}')
textures = Path(f'Assets/Art/Textures/Models/RebellionShips/{NAME}')
for path in (model, textures, Path(f'Assets/Art/Materials/Models/RebellionShips/{NAME}'), Path(f'Assets/Art/Materials/Wrecks/{NAME}')):
    path.mkdir(parents=True, exist_ok=True)
for source in TASK.glob('*.fbx'):
    shutil.copy2(source, model/source.name)
for source in (TASK/'Textures').glob('*.png'):
    shutil.copy2(source, textures/source.name)
print('Staged',len(list(model.glob('*.fbx'))),'models and',len(list(textures.glob('*.png'))),'textures')
