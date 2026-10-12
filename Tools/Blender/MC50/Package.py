"""Verify source hashes, texture fidelity, palette renders and metadata; save evidence."""
import hashlib
import json
import shutil
from pathlib import Path
from PIL import Image

task = Path('Temp/MC50Import')
output = Path('output/remake-rebel-ships/MC50-Converted')
audit = json.loads((task/'SourceAudit.json').read_text())
for filename, expected in audit['source_hashes'].items():
    assert hashlib.sha256(Path(filename).read_bytes()).hexdigest() == expected, filename
for source in audit['variants']['MC50']['textures'].values():
    original = Image.open(source['source']).convert('RGBA')
    copied = Image.open(task/'MC50/Textures'/(source['stem']+'.png')).convert('RGBA')
    assert original.tobytes() == copied.tobytes(), source['stem']
icon = Image.open('Assets/Art/Textures/Ui/Icons/ShipIcon/MC50Icon.png').convert('RGBA')
assert icon.size == (512,512)
box = icon.getchannel('A').getbbox()
assert box and all(0 < x < 512 for x in box), box
palettes = {}
for prefix in ('Team','WreckTeam'):
    images = [Image.open(task/'Previews'/(prefix+str(i)+'.png')).convert('RGBA') for i in range(8)]
    assert len({hashlib.sha256(image.tobytes()).hexdigest() for image in images}) == 8
    palettes[prefix] = [hashlib.sha256(image.tobytes()).hexdigest() for image in images]
assets = []
for root in ('Assets/Art/Models/RebellionShips/MC50','Assets/Art/Materials/Models/RebellionShips/MC50','Assets/Art/Textures/Models/RebellionShips/MC50','Assets/Art/Materials/Wrecks/MC50'):
    assets.extend(p for p in Path(root).rglob('*') if p.is_file() and p.suffix!='.meta')
assets.extend(Path(p) for p in ('Assets/Prefabs/Models/Ships/MC50.prefab','Assets/Prefabs/Models/Ships/MC50ShipView.prefab','Assets/Prefabs/Models/Wrecks/MC50WreckView.prefab','Assets/Prefabs/Ui/Reinforcement/MC50ReinforcementView.prefab','Assets/Settings/Data/Ship/MC50ShipData.asset','Assets/Settings/Data/Ship/Wreck/MC50WreckData.asset','Assets/Settings/Data/Tooltip/Matchups/MC50Matchups.asset','Assets/Art/Models/Shields/MC50Shield.asset','Assets/Art/Textures/Ui/Icons/ShipIcon/MC50Icon.png','Assets/Art/Textures/Ui/Icons/ShipIcon/MC50Silhouette.png'))
for asset in assets:
    assert asset.exists() and Path(str(asset)+'.meta').exists(), str(asset)
output.mkdir(parents=True,exist_ok=True)
for filename in ('MC50.fbx','MC50.blend','ConversionReport.json'):
    shutil.copyfile(task/'MC50'/filename,output/filename)
shutil.copytree(task/'MC50/Textures',output/'Textures',dirs_exist_ok=True)
shutil.copytree(task/'Previews',output/'Previews',dirs_exist_ok=True)
for filename in ('SourceAudit.json','UnityInspection.json','HardpointMapping.json','ArtReport.json'):
    shutil.copyfile(task/filename,output/filename)
evidence = dict(sourceHashesVerified=len(audit['source_hashes']),losslessTextureReferences=len(audit['variants']['MC50']['textures']),iconAlphaBounds=box,allEightLiveAndWreckPalettesDistinct=True,assetMetadataVerified=len(assets),paletteHashes=palettes,unity=json.loads((task/'UnityInspection.json').read_text()))
(output/'Verification.json').write_text(json.dumps(evidence,indent=2))
Path('Tools/Blender/MC50/ImportEvidence.json').write_text(json.dumps(evidence,indent=2))
print(json.dumps({k:v for k,v in evidence.items() if k not in ('paletteHashes','unity')},indent=2))
