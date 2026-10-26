"""Stage the validated Dauntless art and normalize the conversion manifest."""
import json
import shutil
from pathlib import Path

task = Path('Temp/DauntlessImport')
report = json.loads((task/'Dauntless/ConversionReport.json').read_text())
combined = dict(materials=report['materials'], models={'Dauntless': report})
(task/'ConversionReport.json').write_text(json.dumps(combined, indent=2))
for category in ('Models', 'Materials/Models', 'Textures/Models'):
    Path(f'Assets/Art/{category}/RebellionShips/Dauntless').mkdir(parents=True, exist_ok=True)
Path('Assets/Art/Materials/Wrecks/Rebellion/Dauntless').mkdir(parents=True, exist_ok=True)
shutil.copy2(task/'Dauntless/Dauntless.fbx', 'Assets/Art/Models/RebellionShips/Dauntless/Dauntless.fbx')
for source in (task/'Dauntless/Textures').glob('*.png'):
    shutil.copy2(source, Path('Assets/Art/Textures/Models/RebellionShips/Dauntless')/source.name)
