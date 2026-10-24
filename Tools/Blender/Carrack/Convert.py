"""Convert RaW's Carrack through Blender MCP; preserve all other scenes."""
import bpy
import bmesh
import json
from mathutils import Matrix, Vector

OUTPUT_DIRECTORY = 'F:/Private/empire-at-war/Temp/CarrackImport/Output'
AUDIT = json.loads('{\n  "source_unit": "Carrack",\n  "source_model": "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Models\\\\ReV_carrack.ALO",\n  "requested_name": "Carrack-class Cruiser",\n  "model": {\n    "bones": [\n      {\n        "name": "Root",\n        "parent_index": 4294967295,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          0.0,\n          0.0,\n          1.0,\n          0.0,\n          0.0,\n          0.0,\n          0.0,\n          1.0,\n          0.0\n        ]\n      },\n      {\n        "name": "HULL",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          -0.14056789875030518,\n          0.0,\n          1.0,\n          0.0,\n          -11.457505226135254,\n          0.0,\n          0.0,\n          1.0,\n          1.6509047746658325\n        ]\n      },\n      {\n        "name": "MuzzleA_00",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          11.706940650939941,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          -80.169677734375,\n          0.0,\n          0.0,\n          1.0,\n          -7.014991760253906\n        ]\n      },\n      {\n        "name": "MuzzleA_01",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          -10.715988159179688,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          -80.169677734375,\n          0.0,\n          0.0,\n          1.0,\n          -7.014991760253906\n        ]\n      },\n      {\n        "name": "MuzzleA_02",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          15.370361328125,\n          0.0,\n          1.0,\n          0.0,\n          -58.25625228881836,\n          0.0,\n          0.0,\n          1.0,\n          5.8301496505737305\n        ]\n      },\n      {\n        "name": "MuzzleA_03",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          12.52713680267334,\n          0.0,\n          1.0,\n          0.0,\n          -17.00377655029297,\n          0.0,\n          0.0,\n          1.0,\n          5.8301496505737305\n        ]\n      },\n      {\n        "name": "MuzzleA_04",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          13.241314888000488,\n          0.0,\n          1.0,\n          0.0,\n          23.681354522705078,\n          0.0,\n          0.0,\n          1.0,\n          5.8301496505737305\n        ]\n      },\n      {\n        "name": "MuzzleA_05",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          18.558345794677734,\n          0.0,\n          1.0,\n          0.0,\n          56.04056930541992,\n          0.0,\n          0.0,\n          1.0,\n          10.647329330444336\n        ]\n      },\n      {\n        "name": "MuzzleA_06",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -0.9999997615814209,\n          -0.0007578452350571752,\n          1.1443145736533822e-10,\n          -11.852883338928223,\n          -0.0007578452350571752,\n          0.9999997019767761,\n          -1.509957741063772e-07,\n          23.681354522705078,\n          6.938893903907228e-18,\n          -1.5099583094979607e-07,\n          -1.0,\n          5.8301496505737305\n        ]\n      },\n      {\n        "name": "MuzzleA_07",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -0.9999279975891113,\n          0.011999947018921375,\n          -1.811941485740931e-09,\n          -11.138705253601074,\n          0.011999947018921375,\n          0.9999279975891113,\n          -1.509849170133748e-07,\n          -17.00377655029297,\n          5.551115123125783e-17,\n          -1.5099580252808664e-07,\n          -1.0,\n          5.8301496505737305\n        ]\n      },\n      {\n        "name": "MuzzleA_08",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -0.9999818801879883,\n          -0.00602684635668993,\n          9.497653774559467e-10,\n          -13.981929779052734,\n          -0.00602684635668993,\n          0.9999818205833435,\n          -1.509788489784114e-07,\n          -58.25625228881836,\n          -3.9821868025313734e-11,\n          -1.509818332579016e-07,\n          -1.0,\n          5.8301496505737305\n        ]\n      },\n      {\n        "name": "MuzzleA_09",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -0.9999732971191406,\n          -0.007321443874388933,\n          1.1055072413057587e-09,\n          -17.16991424560547,\n          -0.007321443874388933,\n          0.9999731779098511,\n          -1.5099176664534752e-07,\n          56.04056930541992,\n          0.0,\n          -1.5099580252808664e-07,\n          -1.0,\n          10.647329330444336\n        ]\n      },\n      {\n        "name": "MuzzleA_10",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          0.0,\n          -1.0,\n          -0.0,\n          6.246368885040283,\n          1.0,\n          0.0,\n          0.0,\n          79.67942810058594,\n          0.0,\n          -0.0,\n          1.0,\n          -8.982683181762695\n        ]\n      },\n      {\n        "name": "MuzzleA_11",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          0.0,\n          -1.0,\n          -0.0,\n          -5.665263652801514,\n          1.0,\n          0.0,\n          0.0,\n          79.67942810058594,\n          0.0,\n          -0.0,\n          1.0,\n          -8.982683181762695\n        ]\n      },\n      {\n        "name": "Hp01",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          11.679673194885254,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          -75.94877624511719,\n          0.0,\n          0.0,\n          1.0,\n          -6.97574520111084\n        ]\n      },\n      {\n        "name": "Hp02",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          -10.753350257873535,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          -75.94877624511719,\n          0.0,\n          0.0,\n          1.0,\n          -6.97574520111084\n        ]\n      },\n      {\n        "name": "Hp03",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          18.73183822631836,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          60.31538391113281,\n          0.0,\n          0.0,\n          1.0,\n          10.757771492004395\n        ]\n      },\n      {\n        "name": "Hp04",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          -16.901416778564453,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          60.31538391113281,\n          0.0,\n          0.0,\n          1.0,\n          10.757771492004395\n        ]\n      },\n      {\n        "name": "Hp05",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          12.365899085998535,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          -12.71838665008545,\n          0.0,\n          0.0,\n          1.0,\n          5.872716426849365\n        ]\n      },\n      {\n        "name": "Hp06",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          13.06147289276123,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          28.003833770751953,\n          0.0,\n          0.0,\n          1.0,\n          5.872716426849365\n        ]\n      },\n      {\n        "name": "Hp07",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          -12.133837699890137,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          28.003833770751953,\n          0.0,\n          0.0,\n          1.0,\n          5.872716426849365\n        ]\n      },\n      {\n        "name": "Hp08",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          -12.133837699890137,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          -12.57075023651123,\n          0.0,\n          0.0,\n          1.0,\n          5.872716426849365\n        ]\n      },\n      {\n        "name": "Shieldgen",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          0.30235975980758667,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          -32.823524475097656,\n          0.0,\n          0.0,\n          1.0,\n          15.2391357421875\n        ]\n      },\n      {\n        "name": "Thruster",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          -1.1920928955078125e-07,\n          0.9999999403953552,\n          0.0,\n          0.3023545444011688,\n          -0.9999999403953552,\n          -1.1920928955078125e-07,\n          0.0,\n          85.82783508300781,\n          0.0,\n          0.0,\n          1.0,\n          2.7707834243774414\n        ]\n      },\n      {\n        "name": "engines",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          -0.0,\n          -0.7152824401855469,\n          -0.0,\n          0.0,\n          -1.0,\n          87.33805847167969,\n          0.0,\n          1.0,\n          0.0,\n          8.647927284240723\n        ]\n      },\n      {\n        "name": "pe_tartanengine_sml",\n        "parent_index": 24,\n        "visible": 1,\n        "matrix": [\n          -1.0,\n          -8.7422719730057e-08,\n          1.5099581673894136e-07,\n          0.9439920783042908,\n          -8.742276236262114e-08,\n          0.9999999403953552,\n          -3.5762786865234375e-07,\n          -4.219535827636719,\n          -1.5099578831723193e-07,\n          -3.5762786865234375e-07,\n          -0.9999999403953552,\n          -1.1553802490234375\n        ]\n      },\n      {\n        "name": "collision",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          -0.26110541820526123,\n          0.0,\n          1.0,\n          0.0,\n          -4.603557109832764,\n          0.0,\n          0.0,\n          1.0,\n          -12.114222526550293\n        ]\n      },\n      {\n        "name": "shadow",\n        "parent_index": 0,\n        "visible": 1,\n        "matrix": [\n          1.0,\n          0.0,\n          0.0,\n          -0.14056789875030518,\n          0.0,\n          1.0,\n          0.0,\n          -11.457505226135254,\n          0.0,\n          0.0,\n          1.0,\n          1.6509047746658325\n        ]\n      }\n    ],\n    "objects": [\n      {\n        "kind": "0x400",\n        "name": "HULL"\n      },\n      {\n        "kind": "0x400",\n        "name": "engines"\n      },\n      {\n        "kind": "0x400",\n        "name": "collision"\n      },\n      {\n        "kind": "0x400",\n        "name": "shadow"\n      }\n    ],\n    "connections": [\n      {\n        "object_index": 0,\n        "bone_index": 1,\n        "bone": "HULL"\n      },\n      {\n        "object_index": 1,\n        "bone_index": 24,\n        "bone": "engines"\n      },\n      {\n        "object_index": 2,\n        "bone_index": 26,\n        "bone": "collision"\n      },\n      {\n        "object_index": 3,\n        "bone_index": 27,\n        "bone": "shadow"\n      }\n    ]\n  },\n  "materials": {\n    "file": "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Models\\\\ReV_carrack.ALO",\n    "sha256": "bc507ee84e45304aa74ae4d61ca7f7540eadd65b76d80d5e139828bb35d6962f",\n    "meshes": [\n      {\n        "materials": [\n          {\n            "properties": {\n              "Emissive": [\n                0.0,\n                0.0,\n                0.0,\n                0.0\n              ],\n              "Diffuse": [\n                1.0,\n                1.0,\n                1.0,\n                0.0\n              ],\n              "Specular": [\n                0.45098042488098145,\n                0.45098042488098145,\n                0.45098042488098145,\n                0.0\n              ],\n              "Shininess": [\n                32.0\n              ],\n              "Colorization": [\n                1.0,\n                1.0,\n                1.0,\n                1.0\n              ],\n              "UVOffset": [\n                0.0,\n                0.0,\n                0.0,\n                0.0\n              ],\n              "BaseTexture": "ReV_carrack.dds",\n              "NormalTexture": "rev_carrack_normals.dds"\n            },\n            "shader": "MeshBumpColorize.fx",\n            "vertexCount": 7896,\n            "triangleCount": 4638\n          }\n        ],\n        "name": "HULL",\n        "hidden": false,\n        "collision": false\n      },\n      {\n        "materials": [\n          {\n            "properties": {},\n            "shader": "alDefault.fx",\n            "vertexCount": 9,\n            "triangleCount": 8\n          }\n        ],\n        "name": "engines",\n        "hidden": true,\n        "collision": false\n      },\n      {\n        "materials": [\n          {\n            "properties": {\n              "Color": [\n                0.0,\n                0.0,\n                1.0,\n                0.5\n              ]\n            },\n            "shader": "MeshCollision.fx",\n            "vertexCount": 24,\n            "triangleCount": 12\n          }\n        ],\n        "name": "collision",\n        "hidden": true,\n        "collision": true\n      },\n      {\n        "materials": [\n          {\n            "properties": {\n              "DebugColor": [\n                0.0,\n                1.0,\n                1.0,\n                1.0\n              ]\n            },\n            "shader": "MeshShadowVolume.fx",\n            "vertexCount": 13545,\n            "triangleCount": 18735\n          }\n        ],\n        "name": "shadow",\n        "hidden": false,\n        "collision": false\n      }\n    ],\n    "connections": [\n      {\n        "mesh": 0,\n        "bone": 1\n      },\n      {\n        "mesh": 1,\n        "bone": 24\n      },\n      {\n        "mesh": 2,\n        "bone": 26\n      },\n      {\n        "mesh": 3,\n        "bone": 27\n      }\n    ],\n    "proxies": [\n      {\n        "name": "pe_tartanengine_sml",\n        "bone": 25\n      }\n    ]\n  },\n  "textures": {\n    "ReV_carrack.dds": {\n      "stem": "Carrack_ReV_carrack",\n      "size": [\n        1024,\n        1024\n      ],\n      "alpha": [\n        0,\n        255\n      ],\n      "team_mask_coverage": 0.9978876113891602\n    },\n    "rev_carrack_normals.dds": {\n      "stem": "Carrack_ReV_carrack_normals",\n      "size": [\n        1024,\n        1024\n      ],\n      "alpha": [\n        0,\n        255\n      ]\n    }\n  },\n  "source_hashes": {\n    "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Models\\\\ReV_carrack.ALO": "bc507ee84e45304aa74ae4d61ca7f7540eadd65b76d80d5e139828bb35d6962f",\n    "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\XML\\\\Units_space_republic_carrack.xml": "f4b6da60d68ea7cf26208953c4621590bdfe5157adcab3c13ff346e634348357",\n    "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Textures\\\\ReV_carrack.dds": "b3f42eba16336746b876c46b54be5589fac4ca81e5eb672dc7dc4c04bc391833",\n    "D:\\\\SteamLibrary\\\\steamapps\\\\workshop\\\\content\\\\32470\\\\1129810972\\\\Data\\\\Art\\\\Textures\\\\ReV_carrack_normals.dds": "855e6463d2655b05735315ee88a98df5a192ef39d6241b465b267d0a271b5370"\n  }\n}')


