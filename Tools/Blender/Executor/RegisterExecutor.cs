using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterExecutor
{
    const string VIEW="Assets/Prefabs/Models/Ships/ExecutorShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ExecutorShipData.asset";
    public static string Main()
    {
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,2,49,"Heavy Long-Range 2-Burst Turbolaser",150,2,7.5f,15,450,45,3);
        Profile(weapons,43,50,"Heavy 2-Burst Dual Turbolaser",300,2,7.5f,20,450,45,3);
        Profile(weapons,44,51,"Heavy Long-Range 2-Burst Dual Turbo-Ion",315,2,11.33f,22.66f,450,45,4);
        Profile(weapons,34,52,"Heavy 8-Burst Artillery-Rocket Launcher",20,8,3,15,500,75,11);
        Profile(weapons,5,53,"Medium Long-Range 2-Burst Dual Turbolaser",210,2,8.16f,21.719f,393.75f,55,2);
        Profile(weapons,14,54,"Repeating Point-Defense Mount",4,6,.1f,3.998f,125,80,1);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(var pair in new[]{new{source=2,target=49},new{source=43,target=50},new{source=44,target=51},new{source=34,target=52},new{source=5,target=53},new{source=14,target=54}})
            CloneRow(audio.FindProperty("weapons"),"weaponType",pair.source,pair.target);
        Save(audio);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ExecutorIcon.png");
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
        Entry(ships.FindProperty("shipsData.keyValue"),207).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Entry(ui.FindProperty("shipIconWrapper.keyValue"),207).objectReferenceValue=icon;Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltip);
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ExecutorReinforcementView.prefab");
        var spawn=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),207).objectReferenceValue=spawn;Save(reinforcement);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/ExecutorMatchups.asset";
        if(!File.Exists(MATCHUPS))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/VictoryMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="ExecutorMatchups";Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),207);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="Executor Star Dreadnought";
        unit.FindPropertyRelative("role").stringValue="Dreadnought / Fleet Flagship";
        unit.FindPropertyRelative("description").stringValue="200,000 hull; 200,000 shields; speed 15. Targetable: ten heavy 2-burst long-range turbolasers, 23 heavy 2-burst dual turbolasers, eight heavy 2-burst long-range dual turbo-ions, ten heavy 8-burst artillery-rocket launchers, four shield generators, two engines and two hangars. Integrated: 69 medium 2-burst dual long-range turbolasers, 40 heavy lasers and 14 repeating point-defense mounts. Laser Beam and Tractor Beam. TIE Fighter and TIE Bomber complement.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;unit.FindPropertyRelative("isHero").boolValue=false;
        foreach(var field in new[]{new{key="MaxCount",value=1},new{key="AvailableLevel",value=5},new{key="Price",value=125000},new{key="BuildTime",value=2500},new{key="UnitCapacity",value=40}})
            unit.FindPropertyRelative("<"+field.key+">k__BackingField").intValue=field.value;
        Save(faction);AssetDatabase.SaveAssets();
        return "Registered ship 207, own icon/preview/data/Addressables/faction entry, six dedicated weapon profiles and firing audio.";
    }
    static void Profile(SerializedObject data,int source,int type,string name,float damage,int shots,float interval,float reload,float range,float speed,int damageType)
    {
        var row=CloneRow(data.FindProperty("weapons"),"weaponType",source,type);
        row.FindPropertyRelative("displayName").stringValue=name;row.FindPropertyRelative("damage").floatValue=damage;
        row.FindPropertyRelative("damageType").intValue=damageType;row.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        row.FindPropertyRelative("shotInterval").floatValue=interval;row.FindPropertyRelative("reload").floatValue=reload;
        row.FindPropertyRelative("range").floatValue=range;row.FindPropertyRelative("projectileSpeed").floatValue=speed;
        row.FindPropertyRelative("interceptable").boolValue=type==52;row.FindPropertyRelative("color").colorValue=type==52?new Color(1,.25f,.1f):type==51?new Color(.2f,.6f,1):new Color(.2f,1,.25f);
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
