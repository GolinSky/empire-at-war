"""Audit Quasar's Remake source and losslessly stage its referenced textures."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/QuasarFireImport')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Quasar_Carrier_MP')")
source = source.replace("{'ISDI':", "{'QuasarFire':").replace("'ISDIPart'", "'QuasarFirePart'")
source = source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})")
source = source.replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Quasar has no source wreck; derive the project wreck from the hull.")
exec(compile(source, __file__, 'exec'))
