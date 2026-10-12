"""Use the audited binary-material exporter in an isolated Blender instance."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/NebulaImport')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import Nebula Star Destroyer')
exec(compile(source, __file__, 'exec'))
