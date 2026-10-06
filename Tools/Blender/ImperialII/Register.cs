using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterImperialII
{
    const string VIEW="Assets/Prefabs/Models/Ships/ImperialIIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ImperialIIShipData.asset";
    const int SHIP_ID=205;
    public static string Main()
    {
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,36,41,"Light Long-Range 4-Rapid-Burst Dual Turbolaser",135,4,.317f,17.1705f,450,133,2);
        Profile(weapons,32,42,"Medium Long-Range 2-Burst Turbo-Ion Cannon",225,2,9.5f,19.25f,525,55,4);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("weapons"),"weaponType",36,41);
        CloneRow(audio.FindProperty("weapons"),"weaponType",32,42);
        CloneRow(audio.FindProperty("abilities"),"abilityId",5,24);
        Save(audio);
        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definition=CloneRow(catalog.FindProperty("definitions.keyValue"),"key",5,24).FindPropertyRelative("value");
        definition.FindPropertyRelative("displayName").stringValue="Power to Main Batteries";
        definition.FindPropertyRelative("description").stringValue="Main-battery shot and reload delays are reduced to 33%. All other weapons are disabled, shield regeneration stops and movement speed is reduced to 25% for 20 s. 60 s recharge.";
        definition.FindPropertyRelative("duration").floatValue=20;
        definition.FindPropertyRelative("recoveryDelay").floatValue=60;
        definition.FindPropertyRelative("canCancel").boolValue=false;
        definition.FindPropertyRelative("isToggle").boolValue=false;
        definition.FindPropertyRelative("requiresEnemyTarget").boolValue=false;
        definition.FindPropertyRelative("range").floatValue=0;
        definition.FindPropertyRelative("aiUse").intValue=1;
        definition.FindPropertyRelative("settings").managedReferenceValue=new PowerToMainBatteriesSettings();
        Save(catalog);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ImperialIIIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId);
        string iconKey=iconGuid+":"+localId;
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        var mapping=Load("Assets/Settings/AssetMappingData.asset");
        foreach(var path in new[]{VIEW,DATA})
        {
            string guid=AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"),Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue=guid;
            var donor=path==VIEW?"Assets/Prefabs/Models/Ships/ImperatorShipView.prefab":"Assets/Settings/Data/Ship/ImperatorShipData.asset";
            var addressable=settings.CreateOrMoveEntry(guid,settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            addressable.address=Path.GetFileNameWithoutExtension(path);EditorUtility.SetDirty(addressable.parentGroup);
        }
        Save(mapping);EditorUtility.SetDirty(settings);
        var ships=Load("Assets/Settings/Data/Ship/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"),SHIP_ID).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Entry(ui.FindProperty("shipIconWrapper.keyValue"),SHIP_ID).objectReferenceValue=icon;Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons=tooltip.FindProperty("icons");int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltip);
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ImperialIIReinforcementView.prefab");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),SHIP_ID).objectReferenceValue=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");Save(reinforcement);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/ImperialIIMatchups.asset";
        if(!File.Exists(MATCHUPS))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/ImperatorMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="ImperialIIMatchups";Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),SHIP_ID);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="Imperial II Star Destroyer";
        unit.FindPropertyRelative("role").stringValue="Capital ship / Ship of the line";
        unit.FindPropertyRelative("description").stringValue="21,000 hull; 18,000 shields; speed 25. Targetable: eight light long-range 4-rapid-burst dual turbolasers, two medium long-range 2-burst turbo-ion cannons, two shield generators, three engines, tractor beam and hangar. Additional weapons: three medium 3-burst turbolasers, six light turbolasers and four heavy laser cannons. Power to Main Batteries boosts main-battery firing rate, disables other weapons, halts shield regeneration and reduces speed. Tractor Beam restricts enemy movement. Launches TIE Interceptors, TIE Brutes and TIE Punishers.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        unit.FindPropertyRelative("isHero").boolValue=false;
        unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=3;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue=25000;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue=500;
        unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=8;Save(faction);
        AssetDatabase.SaveAssets();
        return "Imperial II registered as Empire ship 205 with main-battery ability 24, weapons 41/42, icons, placement, faction, audio and existing Addressables groups.";
    }
    static SerializedProperty Entry(SerializedProperty array,object key)
    {
        for(int i=0;i<array.arraySize;i++)
        {
            var row=array.GetArrayElementAtIndex(i);var item=row.FindPropertyRelative("key");
            if(key is int?item.intValue==(int)key:item.stringValue==(string)key)return row.FindPropertyRelative("value");
        }
        int index=array.arraySize;array.arraySize++;var added=array.GetArrayElementAtIndex(index);
        if(key is int)added.FindPropertyRelative("key").intValue=(int)key;else added.FindPropertyRelative("key").stringValue=(string)key;
        return added.FindPropertyRelative("value");
    }
    static SerializedProperty CloneRow(SerializedProperty array,string field,int source,int target)
    {
        for(int i=0;i<array.arraySize;i++)if(array.GetArrayElementAtIndex(i).FindPropertyRelative(field).intValue==target)return array.GetArrayElementAtIndex(i);
        int index=Enumerable.Range(0,array.arraySize).Single(i=>array.GetArrayElementAtIndex(i).FindPropertyRelative(field).intValue==source);
        array.InsertArrayElementAtIndex(index);var row=array.GetArrayElementAtIndex(index);row.FindPropertyRelative(field).intValue=target;return row;
    }
    static void Profile(SerializedObject weapons,int source,int type,string name,float damage,int shots,float interval,float reload,float range,float speed,int damageType)
    {
        var p=CloneRow(weapons.FindProperty("weapons"),"weaponType",source,type);
        p.FindPropertyRelative("displayName").stringValue=name;p.FindPropertyRelative("damageType").intValue=damageType;
        p.FindPropertyRelative("damage").floatValue=damage;p.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        p.FindPropertyRelative("shotInterval").floatValue=interval;p.FindPropertyRelative("reload").floatValue=reload;
        p.FindPropertyRelative("range").floatValue=range;p.FindPropertyRelative("projectileSpeed").floatValue=speed;
    }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Save(SerializedObject data){data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);}
}
