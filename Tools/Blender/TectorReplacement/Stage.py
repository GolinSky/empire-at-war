import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

OUTPUT = Path('F:/CodexArtifacts/tector-preview/UnityReplacement')
ASSETS = Path('Assets')
source = json.loads((OUTPUT.parent/'SourceAudit.json').read_text())
snapshot = {str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in (ASSETS/'Settings').rglob('*') if p.is_file()}
snapshot.update({str(p): hashlib.sha256(p.read_bytes()).hexdigest() for p in (ASSETS/'AddressableAssetsData').rglob('*') if p.is_file()})
(OUTPUT/'CoreDataHashes.json').write_text(json.dumps(snapshot,indent=2))
for path in ['Prefabs/Models/Ships/Empire/Tector.prefab','Prefabs/Models/Ships/Empire/TectorShipView.prefab','Prefabs/Models/Wrecks/TectorWreckView.prefab','Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab','Art/Textures/Ui/Icons/ShipIcon/TectorIcon.png','Art/Textures/Ui/Icons/ShipIcon/TectorSilhouette.png']:
    asset = ASSETS/path
    backup = OUTPUT/'OriginalAssets'/path
    backup.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(asset,backup)
    shutil.copyfile(str(asset)+'.meta',str(backup)+'.meta')
texture_folder=ASSETS/'Art/Textures/Models/EmpireShips/Tector'
model_folder=ASSETS/'Art/Models/EmpireShips/Tector'
model_folder.mkdir(parents=True,exist_ok=True)
texture_folder.mkdir(parents=True,exist_ok=True)
textures={}
for name,record in source['textures'].items():
    path=texture_folder/('Tector_Remake_'+Path(name).stem+'.png')
    original=Image.open(record['path']).convert('RGBA')
    original.save(path)
    assert Image.open(path).convert('RGBA').tobytes()==original.tobytes(),name
    textures[name]=dict(path=path.as_posix(),sha256=record['sha256'])
materials={}
counts={}
for record in source['meshes']:
    name=record['name']; count=counts.get(name,0); counts[name]=count+1
    name=name if count==0 else name+'.'+str(count).zfill(3)
    definition=record['materials'][0]
    materials[name]=definition
(OUTPUT/'ArtManifest.json').write_text(json.dumps(dict(model_path=(model_folder/'Tector_Remake.fbx').as_posix(),textures=textures,meshes=materials,source=source),indent=2))
print('Captured',len(snapshot),'core-data hashes and staged',len(textures),'lossless source texture copies')
