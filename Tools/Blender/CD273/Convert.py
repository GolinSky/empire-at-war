"""Convert audited CD-273 art in its isolated Blender MCP process."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/CD273Import')
source = source.replace('9886', '9896').replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/CD273/Call.py')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import CD-273 Corellian Destroyer')
exec(compile(source, 'Tools/Blender/CD273/Convert.py', 'exec'))
