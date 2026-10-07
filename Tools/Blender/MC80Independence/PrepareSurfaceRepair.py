"""Build a safe Blender MCP request from audited original material records."""
import json
from pathlib import Path

TASK = Path('Temp/MC80IndependenceImport')
source = json.loads((TASK/'BinaryMaterialAudit.json').read_text())['RV_MC80_Independence.ALO']
textures = json.loads((TASK/'TextureMapping.json').read_text())
names = {
    ('REBEL_MON_CAL_HOME_ONE_1.DDS','MeshBumpSpecGlowColorize.fx'): 'Rebel_Mon_Cal_Home_One_1',
    ('REBEL_MON_CAL_HOME_ONE_1.DDS','MeshBumpColorize.fx'): 'Rebel_Mon_Cal_Home_One_11',
    ('REBEL_MON_CAL_HOME_ONE_2.DDS','MeshBumpSpecGlowColorize.fx'): 'Rebel_Mon_Cal_Home_One_2_SpecGlow',
    ('REBEL_MON_CAL_HOME_ONE_2.DDS','MeshBumpColorize.fx'): 'Rebel_Mon_Cal_Home_One_21',
    ('REBEL_MON_CAL_HOME_ONE_3.DDS','MeshBumpColorize.fx'): 'Rebel_Mon_Cal_Home_One_31',
    ('REBEL_MON_CAL_HOME_ONE_3.DDS','MeshBumpSpecGlowColorize.fx'): 'Rebel_Mon_Cal_Home_One_3_SpecGlow',
    ('REBEL_MON_CAL_HOME_ONE_4.DDS','MeshBumpSpecGlowColorize.fx'): 'Rebel_Mon_Cal_Home_One_4',
    ('REBEL_MON_CAL_HOME_ONE_4.DDS','MeshBumpColorize.fx'): 'Rebel_Mon_Cal_Home_One_41',
    ('ENGINE_EXHAUST.DDS','MeshBumpColorize.fx'): 'Engine_Exhaust1'
}
assigned = {}
definitions = {}
for mesh in source['meshes']:
    for material in mesh['materials']:
        shader = material['shader']
        base = material['properties'].get('BaseTexture','').upper()
        signature = json.dumps([shader,material['properties']],sort_keys=True)
        if signature not in assigned:
            if not base:
                suffix = 'COLLISION' if 'Collision' in shader else 'SHADOW'
            elif (base,shader) in names:
                suffix = names[base,shader]
            else:
                stem = textures[base][0].removeprefix('MC80Independence_')
                suffix = stem if 'Additive' in shader and base not in ('REBEL_MON_CAL_HOME_ONE_1.DDS','REBEL_MON_CAL_HOME_ONE_3.DDS') else stem+'_'+shader.removeprefix('Mesh').removesuffix('.fx')
            name = 'MC80Independence_'+suffix
            if name in definitions:
                name += '_Variant'+str(len(definitions))
            assert len(name) <= 63, name
            assigned[signature] = name
            definition = dict(name=name,shader=shader,properties=material['properties'],
                              base=textures[base][0] if base else None)
            normal = material['properties'].get('NormalTexture','').upper()
            definition['normal'] = textures[normal][0] if normal else None
            definitions[name] = definition
        material['materialName'] = assigned[signature]
source['materialDefinitions'] = list(definitions.values())
functions = Path('Tools/Blender/MC80Independence/Convert.py').read_text().split('assert bpy.types.blendermcp_server.port')[0]
body = Path('Tools/Blender/MC80Independence/RepairSurfaces.py').read_text()
code = functions+'\nMATERIAL_AUDIT = '+repr(source)+'\n'+body
(TASK/'RepairSurfacesRequest.json').write_text(json.dumps(dict(tool='execute_blender_code',arguments=dict(code=code))))
print(len(source['meshes']), 'meshes;', len(definitions), 'independent material definitions')
