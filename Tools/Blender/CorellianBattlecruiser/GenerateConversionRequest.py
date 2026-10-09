"""Embed audits outside Blender; the safe-mode request uses no filesystem reads/writes."""
import json
from pathlib import Path

TASK = Path('Temp/CorellianBattlecruiserImport')
functions = Path('Tools/Blender/MC80Independence/Convert.py').read_text()
functions = functions[functions.index('def Snapshot'):functions.index('assert bpy.types.blendermcp_server.port')]
code = 'import bpy, math, itertools\nfrom mathutils import Vector\n'+functions
code += '\naudit = '+repr(json.loads((TASK/'SourceAudit.json').read_text()))
code += '\nbinary = '+repr(json.loads((TASK/'BinaryMaterialAudit.json').read_text()))+'\n'
code += Path('Tools/Blender/CorellianBattlecruiser/Convert.py').read_text()
prompt = 'Read Temp/ALO_MODEL_MAP.txt, then find the model for Corellian Battlecruiser. Read ALO_MODEL_IMPORT_GUIDE note before starting. Add this unit to rebellion, available lvl 2. 4 Heavy 2-Burst Barrage Rocket Launchers, 1 Shield Generator, 1 Engine, 8 Medium Turbolasers, 5 Laser Cannons, 1 Hangar. Power to Shields. Shield and hull regeneration: use frigate value (acclamators, captor). Tactical population 3. Combat role: ship focuses on reinforcing. Launch squadrons: X-Wing, A-Wing and bomber we have in Rebellion.'
(TASK/'ConvertRequest.json').write_text(json.dumps(dict(tool='execute_blender_code',arguments=dict(code=code,user_prompt=prompt))))
