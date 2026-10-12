"""Stage audited MC50 art and lossless source texture copies."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Stage.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/MC50Import')
source = source.replace('EmpireShips/ISDI', 'RebellionShips/MC50')
source = source.replace("'ISDI'", "'MC50'")
source = source.replace("Image.new('L', (32,32),255).save(textures/'ISDI_TeamStripes_TeamMask.png')", '')
exec(compile(source, __file__, 'exec'))
