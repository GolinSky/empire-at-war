"""Audit MC30c Remake source using the verified Remake binary audit workflow."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/MC30cImport')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('MC-30_Frigate')")
source = source.replace("{'ISDI':", "{'MC30c':").replace("'ISDIPart'", "'MC30cPart'").replace("models['ISDIWreck']", "models['MC30cSourceWreck']")
source = source.replace("models['MC30cSourceWreck'] = death['values']['Space_Model_Name'][0]", "# Remake references RV_MC30CMONCAL_D.ALO, which is absent. Generate the project wreck from the live hull.")
exec(compile(source, 'Tools/Blender/MC30c/Prepare.py', 'exec'))
