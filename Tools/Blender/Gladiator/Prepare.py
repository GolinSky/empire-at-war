from pathlib import Path
p=Path('Tools/Blender/ISDIRemake/Prepare.py').read_text().replace('Temp/ISDIRemakeImport','Temp/GladiatorImport').replace("Resolve('Star_Destroyer')","Resolve('Gladiator_Star_Destroyer')").replace("{'ISDI':","{'Gladiator':").replace("'ISDIPart'","'GladiatorPart'").replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Keep source death model in evidence; project wreck uses the verified living hull.")
exec(compile(p,__file__,'exec'))

