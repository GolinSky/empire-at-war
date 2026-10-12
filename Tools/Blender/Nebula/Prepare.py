"""Audit the installed Remake Nebula hull, attachments and original materials."""
from pathlib import Path

source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source = source.replace('Temp/ISDIRemakeImport', 'Temp/NebulaImport')
source = source.replace("Resolve('Star_Destroyer')", "Resolve('Nebula_Star_Destroyer')")
source = source.replace("{'ISDI':", "{'Nebula':").replace("'ISDIPart'", "'NebulaPart'")
source = source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})")
source = source.replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Project wreck uses the verified live hull.")
source = source.replace("refs = sorted(set(name.decode('ascii') for name in re.findall(rb'(?i)[a-z0-9_.-]+\\.(?:dds|tga)', path.read_bytes())))", "refs = sorted({value for mesh in audit['material_audit']['meshes'] for material in mesh['materials'] for value in material['properties'].values() if isinstance(value, str) and value.lower().endswith(('.dds', '.tga'))})")
exec(compile(source, __file__, 'exec'))
