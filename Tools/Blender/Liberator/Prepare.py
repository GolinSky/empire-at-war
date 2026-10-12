"""Resolve Liberator's source art; gameplay balance comes from Rebel units."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/LiberatorImport')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Liberator_Carrier_R')")
source = source.replace("{'ISDI':", "{'Liberator':").replace("'ISDIPart'", "'LiberatorPart'")
source = source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})")
source = source.replace("    models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "    # Generate the project wreck from the verified live hull.")
start = source.index('        refs = sorted(')
end = source.index('        for name in refs:', start)
source = source[:start] + "        refs = sorted({value for mesh in audit['material_audit']['meshes'] for material in mesh['materials'] for key, value in material['properties'].items() if key.endswith('Texture') and value != 'None'})\n" + source[end:]
exec(compile(source, __file__, 'exec'))
