"""Use the audited binary-material exporter in an isolated Blender instance."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Convert.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/TitanImport')
source = source.replace('Integrate the Imperial I-class Star Destroyer into the Empire faction using Steam Workshop mod 1770851727.', 'import Titan-class Battlecruiser - read about it in Empire Fleet Import note. read ALO_MODEL_IMPORT_GUIDE. hangar squadrons left disabled. run test - fix relate to this unit issues. commit after')
exec(compile(source, __file__, 'exec'))
