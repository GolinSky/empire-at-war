using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;

public static class InspectCorellianBattlecruiser
{
    const string NAME = "CorellianBattlecruiser";
    const string TASK = "Temp/CorellianBattlecruiserImport/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/CorellianBattlecruiser.prefab";
    const string GAMEPLAY = "Assets/Prefabs/Models/Ships/CorellianBattlecruiserShipView.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/CorellianBattlecruiserReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/CorellianBattlecruiserWreckView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/CorellianBattlecruiserShipData.asset";

    public static string Main()
    {
        var report=new JObject();
        var conversion=JObject.Parse(File.ReadAllText(TASK+"ConversionReport.json"));
        float boneError=0; int triangles=0,meshCount=0;
        foreach(var source in ((JObject)conversion["models"]).Properties())
        {
            var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/"+NAME+"/"+source.Name+".fbx");
            var transforms=model.GetComponentsInChildren<Transform>(true);
            foreach(var bone in ((JObject)source.Value["before"]["bones"]).Properties())
            {
                var transform=transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
                var head=bone.Value["head"].Values<float>().ToArray();
                boneError=Mathf.Max(boneError,Vector3.Distance(transform.position,new Vector3(-head[0],head[2],-head[1])*.02f));
                string parent=(string)bone.Value["parent"];
                Check(parent==null || transform.parent.name==parent,"Bone hierarchy");
            }
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var mesh=renderer is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)renderer).sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
                string name=renderer.name.Split('.')[0];
                Check(mesh.triangles.Length/3==(int)source.Value["before"]["meshes"][name]["triangles"],"Unity triangle count");
                Check(mesh.uv.Length==mesh.vertexCount,"Unity UVs");
                var slots=conversion["mesh_materials"][source.Name]["meshes"].Single(m=>(string)m["name"]==name)["materials"].Select(m=>(string)m["materialName"]).ToArray();
                Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(slots),"Source material assignments");
                triangles+=mesh.triangles.Length/3; meshCount++;
            }
        }
        Check(boneError<.00001f,"Unity attachment error");
        report["unityGeometry"]=new JObject{["models"]=5,["meshes"]=meshCount,["triangles"]=triangles,["boneErrorUnits"]=boneError};
        var prefabs=new JArray();
        foreach(string path in new[]{VISUAL,GAMEPLAY,PREVIEW,WRECK})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.transform.localScale==Vector3.one,"Prefab root scale");
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script");
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                var property=new SerializedObject(component).GetIterator();
                while(property.Next(true))
                    if(property.propertyType==SerializedPropertyType.ObjectReference)Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference: "+component.name+"."+property.propertyPath);
            }
            var geometry=path==GAMEPLAY ? root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME && t.parent.name=="BodyPivot").gameObject : root;
            var bounds=BoundsOf(geometry);
            Check(Mathf.Abs(bounds.size.z-140)<.001f,"Prefab hull length");
            Check(!geometry.GetComponentsInChildren<MeshRenderer>(true).Any(r=>new[]{"Collision","Object03","Plane07","Plane08"}.Contains(r.name)),"Authored hidden helpers");
            prefabs.Add(new JObject{["path"]=path,["size"]=Vector(bounds.size),["visibleMeshes"]=geometry.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled)});
        }
        report["prefabs"]=prefabs;
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>(GAMEPLAY);
        var health=Component(ship,"HealthComponent");
        var hardpoints=References(health,"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(hardpoints.Length==20 && hardpoints.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,20)),"Targetable hardpoints and ordered IDs");
        Check(weapons.Length==17 && weapons.All(w=>hardpoints.Contains(w)),"Weapon health bindings");
        Check(weapons.Count(w=>(int)w.WeaponType==64)==4 && weapons.Count(w=>(int)w.WeaponType==4)==8 && weapons.Count(w=>(int)w.WeaponType==8)==5,"Requested weapon counts");
        Check(hardpoints.Count(h=>(int)h.HardPointType==1)==1 && hardpoints.Count(h=>(int)h.HardPointType==2)==1 && hardpoints.Count(h=>(int)h.HardPointType==4)==1,"Requested system counts");
        Check(References(Component(ship,"FogVisibilityComponent"),"hardPoints").SequenceEqual(hardpoints),"Fog hardpoints");
        var hull=ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name==NAME+"Rig");
        foreach(var mapping in JArray.Parse(File.ReadAllText(TASK+"HardpointMapping.json")).Where(m=>m["derivedHangar"]==null))
        {
            string bone=(string)mapping["muzzle"] ?? (string)mapping["bone"];
            var transform=hull.GetComponentsInChildren<Transform>(true).Single(t=>string.Equals(t.name,bone,StringComparison.OrdinalIgnoreCase) && t.GetComponent<MeshFilter>()==null);
            Check(Vector3.Distance(transform.position,hardpoints[(int)mapping["id"]].transform.position)<.00001f,"Hardpoint attachment");
        }
        var data=Load(DATA); var healthTypes=data.FindProperty("hardPointHealth");
        foreach(var hardpoint in hardpoints)Check(Enumerable.Range(0,healthTypes.arraySize).Any(i=>healthTypes.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)hardpoint.HardPointType),"Hardpoint health type");
        var hangar=new SerializedObject(Component(ship,"HangarComponent"));
        Check(hangar.FindProperty("hangarHardPoint").objectReferenceValue==hardpoints.Single(h=>(int)h.HardPointType==4),"Single hangar target");
        Check(hangar.FindProperty("isDestroyable").boolValue && hangar.FindProperty("bayHardPoints").arraySize==0 && hangar.FindProperty("bayLaunchPoints").arraySize==0,"Shared hangar shutdown");
        var launch=(Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        var collider=ship.GetComponent<BoxCollider>(); float clearance=collider.center.y-collider.size.y/2-launch.position.y;
        Check(clearance>=7.99f,"Launch clearance");
        var bays=data.FindProperty("hangarBays"); int[] squadrons={300,1,3};
        Check(bays.arraySize==3,"Squadron complement");
        for(int i=0;i<3;i++){var bay=bays.GetArrayElementAtIndex(i);Check(bay.FindPropertyRelative("squadronType").intValue==squadrons[i] && bay.FindPropertyRelative("reserve").intValue==6 && bay.FindPropertyRelative("maxActive").intValue==1,"Squadron capacity");}
        Check(Mathf.Abs(data.FindProperty("<ShieldRegenerateValue>k__BackingField").floatValue-53.333332f)<.001f && data.FindProperty("<ShieldRegenerateDelay>k__BackingField").floatValue==3,"Frigate shield regeneration");
        var spawn=Component(AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW),"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Placement hologram slots");
        report["gameplay"]=new JObject{["targetableHardpoints"]=20,["weaponCounts"]=new JObject{["rockets"]=4,["mediumTurbolasers"]=8,["laserCannons"]=5},["squadronTypes"]=new JArray(squadrons),["launchClearance"]=clearance,["shieldRegenerationPerSecond"]=53.333332f/3};
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/"+NAME+"Icon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId); string iconKey=iconGuid+":"+iconId;
        var faction=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),306).FindPropertyRelative("value");
        Check(faction.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue==2 && faction.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue==3,"Roster level/population");
        Check(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),306).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltip.arraySize).Select(i=>tooltip.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),306).FindPropertyRelative("value").objectReferenceValue==spawn,"Placement registration");
        Check(Row(Load("Assets/Settings/Data/Ship/ShipsData.asset").FindProperty("shipsData.keyValue"),306).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship-data registration");
        foreach(string path in new[]{GAMEPLAY,DATA})
        {
            string key=Path.GetFileNameWithoutExtension(path),guid=AssetDatabase.AssetPathToGUID(path);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable address");
            var mappings=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mappings.arraySize).Select(i=>mappings.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==key).FindPropertyRelative("value.m_AssetGUID").stringValue==guid,"Asset mapping");
        }
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),34).FindPropertyRelative("value");
        Check(data.FindProperty("abilities").arraySize==1 && data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==34,"Ability loadout");
        Check(ability.FindPropertyRelative("displayName").stringValue=="Power to Shields" && ability.FindPropertyRelative("settings.statModifier.shieldRegenMultiplier").floatValue==2 && ability.FindPropertyRelative("duration").floatValue==15,"Power to Shields definition");
        var profiles=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset").FindProperty("weapons");
        var rocket=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("weaponType").intValue==64);
        Check(rocket.FindPropertyRelative("shotsPerSalvo").intValue==2 && rocket.FindPropertyRelative("shotInterval").floatValue==1,"Heavy two-burst rocket profile");
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(int type in new[]{64,4,8})Check(Enumerable.Range(0,audio.FindProperty("weapons").arraySize).Any(i=>audio.FindProperty("weapons").GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==type),"Weapon audio");
        Check(Enumerable.Range(0,audio.FindProperty("abilities").arraySize).Any(i=>audio.FindProperty("abilities").GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==34),"Ability audio");
        report["registrationsVerified"]=true;
        File.WriteAllText(TASK+"UnityInspection.json",report.ToString());
        return "Verified five models/UVs/materials/bones; four saved prefabs; 20 targets/17 weapons; single hangar and three squadron types; Rebellion level 2/population 3; ability, audio, icons, placement and Addressables.";
    }

    static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static JArray Vector(Vector3 value)=>new JArray(value.x,value.y,value.z);
    static Bounds BoundsOf(GameObject root){var points=root.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.enabled).SelectMany(r=>r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(v=>root.transform.InverseTransformPoint(r.transform.TransformPoint(v)))).ToArray();var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);return bounds;}
}
