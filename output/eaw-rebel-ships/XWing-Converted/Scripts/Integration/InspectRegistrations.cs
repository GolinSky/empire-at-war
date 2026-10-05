using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipAbilities;

public static class InspectXWingRegistrations
{
    public static string Main()
    {
        var catalog=AssetDatabase.LoadAssetAtPath<FactionCatalog>("Assets/Settings/Data/Factions/Shared/FactionCatalog.asset");
        var faction=catalog.GetSquadronFactionData(SquadronType.XWing);
        var ability=AssetDatabase.LoadAssetAtPath<ShipAbilityCatalog>("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset").Get(ShipAbilityId.LockSFoils);
        var rows=new System.Collections.Generic.List<object>();
        foreach(var item in new[]{
            new[]{"Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset","squadrons.keyValue","key","300","value.<Icon>k__BackingField"},
            new[]{"Assets/Settings/Data/Models/ShipUi/ShipUiData.asset","squadronIconWrapper.keyValue","key","300","value"},
            new[]{"Assets/Settings/Data/Reinforcement/ReinforcementData.asset","spawnSquadronWrapper.keyValue","key","300","value"},
            new[]{"Assets/Settings/Data/Models/Audio/ShipSfxData.asset","weapons","weaponType","24",""},
            new[]{"Assets/Settings/Data/Models/Audio/ShipSfxData.asset","abilities","abilityId","18",""},
            new[]{"Assets/Settings/Data/Models/Weapon/WeaponsData.asset","weapons","weaponType","24",""}})
        {
            var so=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(item[0]));var list=so.FindProperty(item[1]);
            var matches=Enumerable.Range(0,list.arraySize).Select(i=>list.GetArrayElementAtIndex(i)).Where(p=>p.FindPropertyRelative(item[2]).intValue==int.Parse(item[3])).ToArray();
            if(matches.Length!=1)throw new Exception("Expected one registration: "+item[0]);
            string target=item[4]==""?"":AssetDatabase.GetAssetPath(matches[0].FindPropertyRelative(item[4]).objectReferenceValue);
            rows.Add(new{asset=item[0],key=item[3],target});
        }
        foreach(var path in new[]{"Assets/Prefabs/Models/Squadrons/XWingSquadronView.prefab","Assets/Settings/Data/Squadron/XWingSquadronData.asset"})
        {
            string guid=AssetDatabase.AssetPathToGUID(path);var entry=AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(guid);
            var map=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Settings/AssetMappingData.asset")).FindProperty("assetMappings.keyValue");
            var mapped=Enumerable.Range(0,map.arraySize).Select(i=>map.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").stringValue==Path.GetFileNameWithoutExtension(path));
            if(mapped.FindPropertyRelative("value.m_AssetGUID").stringValue!=guid)throw new Exception("Wrong asset mapping: "+path);
            rows.Add(new{asset=path,address=entry.address,group=entry.parentGroup.Name,guid});
        }
        var result=new{factionName=faction.Name,faction.Price,faction.BuildTime,faction.AvailableLevel,faction.UnitCapacity,ability.IsToggle,ability.CanCancel,ability.Duration,ability.RecoveryDelay,settings=ability.Settings.GetType().Name,registrations=rows};
        File.WriteAllText("Temp/XWingImport/Output/RegistrationVerification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return Newtonsoft.Json.JsonConvert.SerializeObject(result);
    }
}