def Snapshot(scene):
    bpy.context.window.scene = scene
    bpy.context.view_layer.update()
    meshes = [obj for obj in scene.objects if obj.type == 'MESH']
    points = [obj.matrix_world @ vertex.co for obj in meshes if not obj.hide_render and obj['EaWShader'] != 'MeshAdditive.fx' for vertex in obj.data.vertices]
    return dict(
        meshes={obj['EaWName']: dict(exported_name=obj.name, vertices=len(obj.data.vertices), triangles=sum(len(p.vertices)-2 for p in obj.data.polygons), uv_layers=len(obj.data.uv_layers), hidden=obj.hide_render, materials=[slot.material.name for slot in obj.material_slots], slot_triangles=[sum(len(p.vertices)-2 for p in obj.data.polygons if p.material_index == slot) for slot in range(len(obj.material_slots))]) for obj in meshes},
        bones={bone.name: dict(parent=bone.parent.name if bone.parent else None, head=list(obj.matrix_world @ bone.head_local)) for obj in scene.objects if obj.type == 'ARMATURE' for bone in obj.data.bones},
        bounds_min=[min(p[i] for p in points) for i in range(3)],
        bounds_max=[max(p[i] for p in points) for i in range(3)])


scene = bpy.data.scenes['Carrack Source']
bpy.context.window.scene = scene
rig = next(obj for obj in scene.objects if obj.type == 'ARMATURE')
assert len(rig.data.bones) == len(AUDIT['model']['bones']) == 28
rig.name = 'CarrackRig'
# The importer stopped before its final hierarchy cleanup; restore audited parents.
bpy.context.view_layer.objects.active = rig
rig.hide_set(False)
rig.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
for index, record in enumerate(AUDIT['model']['bones']):
    bone = rig.data.edit_bones[record['name']]
    parent = record['parent_index']
    bone.parent = rig.data.edit_bones[AUDIT['model']['bones'][parent]['name']] if parent != 4294967295 else None
