"""Capture read-only representative migration baselines from current disk bytes."""
import json
import re
from pathlib import Path
from source_audit import audit_alo, sha256

ROOT = Path(__file__).resolve().parents[2]


def record(path):
    path = Path(path)
    result = {'path':str(path.resolve()),'exists':path.is_file()}
    if path.is_file():
        result.update(sha256=sha256(path),bytes=path.stat().st_size)
        meta=path.with_name(path.name+'.meta')
        if meta.is_file():
            match=re.search(r'^guid: ([a-f0-9]+)$',meta.read_text(),re.M)
            result['guid']=match.group(1) if match else None
    return result


def summarize(path,piece):
    model=audit_alo(path,piece)
    return {**record(path),'pieceId':piece,'bones':len(model['bones']),'meshes':len(model['meshes']),
            'triangles':sum(s['triangleCount'] for m in model['meshes'] for s in m['materials']),
            'materialSlots':sum(len(m['materials']) for m in model['meshes']),
            'root':model['bones'][0],
            'slots':[{'meshIndex':m['index'],'mesh':m['name'],'hidden':m['hidden'],
                      'slots':[{k:s[k] for k in ('slot','signature','shader','triangleCount')} for s in m['materials']]} for m in model['meshes']]}


def main():
    rothana=Path('C:/Users/golin/Downloads/Rothana.2/Models/Rothana_Stardestroyer_Full_Armed.ALO')
    endurance=json.loads((ROOT/'Tools/Blender/Endurance/Evidence/SourceAudit.json').read_text())
    models=[summarize(rothana,'Rothana')]
    for name,variant in endurance['variants'].items():
        models.append(summarize(variant['file'],name))
    for level in range(1,6):
        models.append(summarize(ROOT/f'output/aotr-space-stations/preview-selection/Vanilla/Data/ART/MODELS/RB_STATION_{level:02}.ALO',f'RebelLevel{level}'))
    previous=[]
    for path,expected in endurance['source_hashes'].items():
        row=record(path)
        row['historicalHashMatches']=row.get('sha256')==expected
        previous.append(row)
    assets=[record(p) for p in (ROOT/'Assets').rglob('*') if p.is_file() and p.suffix!='.meta' and
            any(name.lower() in str(p.relative_to(ROOT)).lower() for name in ('Rothana','Endurance','RebelSpaceStation','RebellionSpaceStation'))]
    registries=[]
    for path in (ROOT/'Assets/Settings').rglob('*.asset'):
        text=path.read_text(errors='replace')
        if any(asset.get('guid','__absent__') in text for asset in assets if asset.get('guid')):
            registries.append(record(path))
    report={'schemaVersion':1,'capturedLocalDate':'2026-10-28','checks':{'diskInventory':'passed','unityReadback':'not-run','runtime':'not-run','visualReview':'not-run'},
            'models':models,'enduranceOriginalFiles':previous,'assets':assets,'referencingSettings':registries,
            'stationArchives':[record(Path('D:/SteamLibrary/steamapps/common/Star Wars Empire at War/GameData/Data')/name) for name in ('config.meg','models.meg')],
            'legacyAttachmentCounts':{level:len(rows) for level,rows in json.loads((ROOT/'Tools/Blender/RebelSpaceStation/Attachments.json').read_text()).items()},
            'limitations':['Disk snapshots do not prove component injection, gameplay registration routing or runtime behavior.',
                            'Station archive attachment extraction and XML-derived instance comparison still required; legacy attachment counts are not fresh acceptance.']}
    target=ROOT/'Tools/AloImport/Evidence/Baseline.json'
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({'models':len(models),'assets':len(assets),'sourceHashesMatch':all(r['historicalHashMatches'] for r in previous),'baseline':str(target)}))


if __name__=='__main__':
    main()
