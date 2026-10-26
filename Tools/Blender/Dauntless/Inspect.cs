using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Components.AttackComponent;

public static class InspectDauntless
{
    const string TASK="Temp/DauntlessImport/";
    const string VISUAL="Assets/Prefabs/Models/Ships/Rebellion/Dauntless.prefab";
    const string VIEW="Assets/Prefabs/Models/Ships/Rebellion/DauntlessShipView.prefab";
    const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/DauntlessReinforcementView.prefab";
    const string WRECK="Assets/Prefabs/Models/Wrecks/DauntlessWreckView.prefab";
    const string DATA="Assets/Settings/Data/Ship/Rebellion/DauntlessShipData.asset";
    static int _checks;

    public static string Main()
    {
        _checks=0;
        var source=JObject.Parse(File.ReadAllText(TASK+"Dauntless/ConversionReport.json"));
        var model=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RebellionShips/Dauntless/Dauntless.fbx");
        var transforms=model.GetComponentsInChildren<Transform>(true);
        float error=0;int triangles=0;
        foreach(var bone in ((JObject)source["before"]["bones"]).Properties())
        {
            var transform=transforms.Single(t=>t.name==bone.Name && t.GetComponent<Renderer>()==null);
            var head=bone.Value["head"].Values<float>().ToArray();
            error=Mathf.Max(error,Vector3.Distance(transform.position,new Vector3(-head[0],head[2],-head[1])*.02f));
            Check(bone.Value["parent"].Type==JTokenType.Null || transform.parent.name==(string)bone.Value["parent"],"Bone hierarchy");
        }
        Check(error<.00001f,"Unity bone displacement");
        foreach(var renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            var mesh=renderer is SkinnedMeshRenderer ? ((SkinnedMeshRenderer)renderer).sharedMesh : renderer.GetComponent<MeshFilter>().sharedMesh;
            var record=source["before"]["meshes"][renderer.name.Split('.')[0]];
            Check(mesh.triangles.Length/3==(int)record["triangles"],"Triangle count");
            Check(mesh.uv.Length==mesh.vertexCount,"UV count");
            Check(renderer.sharedMaterials.Select(m=>m.name).SequenceEqual(record["materials"].Values<string>()),"Material slots");
            triangles+=mesh.triangles.Length/3;
        }
        var prefabs=new JArray();
        foreach(var path in new[]{VISUAL,VIEW,PREVIEW,WRECK})
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.transform.localScale==Vector3.one,"Root scale");
            foreach(var transform in root.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject)==0,"Missing script");
            foreach(var component in root.GetComponentsInChildren<Component>(true))
            {
                var property=new SerializedObject(component).GetIterator();
                while(property.Next(true))if(property.propertyType==SerializedPropertyType.ObjectReference)
                    Check(property.objectReferenceValue!=null || property.objectReferenceEntityIdValue.Equals(default(EntityId)),"Broken reference "+property.propertyPath);
            }
            Check(!root.GetComponentsInChildren<MeshRenderer>(true).Any(r=>new[]{"shadowmesh","Collisionmesh_1","Collisionmesh_2","Shield"}.Contains(r.name)),"Source helpers removed");
            prefabs.Add(new JObject{["path"]=path,["renderers"]=root.GetComponentsInChildren<MeshRenderer>(true).Count(r=>r.enabled)});
        }
        var ship=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var hp=References(Component(ship,"HealthComponent"),"<ShipUnits>k__BackingField").Cast<HardPoint>().ToArray();
        var weapons=References(Component(ship,"WeaponComponent"),"hardPoints").Cast<WeaponHardPoint>().ToArray();
        Check(hp.Length==23 && hp.Select(h=>h.Id).SequenceEqual(Enumerable.Range(0,23)),"Target IDs");
        Check(weapons.Length==18 && weapons.All(w=>hp.Contains(w)),"Weapon targeting");
        int[] profiles={2,5,11,14},counts={8,4,4,2};
        for(int i=0;i<4;i++)Check(weapons.Count(w=>(int)w.WeaponType==profiles[i])==counts[i],"Weapon composition");
        Check(hp.Count(h=>(int)h.HardPointType==1)==2 && hp.Count(h=>(int)h.HardPointType==2)==1 && hp.Count(h=>(int)h.HardPointType==4)==1 && hp.Count(h=>(int)h.HardPointType==10)==1,"Subsystems");
        var visual=ship.GetComponentsInChildren<Transform>(true).Single(t=>t.name=="DauntlessRig");
        foreach(var mapping in JArray.Parse(File.ReadAllText(TASK+"HardpointMapping.json")).Where(m=>!(bool)(m["derivedPosition"]??false)))
        {
            var bone=visual.GetComponentsInChildren<Transform>(true).Single(t=>t.name==(string)mapping["bone"] && t.GetComponent<Renderer>()==null);
            Check(Vector3.Distance(bone.position,hp[(int)mapping["id"]].transform.position)<.00001f,"Muzzle/socket mapping");
        }
        Check(References(Component(ship,"FogVisibilityComponent"),"hardPoints").SequenceEqual(hp),"Fog targeting");
        var data=Load(DATA);
        Check(data.FindProperty("<Hull>k__BackingField").floatValue==10000 && data.FindProperty("<Shields>k__BackingField").floatValue==5500 && data.FindProperty("<Speed>k__BackingField").floatValue==17,"Balance stats");
        var healthTypes=data.FindProperty("hardPointHealth");
        foreach(var point in hp)Check(Enumerable.Range(0,healthTypes.arraySize).Any(i=>healthTypes.GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue==(int)point.HardPointType),"Hardpoint health");
        var bays=data.FindProperty("hangarBays");Check(bays.arraySize==2,"Bays");
        for(int i=0;i<2;i++){var bay=bays.GetArrayElementAtIndex(i);Check(bay.FindPropertyRelative("squadronType").intValue==(i==0?300:3) && bay.FindPropertyRelative("reserve").intValue==2 && bay.FindPropertyRelative("maxActive").intValue==1,"Squadron capacity");}
        var hangar=new SerializedObject(Component(ship,"HangarComponent"));
        Check(hangar.FindProperty("hangarHardPoint").objectReferenceValue==hp.Single(h=>(int)h.HardPointType==4) && hangar.FindProperty("isDestroyable").boolValue,"Hangar shutdown");
        var collider=ship.GetComponent<BoxCollider>();var launch=(Transform)hangar.FindProperty("launchPoint").objectReferenceValue;
        Check(collider.bounds.min.y-launch.position.y>=7.99f,"Hangar clearance");
        var spawn=Component(AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW),"UnitSpawnView");
        var hologram=new SerializedObject(spawn).FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn,"meshRenderers").Cast<MeshRenderer>().All(r=>r.sharedMaterials.All(m=>m==hologram)),"Hologram slots");
        var faction=Row(Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset").FindProperty("ships.keyValue"),310).FindPropertyRelative("value");
        Check(faction.FindPropertyRelative("<Price>k__BackingField").intValue==5600 && faction.FindPropertyRelative("<BuildTime>k__BackingField").intValue==60,"Roster stats");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/DauntlessIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string guid,out long localId);string iconKey=guid+":"+localId;
        Check(faction.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && faction.FindPropertyRelative("iconKey").stringValue==iconKey,"Roster icon");
        Check(Row(Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset").FindProperty("shipIconWrapper.keyValue"),310).FindPropertyRelative("value").objectReferenceValue==icon,"HUD icon");
        var icons=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset").FindProperty("icons");
        Check(Enumerable.Range(0,icons.arraySize).Any(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey && icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue==icon),"Tooltip icon");
        Check(Row(Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").FindProperty("spawnShipWrapper.keyValue"),310).FindPropertyRelative("value").objectReferenceValue==spawn,"Placement registration");
        Check(Row(Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset").FindProperty("shipsData.keyValue"),310).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Data registration");
        foreach(var path in new[]{VIEW,DATA})
        {
            var address=Path.GetFileNameWithoutExtension(path);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address==address,"Addressables");
            var mappings=Load("Assets/Settings/AssetMappingData.asset").FindProperty("assetMappings.keyValue");
            Check(Enumerable.Range(0,mappings.arraySize).Any(i=>mappings.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==address && mappings.GetArrayElementAtIndex(i).FindPropertyRelative("value.m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(path)),"Asset map");
        }
        var ability=Row(Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").FindProperty("definitions.keyValue"),39).FindPropertyRelative("value");
        Check(ability.FindPropertyRelative("settings").managedReferenceValue is SensorJammingSettings && ability.FindPropertyRelative("duration").floatValue==8 && ability.FindPropertyRelative("recoveryDelay").floatValue==40,"Sensor Jamming");
        Check(data.FindProperty("abilities").GetArrayElementAtIndex(0).intValue==39,"Ability loadout");
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var weaponData=(WeaponsData)Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset").targetObject;
        foreach(var profile in profiles){var definition=weaponData.GetDefinition((WeaponType)profile);Check(((ShipSfxData)audio.targetObject).GetWeapon((WeaponType)profile,definition.Family)!=null,"Weapon/family audio");}
        Check(((ShipSfxData)audio.targetObject).GetAbility((EmpireAtWar.Services.ShipAbilities.ShipAbilityId)39)!=null,"Ability audio");
        var helpers=EmpireAtWar.Editor.Rendering.UnitHelperMeshStripper.FindHelpers(new[]{VISUAL,VIEW,PREVIEW,WRECK},new string[0]);
        Check(helpers.All(h=>h.BlockedBy!=null),"Strippable helpers");
        var report=new JObject{["checks"]=_checks,["meshCount"]=13,["triangleCount"]=triangles,["boneErrorUnits"]=error,["prefabs"]=prefabs,["targetableHardpoints"]=23,["weaponCounts"]=new JArray(counts),["shipId"]=310,["abilityId"]=39};
        File.WriteAllText(TASK+"UnityInspection.json",report.ToString());
        return report.ToString();
    }

    static void Check(bool condition,string label){if(!condition)throw new InvalidOperationException(label);_checks++;}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name==name);
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Row(SerializedProperty array,int key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==key);
    static UnityEngine.Object[] References(MonoBehaviour component,string field){var array=new SerializedObject(component).FindProperty(field);return Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();}
}
