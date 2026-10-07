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
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Services.ShipAbilities.Abilities;
using EmpireAtWar.Editor.Rendering;

public static class VerifyTector
{
    const string VIEW="Assets/Prefabs/Models/Ships/TectorShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/TectorShipData.asset";
    static int _checks;

    public static string Main()
    {
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var targets=Objects(Config(root,"HealthComponent").FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        var guns=Objects(Config(root,"WeaponComponent").FindProperty("hardPoints")).Cast<WeaponHardPoint>().ToArray();
        Check(targets.Length==16 && targets.Select(p=>p.Id).OrderBy(i=>i).SequenceEqual(Enumerable.Range(0,16)),"16 ordered targets");
        Check(guns.Length==25,"25 weapons");
        foreach(var expected in new[]{new{type=43,count=8},new{type=53,count=2},new{type=38,count=7},new{type=33,count=4},new{type=28,count=4}})
            Check(guns.Count(g=>(int)g.WeaponType==expected.type)==expected.count,"Weapon composition "+expected.type);
        Check(targets.OfType<WeaponHardPoint>().Count()==10 && targets.OfType<WeaponHardPoint>().All(p=>(int)p.WeaponType==43||(int)p.WeaponType==53),"Only the 10 requested batteries targetable");
        Check(targets.Count(p=>(int)p.HardPointType==2)==2 && targets.Count(p=>(int)p.HardPointType==1)==3 && targets.Count(p=>(int)p.HardPointType==7)==1,"Six requested systems");
        Check(targets.All(p=>p.GetComponents<SphereCollider>().Length==1),"Target colliders");
        Check(root.GetComponentsInChildren<HardPoint>(true).Select(p=>p.Id).Distinct().Count()==31,"Unique IDs");
        var data=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(DATA));
        Check(Number(data,"Hull")==22000 && Number(data,"Shields")==16000 && Number(data,"Speed")==22,"Requested stats");
        Check(data.FindProperty("hangarBays").arraySize==0 && !Config(root,"HangarComponent").FindProperty("isDestroyable").boolValue,"No hangar or complement");
        Check(Ints(data.FindProperty("abilities")).SequenceEqual(new[]{32,22}),"Weapon boost and tractor abilities");
        Check(AssetDatabase.GetAssetPath(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue)=="Assets/Settings/Data/Ship/Wreck/TectorWreckData.asset","Own wreck data");
        var healthTypes=Enumerable.Range(0,data.FindProperty("hardPointHealth").arraySize).Select(i=>data.FindProperty("hardPointHealth").GetArrayElementAtIndex(i).FindPropertyRelative("hardPointType").intValue).ToArray();
        Check(targets.All(p=>healthTypes.Contains((int)p.HardPointType)),"Health for every target type");
        Check(Entry("Assets/Settings/Data/Ship/ShipsData.asset","shipsData.keyValue",210).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"Ship registry");
        foreach(var path in new[]{VIEW,DATA})
        {
            var guid=AssetDatabase.AssetPathToGUID(path);var key=Path.GetFileNameWithoutExtension(path);
            Check(Entry("Assets/Settings/AssetMappingData.asset","assetMappings.keyValue",key).FindPropertyRelative("m_AssetGUID").stringValue==guid,"Mapping "+key);
            Check(AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid).address==key,"Addressable "+key);
        }
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/TectorIcon.png");
        Check(icon!=null,"Model-rendered sprite");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId);string iconKey=iconGuid+":"+localId;
        var unit=Entry("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset","ships.keyValue",210);
        Check(unit.FindPropertyRelative("<Name>k__BackingField").stringValue=="Tector Star Destroyer","Empire roster");
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && unit.FindPropertyRelative("iconKey").stringValue==iconKey,"Faction icon");
        Check(Entry("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","shipIconWrapper.keyValue",210).objectReferenceValue==icon,"HUD icon");
        var icons=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/Settings/Data/Tooltip/TooltipIconData.asset")).FindProperty("icons");
        Check(Enumerable.Range(0,icons.arraySize).Any(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey && icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue==icon),"Tooltip icon");
        var spawn=Entry("Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnShipWrapper.keyValue",210).objectReferenceValue;
        Check(AssetDatabase.GetAssetPath(spawn)=="Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab","Own placement mapping");
        var profiles=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Check(profiles.GetProfile((WeaponType)43).ShotsPerSalvo==2 && profiles.GetProfile((WeaponType)38).ShotsPerSalvo==3,"Requested burst counts");
        foreach(var gun in guns)Check(profiles.GetProfile(gun.WeaponType).ShotPrefab!=null,"Projectile "+gun.name);
        var catalog=AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var boost=catalog.Get((ShipAbilityId)32);var modifier=((BoostWeaponPowerSettings)boost.Settings).StatModifier;
        Check(boost.Duration==20 && boost.RecoveryDelay==60 && modifier.FireDelayMultiplier==.5f && modifier.SpeedMultiplier==.25f && modifier.ShieldRegenMultiplier==0 && modifier.DamageTakenMultiplier==1.5f,"Boost trades defense and speed for firepower");
        Check(((TractorBeamSettings)catalog.Get((ShipAbilityId)22).Settings).SpeedMultiplier<1,"Tractor movement restriction");
        var body=(Transform)Config(root,"ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        foreach(var point in root.GetComponentsInChildren<HardPoint>(true))Check(point.transform.IsChildOf(body),"Banked hardpoint "+point.name);
        var mappings=JArray.Parse(File.ReadAllText("Temp/TectorImport/HardpointMapping.json"));float mountError=0;
        foreach(var mapping in mappings)
        {
            var point=root.GetComponentsInChildren<HardPoint>(true).Single(p=>p.Id==(int)mapping["id"]);
            string[] pieces=((string)mapping["bone"]).Split('/');
            var search=pieces.Length==2?root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==pieces[0]):root.transform;
            var bone=search.GetComponentsInChildren<Transform>(true).Single(t=>t.name.Equals(pieces.Last(),StringComparison.OrdinalIgnoreCase) && t.GetComponents<HardPoint>().Length==0);
            mountError=Mathf.Max(mountError,Vector3.Distance(point.transform.position,bone.position));
        }
        Check(mountError<.001f,"Actual attachment positions");
        string[] paths={VIEW,"Assets/Prefabs/Models/Ships/Tector.prefab",AssetDatabase.GetAssetPath(spawn),"Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab"};
        foreach(var path in paths)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);Check(prefab!=null && prefab.transform.localScale==Vector3.one,"Identity prefab "+path);
            foreach(var t in prefab.GetComponentsInChildren<Transform>(true))Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0,"Script "+t.name);
            foreach(var renderer in prefab.GetComponentsInChildren<Renderer>(true))Check(renderer.sharedMaterials.All(m=>m!=null),"Materials "+renderer.name);
            foreach(var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var iterator=new SerializedObject(component).GetIterator();
                while(iterator.Next(true))if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                    Check(iterator.objectReferenceValue!=null || iterator.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)),"Reference "+component.name+"/"+iterator.propertyPath);
            }
        }
        var allRenderers=root.GetComponentsInChildren<MeshRenderer>(true);
        var team=Objects(Config(root,"TeamColorView").FindProperty("meshRenderers"));
        Check(allRenderers.All(team.Contains) && team.All(o=>o!=null),"Explicit team renderer ownership");
        var fog=Objects(Config(root,"FogVisibilityComponent").FindProperty("renderers"));
        Check(root.GetComponentsInChildren<Renderer>(true).Where(r=>r.enabled).All(fog.Contains),"Visible fog bindings");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GetAssetPath(spawn));
        var hologram=(Material)Config(preview,"UnitSpawnView").FindProperty("hologramMaterial").objectReferenceValue;
        Check(Objects(Config(preview,"UnitSpawnView").FindProperty("meshRenderers")).Cast<Renderer>().All(r=>r.sharedMaterials.Contains(hologram)),"Hologram tint bindings");
        Check(UnitHelperMeshStripper.FindHelpers(paths,UnitHelperMeshStripper.FindUnitPrefabPaths().Except(paths).ToArray()).All(h=>h.BlockedBy!=null),"No strippable helpers");
        var report=new{checks=_checks,targetable=targets.Length,weapons=guns.Length,hull=22000,shields=16000,speed=22,hangarBays=0,maximumMountError=mountError,automatedTestsRun=false,playModeRun=false};
        File.WriteAllText("Temp/TectorImport/Verification.json",JsonConvert.SerializeObject(report,Formatting.Indented));
        return JsonConvert.SerializeObject(report);
    }
    static void Check(bool value,string message){_checks++;if(!value)throw new InvalidOperationException(message);}
    static SerializedObject Config(GameObject root,string name)=>new SerializedObject(root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name));
    static UnityEngine.Object[] Objects(SerializedProperty p)=>Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static int[] Ints(SerializedProperty p)=>Enumerable.Range(0,p.arraySize).Select(i=>p.GetArrayElementAtIndex(i).intValue).ToArray();
    static float Number(SerializedObject data,string name)=>data.FindProperty("<"+name+">k__BackingField").floatValue;
    static SerializedProperty Entry(string path,string field,object key)
    {
        var rows=new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path)).FindProperty(field);
        return Enumerable.Range(0,rows.arraySize).Select(i=>rows.GetArrayElementAtIndex(i)).Single(r=>key is int n?r.FindPropertyRelative("key").intValue==n:r.FindPropertyRelative("key").stringValue==(string)key).FindPropertyRelative("value");
    }
}
