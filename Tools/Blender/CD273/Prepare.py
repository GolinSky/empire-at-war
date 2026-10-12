"""Resolve CD-273 source and audit its original binary materials and textures."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/CD273Import')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Corellian_Destroyer')")
source = source.replace("{'ISDI':", "{'CD273':").replace("'ISDIPart'", "'CD273Part'")
source = source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})")
source = source.replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Derive a static project wreck from the visible hull.")
exec(compile(source, 'Tools/Blender/CD273/Prepare.py', 'exec'))
