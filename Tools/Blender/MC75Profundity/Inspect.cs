using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class InspectMC75
{
    const string DIRECTORY = "Temp/MC75ProfundityImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/MC75Profundity.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/MC75ProfundityShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/MC75ProfundityShipData.asset";

    public static string Main()
    {
        var report = new JObject();
        var source = JObject.Parse(File.ReadAllText(DIRECTORY+"ConversionReport.json"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/MC75Profundity/MC75Profundity.fbx");
        var transforms = model.GetComponentsInChildren<Transform>(true);
        float boneError = 0;
        var parentMismatches = new JArray();
        foreach(var bone in ((JObject)source["before"]["bones"]).Properties())
        {
            var transform = transforms.Single(t=>t.name==bone.Name && t.GetComponent<MeshFilter>()==null);
            var head = bone.Value["head"].Values<float>().ToArray();
            var expected = new Vector3(-head[0],head[2],-head[1])*.02f;
            boneError = Mathf.Max(boneError,Vector3.Distance(transform.position,expected));
            string parent = (string)bone.Value["parent"];
            if(parent!=null && transform.parent.name!=parent)parentMismatches.Add(bone.Name);
        }
        report["boneCount"] = ((JObject)source["before"]["bones"]).Count;
        report["boneErrorUnits"] = boneError;
        report["boneParentMismatches"] = parentMismatches;
        var meshes = new JArray();
        foreach(var filter in model.GetComponentsInChildren<MeshFilter>(true))
        {
            meshes.Add(new JObject{["name"]=filter.name,["vertices"]=filter.sharedMesh.vertexCount,
                ["triangles"]=filter.sharedMesh.triangles.Length/3,["sourceTriangles"]=(int)source["before"]["meshes"][filter.name]["triangles"],
                ["uvCount"]=filter.sharedMesh.uv.Length});
        }
        report["meshes"] = meshes;
        var prefabs = new JArray();
        foreach(var path in new[]{VISUAL,GAMEPLAY,"Assets/Prefabs/Ui/Reinforcement/MC75ProfundityReinforcementView.prefab","Assets/Prefabs/Models/Wrecks/MC75ProfundityWreckView.prefab"})
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var missing = root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var references = new JArray();
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                var so = new SerializedObject(component);
                var property = so.GetIterator();
                while(property.Next(true))
                {
                    if(property.propertyType!=SerializedPropertyType.ObjectReference)continue;
                    if(property.objectReferenceValue==null && property.objectReferenceInstanceIDValue!=0)
                        references.Add(component.GetType().Name+"."+property.propertyPath);
                }
            }
            var bounds = BoundsOf(root);
            prefabs.Add(new JObject{["path"]=path,["missingScripts"]=missing,["brokenReferences"]=references,
                ["rootScale"]=Vector(root.transform.localScale),["boundsSize"]=Vector(bounds.size),
                ["visibleMeshes"]=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled)});
        }
        report["prefabs"] = prefabs;
        var ship = AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY);
        var health = Component(ship,"HealthComponent");
        var all = ship.GetComponentsInChildren<HardPoint>(true);
        var targetable = References(health,"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons = References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        var mounts = new JArray();
        var mapping = JArray.Parse(File.ReadAllText(DIRECTORY+"HardpointMapping.json"));
        var visual = ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="MC75Profundity" && t.parent.name=="BodyPivot");
        foreach(var record in mapping)
        {
            var hp = all.Single(p=>p.Id==(int)record["id"]);
            string boneName = (string)record["muzzle"] ?? (string)record["bone"];
            var bone = visual.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,boneName,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null);
            mounts.Add(new JObject{["id"]=hp.Id,["name"]=hp.name,["type"]=hp.HardPointType.ToString(),
                ["targetable"]=targetable.Contains(hp),["muzzleError"]=Vector3.Distance(hp.transform.position,bone.position)});
        }
        report["hardpoints"] = mounts;
        report["targetableCount"] = targetable.Length;
        report["weaponCounts"] = JObject.FromObject(weapons.GroupBy(w=>w.WeaponType.ToString()).ToDictionary(g=>g.Key,g=>g.Count()));
        report["healthIds"] = new JArray(targetable.Select(h=>h.Id));
        report["fogCount"] = References(Component(ship,"FogVisibilityComponent"),"hardPoints").Length;
        report["teamRendererCount"] = References(Component(ship,"TeamColorView"),"meshRenderers").Length;
        var hangar = new SerializedObject(Component(ship,"HangarComponent"));
        var launch = (Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        var collider = ship.GetComponent<BoxCollider>();
        report["launchLocalPosition"] = Vector(launch.localPosition);
        report["launchColliderClearance"] = (collider.center-collider.size*.5f).y-launch.position.y;
        var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DATA));
        report["hull"] = data.FindProperty("<Hull>k__BackingField").floatValue;
        report["shields"] = data.FindProperty("<Shields>k__BackingField").floatValue;
        report["speed"] = data.FindProperty("<Speed>k__BackingField").floatValue;
        var registrations = new JObject();
        foreach(var path in new[]{GAMEPLAY,DATA})
        {
            var entry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            registrations[path] = new JObject{["address"]=entry.address,["group"]=entry.parentGroup.Name};
        }
        report["addressables"] = registrations;
        report["dependencies"] = new JArray(AssetDatabase.GetDependencies(new[]{GAMEPLAY,DATA},true).Where(p=>p.Contains("HomeOne")));
        File.WriteAllText(DIRECTORY+"UnityInspection.json",report.ToString());
        return report.ToString();
    }

    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static JArray Vector(Vector3 value)=>new JArray(value.x,value.y,value.z);
    static Bounds BoundsOf(GameObject root){var vertices=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var vertex in vertices)bounds.Encapsulate(vertex);return bounds;}
}
