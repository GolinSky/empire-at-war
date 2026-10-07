using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterVictoryI
{
    const string VIEW="Assets/Prefabs/Models/Ships/VictoryIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/VictoryIShipData.asset";
    public static string Main()
    {
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,13,34,"Heavy Artillery-Rocket Launcher",20,4,3,15,250,75,11);
        Profile(weapons,13,35,"Barrage-Rocket Launcher",40,6,2,15,100,90,0);
        Profile(weapons,5,36,"Light Dual Turbolaser",45,2,.08f,6.91f,125,133,2);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(var pair in new[]{new{source=13,target=34},new{source=13,target=35},new{source=5,target=36}})
            CloneRow(audio.FindProperty("weapons"),"weaponType",pair.source,pair.target);
        Save(audio);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIIcon.png");
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
        Entry(ships.FindProperty("shipsData.keyValue"),202).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Entry(ui.FindProperty("shipIconWrapper.keyValue"),202).objectReferenceValue=icon;Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltip);
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/VictoryIReinforcementView.prefab");
        var spawn=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),202).objectReferenceValue=spawn;Save(reinforcement);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/VictoryIMatchups.asset";
        if(!File.Exists(MATCHUPS))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/VictoryMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="VictoryIMatchups";Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),202);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="Victory I Star Destroyer";
        unit.FindPropertyRelative("role").stringValue="Artillery / Cruiser";
        unit.FindPropertyRelative("description").stringValue="12,000 hull; 8,000 shields; speed 175. Four heavy 4-burst artillery-rocket launchers and two 6-burst barrage-rocket launchers. Targetable shield generator, two engines and tractor beam. Six light dual turbolasers, four heavy laser cannons and hangar are not separately targetable. Full Salvo accelerates rockets and slows other weapons. Tractor Beam restricts enemy movement. TIE-Interceptor complement.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;unit.FindPropertyRelative("isHero").boolValue=false;
        foreach(var field in new[]{new{key="MaxCount",value=10},new{key="AvailableLevel",value=2},new{key="Price",value=12250},new{key="BuildTime",value=245},new{key="UnitCapacity",value=7}})
            unit.FindPropertyRelative("<"+field.key+">k__BackingField").intValue=field.value;
        Save(faction);AssetDatabase.SaveAssets();
        return "Registered ship 202, own icon/preview/data/Addressables/faction entry, three weapon profiles and firing audio.";
    }
    static void Profile(SerializedObject data,int source,int type,string name,float damage,int shots,float interval,float reload,float range,float speed,int damageType)
    {
        var row=CloneRow(data.FindProperty("weapons"),"weaponType",source,type);
        row.FindPropertyRelative("displayName").stringValue=name;row.FindPropertyRelative("damage").floatValue=damage;
        row.FindPropertyRelative("damageType").intValue=damageType;row.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        row.FindPropertyRelative("shotInterval").floatValue=interval;row.FindPropertyRelative("reload").floatValue=reload;
        row.FindPropertyRelative("range").floatValue=range;row.FindPropertyRelative("projectileSpeed").floatValue=speed;
        row.FindPropertyRelative("interceptable").boolValue=type!=36;row.FindPropertyRelative("color").colorValue=type==36?new Color(.2f,1,.25f):type==35?new Color(1,.25f,.65f):new Color(1,.25f,.1f);
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
