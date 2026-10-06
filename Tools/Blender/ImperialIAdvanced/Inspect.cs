using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.ViewComponents.Squadrons;

public static class InspectImperialAdvanced
{
    public static string Main()
    {
        var result=new JObject();var art=new JArray();var manifest=JObject.Parse(File.ReadAllText("Temp/ImperialIAdvancedImport/ArtManifest.json"));
        foreach(var entry in manifest.Properties())
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>((string)entry.Value["model_path"]);var meshes=new JArray();
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var skin=renderer as SkinnedMeshRenderer;var mesh=skin!=null?skin.sharedMesh:renderer.GetComponents<MeshFilter>().Single().sharedMesh;
                var expected=entry.Value["after"]["meshes"][renderer.name];meshes.Add(new JObject{{"name",renderer.name},{"triangles",mesh.triangles.Length/3},{"expected_triangles",(int)expected["triangles"]},{"uv",mesh.uv.Length},{"vertices",mesh.vertexCount}});
            }
            float error=0;var transforms=model.GetComponentsInChildren<Transform>(true);
            foreach(var bone in ((JObject)entry.Value["after"]["bones"]).Properties())
            {
                var t=transforms.Single(x=>x.name==bone.Name && x.GetComponents<Renderer>().Length==0);var v=bone.Value["head"].Values<float>().ToArray();var expected=new Vector3(-v[0],v[2],-v[1])*.02f;
                error=Mathf.Max(error,Vector3.Distance(model.transform.InverseTransformPoint(t.position),expected));
            }
            art.Add(new JObject{{"name",entry.Name},{"meshes",meshes},{"bones",((JObject)entry.Value["after"]["bones"]).Count},{"maximum_bone_error",error}});
        }
        result["art"]=art;var prefabs=new JArray();
        string[] names={"ImperialIAdvanced","TIEInterceptor","TIEBrute","TIEPunisher"};
        foreach(string name in names)
        {
            bool ship=name==names[0];string type=ship?"Ship":"Squadron",kind=ship?"Ships":"Squadrons";
            foreach(string path in new[]{"Assets/Prefabs/Models/"+kind+"/"+name+".prefab","Assets/Prefabs/Models/"+kind+"/"+name+type+"View.prefab","Assets/Prefabs/Ui/Reinforcement/"+name+"ReinforcementView.prefab"})InspectPrefab(path);
        }
        InspectPrefab("Assets/Prefabs/Models/Wrecks/ImperialIAdvancedWreckView.prefab");result["prefabs"]=prefabs;
        var shipRoot=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialIAdvancedShipView.prefab");
        var health=new SerializedObject(Component(shipRoot,"HealthComponent"));var targets=health.FindProperty("<ShipUnits>k__BackingField");var weapons=shipRoot.GetComponentsInChildren<WeaponHardPoint>(true);
        var box=shipRoot.GetComponents<BoxCollider>().Single();var hangar=new SerializedObject(Component(shipRoot,"HangarComponent"));var launch=(Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        result["ship"]=new JObject{{"health_count",targets.arraySize},{"weapon_count",weapons.Length},{"all_hardpoint_ids",new JArray(shipRoot.GetComponentsInChildren<HardPoint>(true).Select(h=>h.Id).OrderBy(i=>i))},{"weapon_counts",JObject.FromObject(weapons.GroupBy(w=>(int)w.WeaponType).ToDictionary(g=>g.Key.ToString(),g=>g.Count()))},{"collider_size",box.size.ToString()},{"hangar_launch",launch.position.ToString()},{"hangar_outside_collider",!new Bounds(box.center,box.size).Contains(launch.position)},{"engines_behind_center",shipRoot.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="EngineLeft"||t.name=="EngineRight").All(t=>t.position.z<0)}};
        result["fighters"]=new JArray(names.Skip(1).Select(name=>{var root=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Squadrons/"+name+"SquadronView.prefab");return new JObject{{"name",name},{"members",root.GetComponentsInChildren<FighterView>(true).Length},{"weapons",root.GetComponentsInChildren<WeaponHardPoint>(true).Length},{"unique_weapon_ids",root.GetComponentsInChildren<WeaponHardPoint>(true).Select(w=>w.Id).Distinct().Count()}};}));
        var registrations=new JArray();
        for(int i=0;i<4;i++)
        {
            bool ship=i==0;string name=names[i],type=ship?"Ship":"Squadron";int id=ship?201:202+i;
            foreach(string path in new[]{"Assets/Prefabs/Models/"+(ship?"Ships":"Squadrons")+"/"+name+type+"View.prefab","Assets/Settings/Data/"+type+"/"+name+type+"Data.asset"})
            {
                string guid=AssetDatabase.AssetPathToGUID(path);var addressable=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);registrations.Add(new JObject{{"path",path},{"guid",guid},{"address",addressable.address},{"group",addressable.parentGroup.Name}});
            }
            var faction=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset"));var rows=faction.FindProperty((ship?"ships":"squadrons")+".keyValue");var row=Enumerable.Range(0,rows.arraySize).Select(n=>rows.GetArrayElementAtIndex(n)).Single(p=>p.FindPropertyRelative("key").intValue==id).FindPropertyRelative("value");registrations.Add(new JObject{{"id",id},{"name",row.FindPropertyRelative("<Name>k__BackingField").stringValue},{"icon",AssetDatabase.GetAssetPath(row.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue)}});
        }
        result["registrations"]=registrations;File.WriteAllText("Temp/ImperialIAdvancedImport/UnityInspection.json",result.ToString(Formatting.Indented));
        return "Inspected 16 imported models, 13 saved prefabs, hardpoint/fighter lists and Empire/Addressables registrations. Report: Temp/ImperialIAdvancedImport/UnityInspection.json";
        void InspectPrefab(string path)
        {
            var root=PrefabUtility.LoadPrefabContents(path);try
            {
                int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));var broken=new List<string>();
                foreach(var component in root.GetComponentsInChildren<Component>(true).Where(c=>c!=null)){var so=new SerializedObject(component);var p=so.GetIterator();while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue==null && p.objectReferenceInstanceIDValue!=0)broken.Add(component.name+"/"+p.propertyPath);}
                var renderers=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).ToArray();var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
                prefabs.Add(new JObject{{"path",path},{"missing_scripts",missing},{"broken_references",new JArray(broken)},{"root_scale",root.transform.localScale.ToString()},{"size",bounds.size.ToString()},{"visible_meshes",renderers.Length},{"embedded_materials",new JArray(renderers.SelectMany(r=>r.sharedMaterials).Where(m=>AssetDatabase.GetAssetPath(m).EndsWith(".fbx")).Select(m=>m.name).Distinct())}});
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
    }
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
}
