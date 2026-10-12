"""Audit Endurance's Remake skirmish hull and required bridge/tower attachments."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/EnduranceImport')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Endurance_Fleet_Carrier_MP')")
source = source.replace("{'ISDI':", "{'Endurance':").replace("'ISDIPart'", "'EndurancePart'")
source = source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})")
source = source.replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Derive the project wreck from the assembled carrier.")
source = source.replace("refs = sorted(set(name.decode('ascii') for name in re.findall(rb'(?i)[a-z0-9_.-]+\\.(?:dds|tga)', path.read_bytes())))", "refs = sorted({value for mesh in audit['material_audit']['meshes'] for material in mesh['materials'] for value in material['properties'].values() if isinstance(value, str) and value.lower().endswith(('.dds', '.tga'))})")
exec(compile(source, __file__, 'exec'))
