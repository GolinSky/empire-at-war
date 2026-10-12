"""Adapt the existing source-attachment art builder and isolated preview renderer."""
from pathlib import Path

folder = Path('Tools/Blender/Endurance')
source = Path('Tools/Blender/ISDIRemake/BuildArt.cs').read_text()
source = source.replace('ISDIRemake', 'Endurance').replace('ISDI', 'Endurance').replace('EmpireShips', 'RebellionShips')
start = source.index('        var wreck = new GameObject')
end = source.index('        AssetDatabase.SaveAssets();', start)
source = source[:start] + source[end:]
source = source.replace('(float)JObject.Parse(File.ReadAllText(TASK + "Baseline.json"))["length"]', '180f')
source = source.replace('Saved full Remake ISD I with 14 source-bone attachments and dedicated DeathClone geometry.', 'Saved Endurance with 14 source-bone attachments at 180 project units.')
source = source.replace('foreach (var attachment in audit["attachments"])\n                AddModel((string)attachment["variant"], Bone(hull, (string)attachment["bone"]), manifest);', 'var bones = hull.GetComponentsInChildren<Transform>(true);\n            foreach (var attachment in audit["attachments"])\n                AddModel((string)attachment["variant"], bones.Single(t=>t.name.Equals((string)attachment["bone"], StringComparison.OrdinalIgnoreCase)), manifest);')
source = source.replace('renderer.enabled = !(bool)manifest[name]["after"]["meshes"][renderer.name]["hidden"];', 'renderer.enabled = !(bool)manifest[name]["after"]["meshes"][renderer.name]["hidden"] && !manifest[name]["before"]["meshes"][renderer.name]["materials"].Any(slot=>manifest[name]["materials"].Any(m=>(string)m["name"]==(string)slot && ((string)m["shader"]).Contains("ShadowVolume")));')
# Source alpha includes neutral panels; recolor only the authored red paint.
source = source.replace('material.SetFloat("_TeamLiveryStrength", 0);', '''material.SetFloat("_TeamLiveryStrength", 1);
                    material.SetFloat("_TeamLiveryHue", 0);
                    material.SetFloat("_TeamLiveryHueRange", .06f);
                    material.SetFloat("_TeamLiveryMinSaturation", .5f);''')
source = source.replace('    private static Transform Bone(GameObject root, string name) => root.GetComponentsInChildren<Transform>(true).Single(t => t.name.Equals(name, StringComparison.OrdinalIgnoreCase));\n', '')
source = source.replace('        return obj;', '''        foreach(var renderer in obj.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.sharedMaterials.Length>1).ToArray())
        {
            var original=renderer.GetComponent<MeshFilter>().sharedMesh;
            for(int slot=0;slot<renderer.sharedMaterials.Length;slot++)
            {
                var mesh=UnityEngine.Object.Instantiate(original);mesh.name=name+"_"+renderer.name+"_Slot"+slot;
                mesh.subMeshCount=1;mesh.SetIndices(original.GetIndices(slot),original.GetTopology(slot),0);
                string path="Assets/Art/Models/RebellionShips/Endurance/"+mesh.name+".asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if(saved==null)AssetDatabase.CreateAsset(mesh,path);
                else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);mesh=saved;EditorUtility.SetDirty(saved);}
                var part=new GameObject(mesh.name);part.transform.SetParent(renderer.transform,false);
                part.AddComponent<MeshFilter>().sharedMesh=mesh;part.AddComponent<MeshRenderer>().sharedMaterial=renderer.sharedMaterials[slot];
            }
            renderer.enabled=false;
        }
        return obj;''')
folder.joinpath('BuildArt.cs').write_text(source)

source = Path('Tools/Blender/MC75Profundity/Render.cs').read_text()
source = source.replace('MC75Profundity', 'Endurance').replace('MC75', 'Endurance')
source = source.replace('camera.orthographicSize=78', 'camera.orthographicSize=108')
source = source.replace('new Vector4(-80,80,0,0)', 'new Vector4(-90,90,0,0)')
# Preserve every slot on the assembled carrier's multi-material hull.
start = source.index('            foreach(var renderer in renderers)\n            {\n                if(renderer.sharedMaterial.shader.name!="EmpireAtWar/Ship Lit"){renderer.enabled=false;continue;}\n                var material=new Material')
end = source.index('            Render(camera,target,texture,PREVIEWS+"Wreck.png");', start)
source = source[:start] + '''            foreach(var renderer in renderers)
            {
                if (!renderer.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Lit")) { renderer.enabled=false; continue; }
                renderer.sharedMaterials = renderer.sharedMaterials.Select(original =>
                {
                    var material = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/Endurance/"+original.name+"_Wreck.mat"));
                    wreckMaterials.Add(material);
                    material.SetVector("_WreckAxis",Vector3.forward);
                    material.SetVector("_WreckCenter",Vector3.zero);
                    material.SetVector("_WreckCuts",new Vector4(-18,20,1000000,1000000));
                    material.SetVector("_WreckAxisRange",new Vector4(-90,90,0,0));
                    material.SetFloat("_WreckStartTime",0);
                    material.SetFloat("_WreckLifetime",1000000000);
                    material.SetFloat("_WreckGlowDuration",1000000000);
                    material.SetFloat("_WreckSeparationSpeed",0);
                    material.SetFloat("_WreckTiltSpeed",0);
                    material.SetFloat("_WreckSinkSpeed",0);
                    return material;
                }).ToArray();
                renderer.SetShaderUserValue(1);
            }
''' + source[end:]
source = source.replace('            Render(camera,target,texture,PREVIEWS+"Wreck.png");', '''            Render(camera,target,texture,PREVIEWS+"Wreck.png");
            for(int i=0;i<8;i++)
            {
                foreach(var renderer in renderers.Where(r=>r.enabled))renderer.SetShaderUserValue((uint)(i+1));
                Render(camera,target,texture,PREVIEWS+"WreckTeam"+i+".png");
            }''')
folder.joinpath('Render.cs').write_text(source)
