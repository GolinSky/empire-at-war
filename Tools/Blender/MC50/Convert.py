"""Convert MC50 in its own safe-mode Blender MCP process on port 9891."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/MC50Import')
source = source.replace('9886', '9891')
source = source.replace('Tools/Blender/ISDIRemake/Call.py', 'Tools/Blender/MC50/Call.py')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import MC50 Medium Cruiser')
exec(compile(source, __file__, 'exec'))
