"""Compare Unity triangle corners, authored normals and hierarchy with original ALO binaries."""
import hashlib
import itertools
import json
import math
import re
import struct
import sys
from collections import defaultdict
from pathlib import Path
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT / 'Tools/Blender/RebelSpaceStation'))
from Prepare import Chunks
TASK = ROOT / 'Temp/AotrEmpressStationImport'
audit=json.loads((TASK/'SourceAudit.json').read_text())
reports=json.loads((TASK/'ConversionReport.json').read_text())
imported=json.loads((TASK/'UnityGeometry.json').read_text())

def Multiply(a,b):
    return [[sum(a[r][k]*b[k][c] for k in range(4)) for c in range(4)] for r in range(4)]

def Position(matrix,vertex):
    p=[sum(matrix[r][c]*(*vertex,1)[c] for c in range(4)) for r in range(3)]
    return [-p[0]*.02,p[2]*.02,-p[1]*.02]

def Normal(matrix,vertex):
    p=[sum(matrix[r][c]*vertex[c] for c in range(3)) for r in range(3)]
    p=[-p[0],p[2],-p[1]]
    length=math.sqrt(sum(v*v for v in p))
    return [v/length for v in p] if length else p

results=[]
for report in reports:
    name=report['name'];model=audit['models'][name];actual=imported[name]
    source=Path(model['file'])
    assert hashlib.sha256(source.read_bytes()).hexdigest()==model['sha256']
    matrices=[]
    for row in model['bones']:
        matrix=[row['matrix'][i:i+4] for i in (0,4,8)]+[[0,0,0,1]]
        if row['parent_index']!=4294967295:
            matrix=Multiply(matrices[row['parent_index']],matrix)
        matrices.append(matrix)
    bone_names=[]
    for row in model['bones']:
        bone_name=row['name']
        if bone_name in bone_names:
            base,separator,suffix=bone_name.rpartition('.')
            base=base if separator and suffix.isdigit() else bone_name
            number=1
            bone_name=f'{base}.{number:03}'
            while bone_name in bone_names:
                number+=1
                bone_name=f'{base}.{number:03}'
        bone_names.append(bone_name)
    by_transport={n:i for i,n in enumerate(bone_names)}
    bone_error=0
    for bone_name,source_name in report['source_bone_names'].items():
        index=by_transport[bone_name]
        row=model['bones'][index]
        source_parent=None if row['parent_index']==4294967295 else bone_names[row['parent_index']]
        parent=actual['bones'][bone_name]['parent']
        assert parent==source_parent,(name,bone_name,parent)
        error=math.dist(actual['bones'][bone_name]['position'],Position(matrices[index],(0,0,0)))
        assert error<.0001,(name,bone_name,error)
        bone_error=max(bone_error,error)
    geometry_error=uv_error=normal_error=0
    checked=0
    payloads=[data for kind,data in Chunks(source.read_bytes()) if kind==0x400]
    for row,payload in zip(model['meshes'],payloads):
        key=row['key']
        if key not in actual['meshes']:
            assert report['source']['meshes'][key]['hidden']
            continue
        connection=next(c for c in model['connections'] if c['mesh']==row['object_index'])
        matrix=matrices[connection['bone']]
        expected=[]
        for kind,data in Chunks(payload):
            if kind!=0x10000:continue
            fields=dict(Chunks(data))
            vertex_count,triangle_count=struct.unpack_from('<II',fields[0x10001])
            stride=144 if 0x10007 in fields else 128
            vertices=fields[0x10007 if stride==144 else 0x10005]
            corners=[]
            for i in range(vertex_count):
                pos=Position(matrix,struct.unpack_from('<3f',vertices,i*stride))
                normal=Normal(matrix,struct.unpack_from('<3f',vertices,i*stride+12))
                u,v=struct.unpack_from('<2f',vertices,i*stride+24)
                corners.append((*pos,u,-v,*normal))
            indices=struct.unpack('<'+'H'*triangle_count*3,fields[0x10004])
            expected.extend(corners[i] for i in indices)
        mesh=actual['meshes'][key]
        assert len(mesh['triangles'])==len(expected),(name,key,len(mesh['triangles']),len(expected))
        buckets=defaultdict(list)
        for corner in expected:buckets[tuple(round(p*1000) for p in corner[:3])].append(corner)
        for index in mesh['triangles']:
            corner=(*mesh['vertices'][index],*mesh['uv'][index],*mesh['normals'][index])
            bucket=tuple(round(p*1000) for p in corner[:3])
            candidates=[p for delta in itertools.product((-1,0,1),repeat=3) for p in buckets.get(tuple(k+d for k,d in zip(bucket,delta)),())]
            assert candidates,(name,key,corner)
            # Coincident triangle corners may carry different authored hard-edge normals.
            viable=[p for p in candidates if math.dist(p[:3],corner[:3])<.0001 and math.dist(p[3:5],corner[3:5])<.0001]
            assert viable,(name,key,corner)
            match=min(viable,key=lambda p:math.dist(p[5:],corner[5:]))
            pe=math.dist(match[:3],corner[:3]);ue=math.dist(match[3:5],corner[3:5]);ne=math.dist(match[5:],corner[5:])
            assert pe<.0001 and ue<.0001 and ne<.0001,(name,key,pe,ue,ne)
            geometry_error=max(geometry_error,pe);uv_error=max(uv_error,ue);normal_error=max(normal_error,ne)
        checked+=len(expected)//3
    results.append(dict(name=name,bones=len(actual['bones']),triangles=checked,bone_error=bone_error,geometry_error=geometry_error,uv_error=uv_error,normal_error=normal_error))
for texture in audit['textures'].values():
    path=Path(texture['source']);assert hashlib.sha256(path.read_bytes()).hexdigest()==texture['sha256']
    png=ROOT/'Assets/Art/Textures/Models/SpaceStations/AotrEmpressStation'/(texture['name']+'.png')
    assert Image.open(path).convert('RGBA').tobytes()==Image.open(png).convert('RGBA').tobytes()
for path,digest in audit['xml_hashes'].items():assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==digest
baseline=json.loads((TASK/'PreservationBaseline.json').read_text())
for path,digest in baseline['hashes'].items():assert hashlib.sha256(Path(path).read_bytes()).hexdigest()==digest,path
def Mappings(text):
    return dict(re.findall(r'- key: ([^\r\n]+)\s+value:\s+m_AssetGUID: ([0-9a-f]+)',text))
before=Mappings(baseline['mapping'])
after=Mappings((ROOT/'Assets/Settings/AssetMappingData.asset').read_text())
for key,guid in before.items():assert after[key]==guid,('Existing asset mapping changed',key)
output=dict(success=True,models=results,sourceTextures=len(audit['textures']),stationAssetsUnchanged=len(baseline['hashes']))
(TASK/'VerifiedSourceGeometry.json').write_text(json.dumps(output,indent=2))
print('PASS',len(results),'models;',sum(r['triangles'] for r in results),'source triangles;',len(audit['textures']),'lossless textures; existing station art unchanged')
print('Maximum errors',{k:max(r[k] for r in results) for k in ('bone_error','geometry_error','uv_error','normal_error')})
