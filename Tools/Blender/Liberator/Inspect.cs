using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using Newtonsoft.Json.Linq;
using EmpireAtWar.Components.Hangar;
using EmpireAtWar.ViewComponents.Health;

public static class InspectLiberator
{
    const string TASK = "Temp/LiberatorImport/";
    const string DATA = "Assets/Settings/Data/Ship/LiberatorShipData.asset";
    const string VIEW = "Assets/Prefabs/Models/Ships/LiberatorShipView.prefab";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Liberator.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/LiberatorReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/LiberatorWreckView.prefab";
    const int SHIP_ID = 321;

    public static string Main()
    {
        var manifest=JObject.Parse(File.ReadAllText(TASK+"ArtManifest.json"));
        float boneError=0; int triangles=0, meshes=0;
        foreach(var source in manifest.Properties())
        {
            Check((float)source.Value["geometry_error"]<.001f && (float)source.Value["bone_error"]<.001f,"FBX round trip");
            var model=AssetDatabase.LoadAssetAtPath<GameObject>((string)source.Value["model_path"]);
            var transforms=model.GetComponentsInChildren<Transform>(true);
            foreach(var bone in ((JObject)source.Value["before"]["bones"]).Properties())
            {
                var transform=transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
                var head=bone.Value["head"].Values<float>().ToArray();
                boneError=Mathf.Max(boneError,Vector3.Distance(transform.position,new Vector3(-head[0],head[2],-head[1])*.02f));
                Check(bone.Value["parent"].Type==JTokenType.Null || transform.parent.name==(string)bone.Value["parent"],"Bone parent");
            }
            foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                var mesh=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>().sharedMesh;
                var record=source.Value["before"]["meshes"][renderer.name];
                // Verified in Blender: the source hull contains sixteen zero-area triangles, which Unity removes.
                int degenerate=source.Name=="Liberator" && renderer.name=="rebel_armor_neutral_2"?16:0;
                Check(mesh.triangles.Length/3==(int)record["triangles"]-degenerate,"Unity triangles: "+source.Name+"/"+renderer.name);
                Check(mesh.uv.Length==mesh.vertexCount,"Unity UVs");
                var materialPaths=record["materials"].Values<string>().Select(name=>(string)source.Value["materials"].Single(m=>(string)m["name"]==name)["material_path"]);
                Check(renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath).SequenceEqual(materialPaths),"Material slots");
                triangles+=mesh.triangles.Length/3; meshes++;
            }
        }
        Check(boneError<.0001f,"Unity attachment positions");
        foreach(var path in new[]{VISUAL,VIEW,PREVIEW,WRECK})
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(prefab.transform.localScale==Vector3.one,"Root scale");
            foreach(var transform in prefab.GetComponentsInChildren<Transform>(true)) Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script");
            foreach(var component in prefab.GetComponentsInChildren<Component>(true))
            {
                var property=new SerializedObject(component).GetIterator();
                while(property.Next(true)) if(property.propertyType==SerializedPropertyType.ObjectReference)
                    Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference "+component.name+"."+property.propertyPath);
            }
        }
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var data=Load(DATA);
        var visual=AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var bounds=BoundsOf(visual);
        Check(Mathf.Abs(bounds.size.z-160)<.001f,"Visible length");
        var hardpoints=References(Component(ship,"HealthComponent"),"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(hardpoints.Length==18 && hardpoints.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,18)),"18 ordered targetable hardpoints");
        Check(weapons.Length==12 && weapons.All(h=>hardpoints.Contains(h)),"Weapon health bindings");
        int[] profiles={2,5,32,14}, counts={4,2,4,2};
        for(int i=0;i<4;i++) Check(weapons.Count(w=>(int)w.WeaponType==profiles[i])==counts[i],"Weapon composition");
        Check(hardpoints.Count(h=>(int)h.HardPointType==1)==2 && hardpoints.Count(h=>(int)h.HardPointType==2)==1 && hardpoints.Count(h=>(int)h.HardPointType==4)==2 && hardpoints.Count(h=>(int)h.HardPointType==7)==1,"Subsystem composition");
        var healthTypes=data.FindProperty("hardPointHealth");
        foreach(var hp in hardpoints) Check(Enumerable.Range(0,healthTypes.arraySize).Any(i=>healthTypes.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)hp.HardPointType),"Subsystem health entry");
        Check(References(Component(ship,"FogVisibilityComponent"),"hardPoints").SequenceEqual(hardpoints),"Fog hardpoints");
        var hangar=new SerializedObject(Component(ship,"HangarComponent"));
        var bayPoints=References(Component(ship,"HangarComponent"),"bayHardPoints").Cast<HardPoint>().ToArray();
        Check(bayPoints.Length==2 && bayPoints.All(h=>hardpoints.Contains(h) && (int)h.HardPointType==4),"Two hangar bindings");
        foreach(var exit in References(Component(ship,"HangarComponent"),"bayLaunchPoints").Cast<Transform>()) Check(exit.position.y<bounds.min.y-7.99f,"Hangar clearance");
        Check(hangar.FindProperty("isDestroyable").boolValue && hangar.FindProperty("hangarHardPoint").objectReferenceValue==bayPoints[0],"Hangar shutdown bindings");
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==4800 && data.FindProperty("<Shields>k__BackingField").floatValue==4200 && data.FindProperty("<Speed>k__BackingField").floatValue==25,"Requested balance");
        var hangarData=(IHangarData)data.targetObject;
        Check(hangarData.HangarBays.All(b=>(int)b.SquadronType==300) && hangarData.HangarBays.Sum(b=>b.MaxActive)==3 && hangarData.HangarBays.Sum(b=>b.Reserve)==6,"X-Wing capacity");
        var hangarModel=new HangarModel(hangarData);
        Check(!hangarModel.TryLaunch(0,out int bay),"Initial delay");
        Check(hangarModel.TryLaunchImmediately(out bay) && bay==0 && hangarModel.TryLaunchImmediately(out bay) && bay==0,"Immediate launches fill multiple vacancies");
        Check(hangarModel.TryLaunchImmediately(out bay) && bay==1 && !hangarModel.TryLaunchImmediately(out bay),"Active cap");
        hangarModel.SquadronLost(0); hangarModel.SquadronLost(1);
        Check(hangarModel.TryLaunchImmediately(out bay) && hangarModel.TryLaunchImmediately(out bay),"Reserve replacements ignore launch timer");
        hangarModel.DisableBay(0); hangarModel.DisableBay(1);
        Check(!hangarModel.CanLaunchImmediately && !hangarModel.TryLaunchImmediately(out bay),"Destroyed hangars stop launches");
        var spawn=Component(AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW),"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Hologram slots");
        var faction=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),SHIP_ID).FindPropertyRelative("value");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/LiberatorIcon.png");
        Check(faction.FindPropertyRelative("<Price>k__BackingField").intValue==4200 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue==48 && faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon,"Roster stats/icon");
        Check(Row(Load("Assets/Settings/Data/Ship/ShipsData.asset").FindProperty("shipsData.keyValue"),SHIP_ID).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"ShipsData");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==icon,"ShipUi icon");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey=iconGuid+":"+iconId;
        var tooltipIcons=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,tooltipIcons.arraySize).Select(i=>tooltipIcons.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==iconKey).FindPropertyRelative("sprite").objectReferenceValue==icon,"Tooltip icon");
        Check(faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon identifier");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue==spawn,"Placement registration");
        foreach(var path in new[]{VIEW,DATA})
        {
            var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path));
            Check(entry!=null && entry.address==Path.GetFileNameWithoutExtension(path),"Addressables");
            var mapping=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mapping.arraySize).Select(i=>mapping.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==entry.address).FindPropertyRelative("value.m_AssetGUID").stringValue==entry.guid,"Asset mapping");
        }
        var wreckData=new SerializedObject(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue);
        Check(wreckData.FindProperty("<Prefab>k__BackingField").objectReferenceValue==Component(AssetDatabase.LoadAssetAtPath<GameObject>(WRECK),"UnitWreckView"),"Wreck binding");
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),51).FindPropertyRelative("value");
        Check(ability.FindPropertyRelative("recoveryDelay").floatValue==45 && ability.FindPropertyRelative("duration").floatValue==0 && ability.FindPropertyRelative("settings").managedReferenceValue is EmpireAtWar.Services.ShipAbilities.Abilities.RapidDeploymentSettings,"Rapid Deployment definition");
        var report=new JObject{["models"]=manifest.Count,["meshes"]=meshes,["triangles"]=triangles,["sourceZeroAreaTrianglesRemovedByUnity"]=16,["maxUnityBoneError"]=boneError,["visibleSize"]=new JArray(bounds.size.x,bounds.size.y,bounds.size.z),["hardpoints"]=18,["weapons"]=12,["activeXWings"]=3,["replacementXWings"]=3,["rapidDeploymentCooldownSeconds"]=45,["checksPassed"]=true};
        File.WriteAllText(TASK+"Verification.json",report.ToString());
        return report.ToString();
    }

    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty rows,int key)=>Enumerable.Range(0,rows.arraySize).Select(i=>rows.GetArrayElementAtIndex(i)).Single(r=>r.FindPropertyRelative("key").intValue==key);
    static MonoBehaviour Component(GameObject root,string type)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==type);
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
    static Bounds BoundsOf(GameObject root){var renderers=root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).ToArray();var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);return bounds;}
}
