using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class VerifyBothanAssaultCruiser
{
    const string NAME = "BothanAssaultCruiser";
    const string TASK = "Temp/BothanAssaultCruiserImport/";
    const string DATA = "Assets/Settings/Data/Ship/Rebellion/BothanAssaultCruiserShipData.asset";
    const string VIEW = "Assets/Prefabs/Models/Ships/Rebellion/BothanAssaultCruiserShipView.prefab";

    public static string Main()
    {
        var conversion=JObject.Parse(File.ReadAllText(TASK+NAME+"/ConversionReport.json"));
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/"+NAME+"/"+NAME+".fbx");
        var transforms=model.GetComponentsInChildren<Transform>(true);
        float boneError=0; int triangles=0;
        foreach(var bone in ((JObject)conversion["before"]["bones"]).Properties())
        {
            var transform=transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
            var head=bone.Value["head"].Values<float>().ToArray();
            boneError=Mathf.Max(boneError,Vector3.Distance(transform.position,new Vector3(-head[0],head[2],-head[1])*.02f));
            string parent=(string)bone.Value["parent"];
            Check(parent==null || transform.parent.name==parent,"Bone hierarchy");
        }
        foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            var mesh=renderer.GetComponent<MeshFilter>().sharedMesh;
            var expected=conversion["before"]["meshes"][renderer.name];
            Check(mesh.triangles.Length/3==(int)expected["triangles"],"Triangle count: "+renderer.name);
            Check(mesh.uv.Length==mesh.vertexCount,"UVs: "+renderer.name);
            Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(expected["materials"].Values<string>().Select(n=>n+"_"+((string)conversion["materials"].Single(m=>(string)m["name"]==n)["base"] ?? "Helper"))),"Material remapping: "+renderer.name);
            triangles+=mesh.triangles.Length/3;
        }
        Check(boneError<.00001f,"Unity bone positions");
        var prefabReport=new JArray();
        string preview="Assets/Prefabs/Ui/Reinforcement/"+NAME+"ReinforcementView.prefab";
        foreach(string path in new[]{"Assets/Prefabs/Models/Ships/Rebellion/"+NAME+".prefab",VIEW,preview,"Assets/Prefabs/Models/Wrecks/"+NAME+"WreckView.prefab"})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.transform.localScale==Vector3.one,"Root scale");
            foreach(var transform in root.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script");
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                var property=new SerializedObject(component).GetIterator();
                while(property.Next(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference) Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference: "+property.propertyPath);
            }
            var visible=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled && r.name!="ShieldSurface").ToArray();
            Check(!root.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.name=="Shadow" || r.name=="Shield"),"Source helpers stripped");
            var bounds=BoundsOf(visible);
            Check(Mathf.Abs(bounds.size.z-160)<.001f,"Hull length");
            prefabReport.Add(new JObject{["path"]=path,["size"]=Vector(bounds.size),["visibleMeshes"]=visible.Length});
        }
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var health=Refs(Component(ship,"HealthComponent"),"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=Refs(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(health.Length==22 && health.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,22)),"22 ordered targetable hardpoints");
        Check(weapons.Length==16 && weapons.All(w=>health.Contains(w)),"Weapon health bindings");
        int[] profiles={3,4,32,48,14},counts={4,4,2,4,2};
        for(int i=0;i<profiles.Length;i++) Check(weapons.Count(w=>(int)w.WeaponType==profiles[i])==counts[i],"Weapon loadout");
        Check(health.Count(h=>(int)h.HardPointType==1)==2 && health.Count(h=>(int)h.HardPointType==2)==1 && health.Count(h=>(int)h.HardPointType==4)==2 && health.Count(h=>(int)h.HardPointType==7)==1,"Subsystem composition");
        Check(Refs(Component(ship,"FogVisibilityComponent"),"hardPoints").SequenceEqual(health),"Fog hardpoints");
        var rig=ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME+"Rig");
        foreach(var mapping in JArray.Parse(File.ReadAllText(TASK+"HardpointMapping.json")))
        {
            var bone=rig.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)mapping["bone"] && t.GetComponent<MeshFilter>()==null);
            var offset=mapping["offsetUnits"].Values<float>().ToArray();
            var hp=health[(int)mapping["id"]];
            Check(Vector3.Distance(bone.position+hp.transform.parent.TransformVector(new Vector3(offset[0],offset[1],offset[2])),hp.transform.position)<.00001f,"Derived mount mapping");
        }
        var data=Load(DATA);
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==7200 && data.FindProperty("<Shields>k__BackingField").floatValue==6500 && data.FindProperty("<Speed>k__BackingField").floatValue==22,"Balance stats");
        Check(data.FindProperty("<ShipClass>k__BackingField").intValue==8,"Cruiser damage class");
        foreach(var hp in health) Check(Enumerable.Range(0,data.FindProperty("hardPointHealth").arraySize).Any(i=>data.FindProperty("hardPointHealth").GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)hp.HardPointType),"Health type");
        var hangar=new SerializedObject(Component(ship,"HangarComponent"));
        var hangarTargets=Refs(Component(ship,"HangarComponent"),"bayHardPoints").Cast<HardPoint>().ToArray();
        var launches=Refs(Component(ship,"HangarComponent"),"bayLaunchPoints").Cast<Transform>().ToArray();
        Check(hangarTargets.Length==2 && launches.Length==2 && hangarTargets.All(h=>health.Contains(h) && (int)h.HardPointType==4),"Independent hangars");
        Check(hangar.FindProperty("hangarHardPoint").objectReferenceValue==hangarTargets[0] && hangar.FindProperty("launchPoint").objectReferenceValue==launches[0] && hangar.FindProperty("isDestroyable").boolValue,"Hangar dependencies");
        var collider=ship.GetComponent<BoxCollider>();
        Check(launches.All(l=>collider.center.y-collider.size.y/2-l.position.y>=7.99f),"Launch clearance");
        var bays=data.FindProperty("hangarBays"); int[] squadrons={300,3};
        Check(bays.arraySize==2,"Two squadron bays");
        for(int i=0;i<2;i++) {var bay=bays.GetArrayElementAtIndex(i);Check(bay.FindPropertyRelative("squadronType").intValue==squadrons[i] && bay.FindPropertyRelative("reserve").intValue==2 && bay.FindPropertyRelative("maxActive").intValue==1,"One active plus one replacement per type");}
        var spawn=Component(AssetDatabase.LoadAssetAtPath<GameObject>(preview),"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(Refs(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Hologram slots");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/"+NAME+"Icon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);string iconKey=iconGuid+":"+iconId;
        var roster=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),312).FindPropertyRelative("value");
        Check(roster.FindPropertyRelative("<Price>k__BackingField").intValue==5200 && roster.FindPropertyRelative("<BuildTime>k__BackingField").intValue==58,"Price/build time");
        Check(roster.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && roster.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),312).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Select(i=>tooltip.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),312).FindPropertyRelative("value").objectReferenceValue==spawn,"Placement registration");
        Check(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"),312).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship data registration");
        foreach(string path in new[]{VIEW,DATA})
        {
            string key=Path.GetFileNameWithoutExtension(path),guid=AssetDatabase.AssetPathToGUID(path);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable");
            var mappings=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mappings.arraySize).Select(i=>mappings.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key).FindPropertyRelative("value.m_AssetGUID").stringValue==guid,"Asset mapping");
        }
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),42).FindPropertyRelative("value");
        Check(ability.FindPropertyRelative("settings.fireDelayMultiplier").floatValue==.8f && ability.FindPropertyRelative("duration").floatValue==8 && ability.FindPropertyRelative("recoveryDelay").floatValue==40,"Coordinated Assault");
        var abilities=data.FindProperty("abilities");Check(abilities.arraySize==2 && abilities.GetArrayElementAtIndex(0).intValue==42 && abilities.GetArrayElementAtIndex(1).intValue==22,"Ability loadout");
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var weaponData=AssetDatabase.LoadAssetAtPath<EmpireAtWar.Components.AttackComponent.WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var soundData=(EmpireAtWar.Services.Audio.ShipSfxData)audio.targetObject;
        foreach(int profile in profiles)
        {
            var type=(EmpireAtWar.Components.AttackComponent.WeaponType)profile;
            Check(soundData.GetWeapon(type,weaponData.GetDefinition(type).Family)!=null,"Weapon family audio");
        }
        foreach(int abilityId in new[]{42,22}) Check(Enumerable.Range(0,audio.FindProperty("abilities").arraySize).Any(i=>audio.FindProperty("abilities").GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==abilityId),"Ability audio");
        var report=new JObject{["meshes"]=6,["triangles"]=triangles,["bones"]=((JObject)conversion["before"]["bones"]).Count,["boneErrorUnits"]=boneError,["targetableHardpoints"]=22,["weaponProfiles"]=new JArray(profiles),["weaponCounts"]=new JArray(counts),["prefabs"]=prefabReport,["registrationsVerified"]=true};
        File.WriteAllText(TASK+"UnityInspection.json",report.ToString());
        return report.ToString();
    }
    static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    static UnityEngine.Object[] Refs(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static JArray Vector(Vector3 value)=>new JArray(value.x,value.y,value.z);
    static Bounds BoundsOf(MeshRenderer[] renderers){var points=renderers.SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>r.transform.TransformPoint(v))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds;}
}