bpy.ops.object.mode_set(mode='OBJECT')
textures = {}
for filename, record in AUDIT['textures'].items():
    image = bpy.data.images.load(OUTPUT_DIRECTORY + '/Textures/' + record['stem'] + '.png', check_existing=False)
    image.alpha_mode = 'CHANNEL_PACKED'
    image.colorspace_settings.is_data = 'normal' in record['stem'].lower()
    image.pack()
    textures[filename.lower()] = image

materials = []
records = {record['name']: record for record in AUDIT['materials']['meshes']}
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    name = obj.name.rsplit('.', 1)[0] if obj.name[-4:-3] == '.' else obj.name
    record = records[name]
    obj['EaWName'] = name
    shader = record['materials'][0]['shader']
    obj['EaWShader'] = shader
    obj.hide_render = bool(record['hidden'])
    obj.hide_set(obj.hide_render)
    obj['EaWHidden'] = obj.hide_render
    obj['EaWHelper'] = shader in ('MeshShadowVolume.fx', 'MeshCollision.fx', 'alDefault.fx')
    # Complete only this scene's cleanup after the importer's global cleanup hit another scene.
    if shader == 'MeshCollision.fx':
        mesh = bmesh.new()
        mesh.from_mesh(obj.data)
        bmesh.ops.remove_doubles(mesh, verts=list(mesh.verts), dist=.0001)
        mesh.to_mesh(obj.data)
        mesh.free()
    obj.data.materials.clear()
    offset = 0
    for index, definition in enumerate(record['materials']):
        props = definition['properties']
        material = bpy.data.materials.new('Carrack_' + name + '_Slot' + str(index))
        material['EaWShader'] = definition['shader']
        material.use_nodes = True
        nodes, links = material.node_tree.nodes, material.node_tree.links
        nodes.clear()
        output, surface = nodes.new('ShaderNodeOutputMaterial'), nodes.new('ShaderNodeBsdfPrincipled')
        links.new(surface.outputs['BSDF'], output.inputs['Surface'])
        surface.inputs['Roughness'].default_value = .65
        base_name, normal_name = props.get('BaseTexture', '').lower(), props.get('NormalTexture', '').lower()
        base, normal = None, None
        if base_name:
            base = AUDIT['textures'][props['BaseTexture']]['stem']
            node = nodes.new('ShaderNodeTexImage')
            node.image = textures[base_name]
            links.new(node.outputs['Color'], surface.inputs['Base Color'])
            if definition['shader'] == 'MeshAdditive.fx':
                links.new(node.outputs['Color'], surface.inputs['Emission'])
        if normal_name:
            normal = AUDIT['textures'][props['NormalTexture']]['stem']
            node, bump = nodes.new('ShaderNodeTexImage'), nodes.new('ShaderNodeNormalMap')
            node.image = textures[normal_name]
            links.new(node.outputs['Color'], bump.inputs['Color'])
            links.new(bump.outputs['Normal'], surface.inputs['Normal'])
        obj.data.materials.append(material)
        if len(record['materials']) == 1:
            for polygon in obj.data.polygons:
                polygon.material_index = 0
        else:
            for polygon in obj.data.polygons[offset:offset+definition['triangleCount']]:
                polygon.material_index = index
        offset += definition['triangleCount']
        materials.append(dict(name=material.name, shader=definition['shader'], base=base, normal=normal, mask=(base+'_TeamMask') if 'Colorize' in definition['shader'] else None, properties=props))
    assert len(obj.data.polygons) == sum(row['triangleCount'] for row in record['materials'])
    for constraint in obj.constraints:
        constraint.inverse_matrix = Matrix.Identity(4)

