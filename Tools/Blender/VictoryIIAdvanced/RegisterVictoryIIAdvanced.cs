using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterVictoryIIAdvanced
{
    const string VIEW="Assets/Prefabs/Models/Ships/VictoryIIAdvancedShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/VictoryIIAdvancedShipData.asset";
    public static string Main()
    {
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,32,37,"Medium Dual Turbo-Ion Cannon",30,2,.08f,10.25f,325,55,4);
        Profile(weapons,4,38,"Medium 3-Burst Turbolaser",70,3,3.25f,6.75f,325,133,2);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(var pair in new[]{new{source=32,target=37},new{source=4,target=38}})
            CloneRow(audio.FindProperty("weapons"),"weaponType",pair.source,pair.target);
        Save(audio);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIIAdvancedIcon.png");
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
        Entry(ships.FindProperty("shipsData.keyValue"),203).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Entry(ui.FindProperty("shipIconWrapper.keyValue"),203).objectReferenceValue=icon;Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltip);
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/VictoryIIAdvancedReinforcementView.prefab");
        var spawn=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),203).objectReferenceValue=spawn;Save(reinforcement);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/VictoryIIAdvancedMatchups.asset";
        if(!File.Exists(MATCHUPS))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/VictoryMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="VictoryIIAdvancedMatchups";Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),203);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="Victory II Star Destroyer — Advanced Loadout";
        unit.FindPropertyRelative("role").stringValue="Ion Support / Cruiser";
        unit.FindPropertyRelative("description").stringValue="12,000 hull; 10,000 shields; speed 20. Four medium dual turbo-ion cannons and two medium turbo-ion cannons. Targetable shield generator, two engines and tractor beam. Six medium 3-burst turbolasers, six heavy laser cannons and hangar are not separately targetable. Boost Weapon Power doubles fire rate, stops shield regeneration and halves speed for 20 s. Tractor Beam restricts enemy movement. TIE-Interceptor complement.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;unit.FindPropertyRelative("isHero").boolValue=false;
        foreach(var field in new[]{new{key="MaxCount",value=10},new{key="AvailableLevel",value=3},new{key="Price",value=13750},new{key="BuildTime",value=275},new{key="UnitCapacity",value=7}})
            unit.FindPropertyRelative("<"+field.key+">k__BackingField").intValue=field.value;
        Save(faction);AssetDatabase.SaveAssets();
        return "Registered ship 203, own icon/preview/data/Addressables/faction entry, two weapon profiles and firing audio.";
    }
    static void Profile(SerializedObject data,int source,int type,string name,float damage,int shots,float interval,float reload,float range,float speed,int damageType)
    {
        var row=CloneRow(data.FindProperty("weapons"),"weaponType",source,type);
        row.FindPropertyRelative("displayName").stringValue=name;row.FindPropertyRelative("damage").floatValue=damage;
        row.FindPropertyRelative("damageType").intValue=damageType;row.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        row.FindPropertyRelative("shotInterval").floatValue=interval;row.FindPropertyRelative("reload").floatValue=reload;
        row.FindPropertyRelative("range").floatValue=range;row.FindPropertyRelative("projectileSpeed").floatValue=speed;
        row.FindPropertyRelative("interceptable").boolValue=false;row.FindPropertyRelative("color").colorValue=type==37?new Color(.4f,.87f,.99f):new Color(.2f,1,.25f);
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
