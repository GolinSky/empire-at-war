from pathlib import Path
source = Path('Tools/Blender/ISDIRemake/Prepare.py').read_text()
source=source.replace('Temp/ISDIRemakeImport','Temp/TitanImport').replace("Resolve('Star_Destroyer')","Resolve('Titan_Star_Destroyer_MP')").replace("{'ISDI':","{'Titan':").replace("'ISDIPart'","'TitanPart'")
source=source.replace("unit['values']['HardPoints'][0].split(',')", "re.split(r'[,\\s]+', unit['values']['HardPoints'][0])")
source=source.replace("death = Resolve(unit['values']['Death_Clone'][0].split(',')[1].strip())", "death = dict(inheritance=[], values={})").replace("models['ISDIWreck'] = death['values']['Space_Model_Name'][0]", "# Wreck derives from verified live hull.")
source=source.replace("refs = sorted(set(name.decode('ascii') for name in re.findall(rb'(?i)[a-z0-9_.-]+\\.(?:dds|tga)', path.read_bytes())))", "refs = sorted({v for mesh in audit['material_audit']['meshes'] for mat in mesh['materials'] for v in mat['properties'].values() if isinstance(v,str) and v.lower().endswith(('.dds','.tga'))})")
source=source.replace("tree = ast.parse(Path(path).read_text(encoding='utf-8-sig'))", "tree = ast.parse(Path(path).read_text(encoding='utf-8-sig').replace('from Prepare import Audit, OUTPUT', ''))")
source=source.replace("for name in refs:", "for name in refs:\n            if name.lower() == 'w_te_rock_f_02_b.dds':\n                audit.setdefault('unresolved_effect_textures', []).append(name)\n                continue")
source=source.replace("texture = texture_files[key]", """if key not in texture_files:
                print('Extracting', key, flush=True)
                fallback = OUTPUT / 'VanillaTextures'
                fallback.mkdir(exist_ok=True)
                LoadFunctions('Tools/Blender/RebelSpaceStation/PrepareAttachments.py')['Extract'](Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/corruption/Data/textures.meg'), [key], fallback)
                texture_files[key] = fallback / key
            texture = texture_files[key]""")
exec(compile(source,__file__,'exec'))

