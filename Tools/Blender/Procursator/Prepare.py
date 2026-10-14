from pathlib import Path
p=Path('Tools/Blender/ISDIRemake/Prepare.py').read_text().replace('Temp/ISDIRemakeImport','Temp/ProcursatorImport').replace("Resolve('Star_Destroyer')","Resolve('Procursator')").replace("{'ISDI':","{'Procursator':").replace("'ISDIPart'","'ProcursatorPart'").replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Keep source death model in evidence; project wreck uses the verified living hull.")
exec(compile(p,__file__,'exec'))
