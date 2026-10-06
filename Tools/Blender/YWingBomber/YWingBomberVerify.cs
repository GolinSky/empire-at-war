using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Squadrons.Data;
using EmpireAtWar.ViewComponents.Squadrons;
using EmpireAtWar.ViewComponents.Health;
using EmpireAtWar.Services.ShipAbilities;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Squadrons.Health;
using EmpireAtWar.Components.Squadrons.Flight;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Components.FogOfWar;
using EmpireAtWar.Components.TeamColor;
public static class YWingBomberVerify
{
    public static object Main()
    {
        const string VIEW="Assets/Prefabs/Models/Squadrons/YWingBomberSquadronView.prefab";
        const string DATA="Assets/Settings/Data/Squadron/YWingBomberSquadronData.asset";
        var root=AssetDatabase.LoadAssetAtPath<GameObject>(VIEW);
        var fighters=root.GetComponentsInChildren<FighterView>(true); var guns=root.GetComponentsInChildren<WeaponHardPoint>(true);
        var data=AssetDatabase.LoadAssetAtPath<SquadronData>(DATA);
        var missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        var broken=new System.Collections.Generic.List<string>();
        foreach(var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            var serialized=new SerializedObject(component); var property=serialized.GetIterator();
            while(property.NextVisible(true))
                if(property.propertyType==SerializedPropertyType.ObjectReference&&property.objectReferenceInstanceIDValue!=0&&property.objectReferenceValue==null)
                    broken.Add(component.GetType().Name+"."+property.propertyPath);
        }
        var ability=AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.IonShot);
        var roster=AssetDatabase.LoadAssetAtPath<FactionCatalog>("Assets/Settings/Data/Factions/Shared/FactionCatalog.asset").GetSquadronFactionData(SquadronType.YWingBomber);
        var preview=AssetDatabase.LoadAssetAtPath<ReinforcementData>("Assets/Settings/Data/Reinforcement/ReinforcementData.asset").GetSpawnPrefab(SquadronType.YWingBomber);
        var weapons=AssetDatabase.LoadAssetAtPath<WeaponsData>("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var result=new {
            fighters=fighters.Length, guns=guns.GroupBy(g=>g.WeaponType).Select(g=>new{type=g.Key.ToString(),count=g.Count()}).ToArray(),
            uniqueIds=guns.Select(g=>g.Id).Distinct().Count(),missingScripts=missing,brokenReferences=broken,
            healthMembers=new SerializedObject(root.GetComponent<SquadronHealthComponent>()).FindProperty("fighters").arraySize,
            flightMembers=new SerializedObject(root.GetComponent<SquadronFlightComponent>()).FindProperty("fighters").arraySize,
            weaponBindings=new SerializedObject(root.GetComponentInChildren<WeaponComponent>(true)).FindProperty("hardPoints").arraySize,
            fogWeapons=new SerializedObject(root.GetComponent<FogVisibilityComponent>()).FindProperty("hardPoints").arraySize,
            teamRenderers=new SerializedObject(root.GetComponent<TeamColorView>()).FindProperty("meshRenderers").arraySize,
            hull=data.MemberHull,shields=data.MemberShields,totalHull=data.MemberHull*fighters.Length,totalShields=data.MemberShields*fighters.Length,
            data.CombatSpeed,data.CruiseSpeed,data.ShieldRegenerateValue,data.LaserShieldDamageMultiplier,
            ability=new{ability.DisplayName,ability.RecoveryDelay,ability.Range},
            roster=new{roster.Name,roster.Price,roster.AvailableLevel,icon=AssetDatabase.GetAssetPath(roster.Icon)},preview=AssetDatabase.GetAssetPath(preview),
            addressables=new[]{VIEW,DATA}.Select(p=>new{path=p,address=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(p)).address}).ToArray(),
            profiles=new[]{WeaponType.LightDualLaser,WeaponType.HeavyDualIonStunner,WeaponType.MediumProtonTorpedo}.Select(t=>new{type=t.ToString(),shots=weapons.GetProfile(t).ShotsPerSalvo,damage=weapons.GetProfile(t).Damage}).ToArray(),
            existingRepublicYWingHull=AssetDatabase.LoadAssetAtPath<SquadronData>("Assets/Settings/Data/Squadron/YWingSquadronData.asset").MemberHull
        };
        System.IO.File.WriteAllText("Temp/YWingBomberImport/Verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
