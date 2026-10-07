using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class InspectMC80Independence
{
    const string DIRECTORY = "Temp/MC80IndependenceImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/MC80Independence.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/MC80IndependenceShipData.asset";

    public static string Main()
    {
        var report = new JObject();
        var source = JObject.Parse(File.ReadAllText(DIRECTORY+"ConversionReport.json"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/MC80Independence/MC80Independence.fbx");
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
        foreach(var path in new[]{VISUAL,GAMEPLAY,"Assets/Prefabs/Ui/Reinforcement/MC80IndependenceReinforcementView.prefab","Assets/Prefabs/Models/Wrecks/MC80IndependenceWreckView.prefab"})
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
        var visual = ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="MC80Independence" && t.parent.name=="BodyPivot");
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
        var bayTargets=References(Component(ship,"HangarComponent"),"bayHardPoints").Cast<HardPoint>().ToArray();
        var bayLaunches=References(Component(ship,"HangarComponent"),"bayLaunchPoints").Cast<Transform>().ToArray();
        report["bayIds"] = new JArray(bayTargets.Select(h=>h.Id));
        report["bayClearances"] = new JArray(bayLaunches.Select(t=>(collider.center-collider.size*.5f).y-t.position.y));
        if(bayTargets.Length!=3 || bayTargets.Distinct().Count()!=3 || bayLaunches.Length!=3 || bayLaunches.Any(t=>t.position.y>collider.center.y-collider.size.y*.5f-7.9f)) throw new InvalidOperationException("Hangar dependencies or hull clearance differ from the approved configuration.");
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
        var registrationChecks=new JObject();
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/MC80IndependenceIcon.png");
        var ships=Load("Assets/Settings/Data/Ship/ShipsData.asset");
        var faction=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),305).FindPropertyRelative("value");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/MC80IndependenceReinforcementView.prefab");
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),26).FindPropertyRelative("value");
        var modifier=ability.FindPropertyRelative("settings.statModifier");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey=iconGuid+":"+iconId;
        registrationChecks["shipData"]=Row(ships.FindProperty("shipsData.keyValue"),305).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA);
        registrationChecks["factionIcon"]=faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon;
        registrationChecks["hudIcon"]=Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),305).FindPropertyRelative("value").objectReferenceValue==icon;
        var tooltipIcons=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        registrationChecks["tooltipIcon"]=Enumerable.Range(0,tooltipIcons.arraySize).Select(i=>tooltipIcons.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon;
        registrationChecks["placement"]=Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),305).FindPropertyRelative("value").objectReferenceValue==Component(preview,"UnitSpawnView");
        registrationChecks["ability"]=data.FindProperty("abilities").arraySize==1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==26 && ability.FindPropertyRelative("duration").floatValue==30 && ability.FindPropertyRelative("recoveryDelay").floatValue==60 && modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue==8 && modifier.FindPropertyRelative("speedMultiplier").floatValue==.8f && modifier.FindPropertyRelative("fireDelayMultiplier").floatValue==2;
        registrationChecks["stats"]=(float)report["hull"]==30000 && (float)report["shields"]==40000 && (float)report["speed"]==17;
        var mappings=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
        foreach(var path in new[]{GAMEPLAY,DATA})
        {
            string key=Path.GetFileNameWithoutExtension(path);
            registrationChecks[key]=Enumerable.Range(0,mappings.arraySize).Select(i=>mappings.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(path);
        }
        var sounds=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var profiles=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset").FindProperty("weapons");
        var weaponSounds=sounds.FindProperty("weapons");
        registrationChecks["weaponProfilesAndAudio"]=weapons.All(w=>Enumerable.Range(0,profiles.arraySize).Count(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==(int)w.WeaponType)==1 && Enumerable.Range(0,weaponSounds.arraySize).Count(i=>weaponSounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==(int)w.WeaponType)==1);
        var abilitySounds=sounds.FindProperty("abilities");
        registrationChecks["abilityAudio"]=Enumerable.Range(0,abilitySounds.arraySize).Count(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==26)==1;
        report["registrations"]=registrationChecks;
        if(registrationChecks.Properties().Any(p=>!(bool)p.Value)) throw new InvalidOperationException("A ship registration failed: "+registrationChecks);
        File.WriteAllText(DIRECTORY+"UnityInspection.json",report.ToString());
        if(targetable.Length!=20 || weapons.Length!=36 || all.Length!=42) throw new InvalidOperationException("Independence hardpoint counts differ from the request.");
        if(targetable.Count(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Engines)!=3 || targetable.Count(h=>h.HardPointType==EmpireAtWar.Components.Ship.Health.HardPointType.Hangar)!=3) throw new InvalidOperationException("Independence systems are incomplete.");
        if(all.Select(h=>h.Id).Distinct().Count()!=42) throw new InvalidOperationException("Hardpoint IDs must be unique.");
        if(boneError>.00001f || parentMismatches.Count!=0) throw new InvalidOperationException("Unity bone conversion differs from source.");
        if(meshes.Any(m=>(int)m["triangles"]!=(int)m["sourceTriangles"] || (int)m["uvCount"]!=(int)m["vertices"])) throw new InvalidOperationException("Mesh triangles or UVs differ from the conversion.");
        if(mounts.Any(m=>(float)m["muzzleError"]>.00001f)) throw new InvalidOperationException("Muzzle or system attachment mapping is incorrect.");
        if(prefabs.Any(p=>(int)p["missingScripts"]!=0 || ((JArray)p["brokenReferences"]).Count!=0)) throw new InvalidOperationException("Saved prefab references are broken.");
        return "Verified 20 targetable / 22 non-targetable hardpoints, 36 weapons, three engines/hangars, saved prefab references, geometry and Addressables. Evidence: "+DIRECTORY+"UnityInspection.json";
    }

    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static JArray Vector(Vector3 value)=>new JArray(value.x,value.y,value.z);
    static Bounds BoundsOf(GameObject root){var vertices=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var vertex in vertices)bounds.Encapsulate(vertex);return bounds;}
}
