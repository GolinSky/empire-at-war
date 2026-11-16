import json, sys, hashlib
from pathlib import Path
from PIL import Image
root=Path('F:/Private/empire-at-war')
sys.path.insert(0,str(root/'Tools/AloImport'))
from source_audit import audit_alo
art=Path('F:/EaW/Republic_at_War_121_MINIMAL/Mods/Republic_At_War/Data/Art')
out=root/'Temp/GinivexImport'
audit=audit_alo(art/'Models/SeV_fanblade_fighter.ALO','Ginivex',include_geometry=False)
(out/'SourceAudit.json').write_text(json.dumps(audit,indent=2))
textures={}; policies=[]
for mesh in audit['meshes']:
 for slot in mesh['materials']:
  refs=[]
  for key,value in slot['properties'].items():
   if key not in ('BaseTexture','NormalTexture') or not isinstance(value,str) or value=='None': continue
   source=next((art/'Textures').glob(value),None)
   if source is None: raise ValueError(value)
   filename='Ginivex_'+source.stem+('.png')
   target=out/filename
   with Image.open(source) as im:
    im.save(target)
    assert im.convert('RGBA').tobytes()==Image.open(target).convert('RGBA').tobytes()
    textures[value.casefold()]={'path':str(target),'size':im.size,'source':str(source),'sha256':hashlib.sha256(source.read_bytes()).hexdigest()}
   refs.append({'source':value,'semantic':'normal' if key=='NormalTexture' else 'albedo'})
  policies.append({'selector':{'piece':'Ginivex','meshIndex':mesh['index'],'slot':slot['slot'],'shader':slot['shader'],'parameters':[{'name':k,'value':v} for k,v in slot['properties'].items()]},'surface':'additive' if slot['shader']=='MeshAdditive.fx' else 'opaque','normalConvention':'directx','textures':refs})
spec={'output':str(out),'textures':textures,'piece':{'file':str(art/'Models/SeV_fanblade_fighter.ALO'),'pieceId':'Ginivex','sha256':audit['sha256']},'recipe':{'pieces':[{'id':'Ginivex','transportRigName':'GinivexRig'}],'materials':policies}}
(out/'Spec.json').write_text(json.dumps(spec,indent=2))
print(json.dumps({'meshes':len(audit['meshes']),'bones':len(audit['bones']),'textures':list(textures)}))