before = Snapshot(scene)
for obj in scene.objects:
    if obj.type != 'MESH':
        continue
    world = obj.matrix_world.copy()
    constraint = obj.constraints[0]
    assert len(obj.constraints) == 1 and constraint.type == 'CHILD_OF'
    obj.parent, obj.parent_type, obj.parent_bone = constraint.target, 'BONE', constraint.subtarget
    obj.constraints.remove(constraint)
    bpy.context.view_layer.update()
    obj.matrix_world = world
after_parenting = Snapshot(scene)
assert max(abs(before[key][i]-after_parenting[key][i]) for key in ('bounds_min', 'bounds_max') for i in range(3)) < .001
bpy.ops.wm.save_as_mainfile(filepath=OUTPUT_DIRECTORY + '/Carrack.blend', copy=True)
fbx = OUTPUT_DIRECTORY + '/Carrack.fbx'
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=False, object_types={'MESH','ARMATURE','EMPTY'}, add_leaf_bones=False, use_armature_deform_only=False, bake_anim=False, use_custom_props=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, path_mode='RELATIVE')
validation = bpy.data.scenes.new('Carrack FBX Validation')
bpy.context.window.scene = validation
bpy.ops.import_scene.fbx(filepath=fbx, use_anim=False, use_custom_props=True, automatic_bone_orientation=False)
for obj in validation.objects:
    if obj.type == 'MESH':
        obj.hide_render = bool(obj['EaWHidden'])
        obj.hide_set(obj.hide_render)
