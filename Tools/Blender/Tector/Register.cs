using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterTector
{
    const string VIEW="Assets/Prefabs/Models/Ships/TectorShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/TectorShipData.asset";
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Tector registration requires Edit Mode.");
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("abilities"),"abilityId",5,32);Save(audio);
        var abilities=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var ability=CloneRow(abilities.FindProperty("definitions.keyValue"),"key",5,32).FindPropertyRelative("value");
        ability.FindPropertyRelative("settings").managedReferenceValue=new BoostWeaponPowerSettings();
        abilities.ApplyModifiedPropertiesWithoutUndo();abilities.Update();
        ability=Entry(abilities.FindProperty("definitions.keyValue"),32);
        ability.FindPropertyRelative("displayName").stringValue="Boost Weapon Power";
        ability.FindPropertyRelative("description").stringValue="For 20 seconds, double fire rate, stop shield regeneration, take 50% more damage and reduce speed to 25%. Recovers in 60 seconds.";
        ability.FindPropertyRelative("duration").floatValue=20;ability.FindPropertyRelative("recoveryDelay").floatValue=60;
        ability.FindPropertyRelative("canCancel").boolValue=false;ability.FindPropertyRelative("isToggle").boolValue=false;
        ability.FindPropertyRelative("requiresEnemyTarget").boolValue=false;ability.FindPropertyRelative("range").floatValue=0;
        ability.FindPropertyRelative("aiUse").intValue=2;
        var modifier=ability.FindPropertyRelative("settings.statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue=1;modifier.FindPropertyRelative("fireDelayMultiplier").floatValue=.5f;
        modifier.FindPropertyRelative("speedMultiplier").floatValue=.25f;modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue=0;
        modifier.FindPropertyRelative("damageTakenMultiplier").floatValue=1.5f;Save(abilities);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/TectorIcon.png");
        if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId))throw new InvalidOperationException("Missing ship sprite");
        string iconKey=iconGuid+":"+localId;
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        var mapping=Load("Assets/Settings/AssetMappingData.asset");
        foreach(var path in new[]{VIEW,DATA})
        {
            string guid=AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"),Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue=guid;
            var donor=path==VIEW?"Assets/Prefabs/Models/Ships/VictoryShipView.prefab":"Assets/Settings/Data/Ship/VictoryShipData.asset";
            var entry=settings.CreateOrMoveEntry(guid,settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address=Path.GetFileNameWithoutExtension(path);EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping);EditorUtility.SetDirty(settings);
        var ships=Load("Assets/Settings/Data/Ship/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"),210).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Entry(ui.FindProperty("shipIconWrapper.keyValue"),210).objectReferenceValue=icon;Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltip);
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab");
        var spawn=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),210).objectReferenceValue=spawn;Save(reinforcement);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/TectorMatchups.asset";
        if(!File.Exists(MATCHUPS))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/ISDIShipMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="TectorMatchups";Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),210);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="Tector Star Destroyer";
        unit.FindPropertyRelative("role").stringValue="Ship of the Line / Capital Ship";
        unit.FindPropertyRelative("description").stringValue="22,000 hull; 16,000 shields; speed 22. Eight targetable heavy 2-burst turbolasers, two medium long-range dual turbolasers, two shield generators, three engines and tractor beam. Seven medium 3-burst turbolasers, four light turbolasers and four heavy laser cannons are not separately targetable. Boost Weapon Power increases firepower at the expense of defense and speed. Tractor Beam restricts enemy movement. No hangars or fighter complement.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;unit.FindPropertyRelative("isHero").boolValue=false;
        foreach(var field in new[]{new{key="MaxCount",value=3},new{key="AvailableLevel",value=3},new{key="Price",value=25000},new{key="BuildTime",value=500},new{key="UnitCapacity",value=8}})
            unit.FindPropertyRelative("<"+field.key+">k__BackingField").intValue=field.value;
        Save(faction);AssetDatabase.SaveAssets();
        return "Registered Empire Tector 210; data/view/icons/placement/Addressables; weapon-power ability 32 and audio.";
    }
    static SerializedProperty CloneRow(SerializedProperty array,string key,int source,int target)
    {
        for(int i=0;i<array.arraySize;i++)if(array.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue==target)return array.GetArrayElementAtIndex(i);
        int donor=Enumerable.Range(0,array.arraySize).Single(i=>array.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue==source);
        array.InsertArrayElementAtIndex(donor);var row=array.GetArrayElementAtIndex(donor);row.FindPropertyRelative(key).intValue=target;return row;
    }
    static SerializedProperty Entry(SerializedProperty array,object key)
    {
        for(int i=0;i<array.arraySize;i++){var row=array.GetArrayElementAtIndex(i);var k=row.FindPropertyRelative("key");if(key is int n?k.intValue==n:k.stringValue==(string)key)return row.FindPropertyRelative("value");}
        int index=array.arraySize;array.arraySize++;var entry=array.GetArrayElementAtIndex(index);if(key is int number)entry.FindPropertyRelative("key").intValue=number;else entry.FindPropertyRelative("key").stringValue=(string)key;return entry.FindPropertyRelative("value");
    }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Save(SerializedObject data){data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);}
}
