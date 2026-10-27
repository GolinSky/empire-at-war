using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Combat;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Editor.Balance;
using EmpireAtWar.Editor.BattleSimulator;
using EmpireAtWar.ViewComponents.Health;

public static class VerifyPeleset
{
    const int SHIP_ID = 110;
    const string VIEW = "Assets/Prefabs/Models/Ships/Separatist/PelesetShipView.prefab";
    const string DATA = "Assets/Settings/Data/Ship/Separatist/PelesetShipData.asset";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Separatist/Peleset.prefab";
    const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/PelesetReinforcementView.prefab";
    const string WRECK = "Assets/Prefabs/Models/Wrecks/PelesetWreckView.prefab";
    public static string Main()
    {
        var view = AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var preview = AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        var wreck = AssetDatabase.LoadAssetAtPath<GameObject>(WRECK);
        foreach (var prefab in new[] {view, visual, preview, wreck})
        {
            CheckReferences(prefab);
            Check(prefab.transform.localScale == Vector3.one && prefab.transform.localPosition == Vector3.zero
                && prefab.transform.localRotation == Quaternion.identity, "Identity root: " + prefab.name);
        }
        var health = Config(view, "HealthComponent");
        var hardpoints = References(health.FindProperty("<ShipUnits>k__BackingField")).Cast<HardPoint>().ToArray();
        Check(hardpoints.Length == 15 && hardpoints.Select(p => p.Id).SequenceEqual(Enumerable.Range(0,15)), "15 targetable ordered hardpoints");
        var weapons = hardpoints.OfType<WeaponHardPoint>().ToArray();
        int[] expected = {48,5,5,97,97,5,5,32,32,93,93,5,5};
        Check(weapons.Select(p => (int)p.WeaponType).SequenceEqual(expected), "Exact weapon composition");
        Check(References(Config(view,"WeaponComponent").FindProperty("hardPoints")).SequenceEqual(weapons.Cast<UnityEngine.Object>()), "Weapon bindings");
        Check(hardpoints[13].HardPointType == HardPointType.Engines && hardpoints[14].HardPointType == HardPointType.ShieldGenerator, "Engine and shield types");
        var injected = References(Config(view,"EntityBindings").FindProperty("<Injectables>k__BackingField"));
        Check(injected.Length == 16 && hardpoints.All(injected.Contains) && injected.Contains(Component(view,"UnitRenderStateComponent")), "Complete hardpoint/render injection");
        var body = (Transform)Config(view,"ShipMoveComponent").FindProperty("bodyTransform").objectReferenceValue;
        Check(hardpoints.All(p => p.transform.IsChildOf(body)), "All mounts bank with hull");
        var geometry = body.Cast<Transform>().Single(t=>t.name=="Peleset");
        for (int i = 0; i < hardpoints.Length; i++)
        {
            var socket = geometry.GetComponentsInChildren<Transform>(true).Single(t=>t.name==hardpoints[i].name);
            Check(Vector3.Distance(hardpoints[i].transform.position,socket.position)<.0001f, "Source socket "+i);
            Check(new SerializedObject(hardpoints[i]).FindProperty("destroyedVfxAnchor").objectReferenceValue!=null,"VFX anchor "+i);
        }
        var data = AssetDatabase.LoadAssetAtPath<ShipData>(DATA);
        Check(data.Hull==7000 && data.Shields==3000 && data.ShieldRegenTier==ShieldRegenTier.Cruiser && data.ShieldRegenerateValue==90,"Hull/shields/cruiser regeneration");
        Check(data.ShipClass==ShipClass.Cruiser && data.Speed==19 && data.HangarBays.Count==0,"Cruiser movement/no unsupported hangar");
        Check(data.Abilities.SequenceEqual(new[] {EmpireAtWar.Services.ShipAbilities.ShipAbilityId.BoostShieldPower, EmpireAtWar.Services.ShipAbilities.ShipAbilityId.BoostWeaponPower}),"Shield and weapon power abilities");
        var matrix = AssetDatabase.LoadAssetAtPath<DamageMatrixData>("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
        var model = new HealthModel(data,matrix,new CombatModifiers());
        var models = hardpoints.Select(p=>new HardPointModel(p.HardPointType,p.Id,p.UnlockLevel,p.HealthOverride)).ToArray();
        model.InitializeHardPoints(models);
        float[] expectedHealth = Enumerable.Repeat(400f,11).Concat(new[]{500f,500f,800f,800f}).ToArray();
        Check(models.Select(p=>p.Health).SequenceEqual(expectedHealth),"Initialized HP: 11x400,2x500,2x800");
        var weaponData = AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        foreach (var mount in weapons)
        {
            Check(weaponData.GetDefinition(mount.WeaponType).Caliber==WeaponCaliber.Medium,"Medium caliber: "+mount.name);
            Check(weaponData.GetProfile(mount.WeaponType).Damage>0 && weaponData.GetProfile(mount.WeaponType).ShotPrefab!=null,"Resolvable profile: "+mount.name);
        }
        var ships = Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Check(Entry(ships.FindProperty("shipsData.keyValue"),SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(DATA),"ShipsData registration");
        var mapping = Load("Assets/Settings/AssetMappingData.asset");
        Check(Entry(mapping.FindProperty("assetMappings.keyValue"),"PelesetShipView").FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(VIEW),"Asset mapping");
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach(var path in new[]{VIEW,DATA,PREVIEW}) Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(path)).address==Path.GetFileNameWithoutExtension(path),"Address: "+path);
        Check(settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(PREVIEW)).parentGroup.Name=="SeparatistUnitViews","Faction preview group");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Check(Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue==AssetDatabase.AssetPathToGUID(PREVIEW),"Own Addressable preview");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/Separatist/PelesetIcon.png");
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset"); Check(Entry(ui.FindProperty("shipIconWrapper.keyValue"),SHIP_ID).objectReferenceValue==icon,"Battle icon");
        var faction=Load("Assets/Settings/Data/Factions/Separatist/SeparatistFaction.asset"); var unit=Entry(faction.FindProperty("ships.keyValue"),SHIP_ID);
        Check(unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue==icon && unit.FindPropertyRelative("matchups").objectReferenceValue!=null,"Faction icon/matchups");
        Check(unit.FindPropertyRelative("<Price>k__BackingField").intValue==6000 && unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue==60 && unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue==4 && unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue==4,"Source economy");
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons=tooltip.FindProperty("icons");
        Check(Enumerable.Range(0,icons.arraySize).Any(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==unit.FindPropertyRelative("iconKey").stringValue && icons.GetArrayElementAtIndex(i).FindPropertyRelative("sprite").objectReferenceValue==icon),"Tooltip sprite and local identifier");
        Check(Entry(Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset").FindProperty("ships.keyValue"),SHIP_ID)!=null,"Explicit ship audio profile");
        var spawn=Config(preview,"UnitSpawnView"); var hologram=spawn.FindProperty("hologramMaterial").objectReferenceValue;
        Check(References(spawn.FindProperty("meshRenderers")).Cast<MeshRenderer>().All(r=>r.sharedMaterials.Contains(hologram)),"Tintable hologram material in every preview slot");
        Check(data.Wreck!=null && wreck.GetComponentsInChildren<MeshRenderer>(true).All(r=>r.sharedMaterials.All(m=>m.shader.name=="EmpireAtWar/Ship Wreck")),"Own breaking wreck");
        var registry = BalanceInventory.BuildCombat();
        var balanceUnit = registry.Units.Single(u => u.Data == data);
        float[] simulatorHp;
        using (var factory = new SimUnitFactory(registry, new BalanceDraft()))
        {
            var definition = factory.Build(balanceUnit);
            var simulated = new SimUnit(definition, new BattleFleetEntry {UnitId = definition.Id}, 0, 0, factory.Matrix, new System.Random(1), true);
            simulatorHp = simulated.HardPoints.Select(p=>p.Health).ToArray();
            Check(simulatorHp.SequenceEqual(expectedHealth), "Simulator factory and unit preserve exact mount HP");
        }
        var report=new{simulatorHp,shipId=SHIP_ID,hardpoints=15,weapons=13,hull=data.Hull,shields=data.Shields,shieldRegen=data.ShieldRegenerateValue,hardpointHp=models.Select(p=>p.Health).ToArray(),weaponProfiles=expected.Distinct().Select(t=>new{type=t,caliber=weaponData.GetDefinition((WeaponType)t).Caliber.ToString(),name=weaponData.GetProfile((WeaponType)t).DisplayName}).ToArray(),passed=true};
        string json=JsonConvert.SerializeObject(report,Formatting.Indented);File.WriteAllText("Temp/PelesetImport/VerificationResult.json",json);return json;
    }
    static void CheckReferences(GameObject prefab)
    {
        Check(prefab!=null,"Prefab exists");
        foreach(var component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
        {
            Check(component!=null,"No missing script: "+prefab.name);
            var iterator=new SerializedObject(component).GetIterator();
            while(iterator.Next(true)) if(iterator.propertyType==SerializedPropertyType.ObjectReference)
                Check(iterator.objectReferenceEntityIdValue==UnityEngine.EntityId.None || iterator.objectReferenceValue!=null,"No stale reference: "+component.GetType().Name+"."+iterator.propertyPath);
        }
    }
    static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static MonoBehaviour Component(GameObject root,string name)=>root.GetComponentsInChildren<MonoBehaviour>(true).Single(m=>m.GetType().Name==name);
    static SerializedObject Config(GameObject root,string name)=>new SerializedObject(Component(root,name));
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static UnityEngine.Object[] References(SerializedProperty array)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
    static SerializedProperty Entry(SerializedProperty array,object key)=>Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).Single(r=>key is int n?r.FindPropertyRelative("key").intValue==n:r.FindPropertyRelative("key").stringValue==(string)key).FindPropertyRelative("value");
}