after = Snapshot(validation)
assert set(before['bones']) == set(after['bones'])
assert all(before['bones'][n]['parent'] == after['bones'][n]['parent'] for n in before['bones'])
bone_error = max((Vector(before['bones'][n]['head']) - Vector(after['bones'][n]['head'])).length for n in before['bones'])
assert bone_error < .001
assert set(before['meshes']) == set(after['meshes'])
assert all(before['meshes'][n]['triangles'] == after['meshes'][n]['triangles'] for n in before['meshes'])
assert all(before['meshes'][n]['slot_triangles'] == after['meshes'][n]['slot_triangles'] for n in before['meshes'])
report = dict(models={'Carrack':dict(before=before,after=after)}, materials=materials, bone_error=bone_error, fbx=fbx, source_model=AUDIT['source_model'])
print('CARRACK_REPORT ' + json.dumps(report))
bpy.context.window.scene = scene
for obj in scene.objects:
    if obj.type == 'ARMATURE':
        obj.hide_set(True)
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.shading.type = 'MATERIAL'
        area.spaces.active.overlay.show_overlays = False
        area.spaces.active.region_3d.view_location = Vector((0,0,0))
        area.spaces.active.region_3d.view_distance = 330
        area.spaces.active.region_3d.view_rotation = Vector((1,-1,1)).to_track_quat('Z','Y')
        area.spaces.active.region_3d.view_perspective = 'ORTHO'
