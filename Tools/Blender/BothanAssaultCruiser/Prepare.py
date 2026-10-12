"""Audit the Remake Bothan source, keeping project balance separate."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/BothanAssaultCruiserImport')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Bothan_Assault_Cruiser_MP')")
source = source.replace("{'ISDI':", "{'BothanAssaultCruiser':").replace("'ISDIPart'", "'BothanAssaultCruiserPart'")
source = source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})")
source = source.replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Build a project wreck from the live hull.")
exec(compile(source, __file__, 'exec'))
