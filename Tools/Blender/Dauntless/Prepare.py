"""Audit the Dauntless Remake source with the established binary audit workflow."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/DauntlessImport')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Dauntless_Cruiser_R')")
source = source.replace("{'ISDI':", "{'Dauntless':").replace("'ISDIPart'", "'DauntlessPart'").replace("models['ISDIWreck']", "models['DauntlessSourceWreck']")
source = source.replace("models['DauntlessSourceWreck'] = death['values']['Space_Model_Name'][0]", "# Source damaged model references absent destruction_for_dcs.dds; project wreck uses the verified live hull.")
exec(compile(source, 'Tools/Blender/Dauntless/Prepare.py', 'exec'))
