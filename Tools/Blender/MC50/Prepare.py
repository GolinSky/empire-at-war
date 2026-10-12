"""Audit Remake MC50 geometry/materials; gameplay balance comes from Rebel units."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/MC50Import')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('MC-50_Frigate')")
source = source.replace("{'ISDI':", "{'MC50':").replace("'ISDIPart'", "'MC50Part'")
source = source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})")
source = source.replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Build the Unity wreck from the visible hull.")
exec(compile(source, __file__, 'exec'))
