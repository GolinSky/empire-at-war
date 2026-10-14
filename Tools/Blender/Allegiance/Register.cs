using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterAllegiance
{
    const string VIEW="Assets/Prefabs/Models/Ships/Empire/AllegianceShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/Empire/AllegianceShipData.asset";
    public static string Main()
    {
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/AllegianceIcon.png");
        if(!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long localId))throw new InvalidOperationException("Missing ship sprite");
        string iconKey=iconGuid+":"+localId;
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        var mapping=Load("Assets/Settings/AssetMappingData.asset");
        foreach(var path in new[]{VIEW,DATA})
        {
            string guid=AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"),Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue=guid;
            var donor=path==VIEW?"Assets/Prefabs/Models/Ships/Empire/VictoryShipView.prefab":"Assets/Settings/Data/Ship/Empire/VictoryShipData.asset";
            var entry=settings.CreateOrMoveEntry(guid,settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address=Path.GetFileNameWithoutExtension(path);EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping);EditorUtility.SetDirty(settings);
        var ships=Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        Entry(ships.FindProperty("shipsData.keyValue"),213).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Entry(ui.FindProperty("shipIconWrapper.keyValue"),213).objectReferenceValue=icon;Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltip);
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/AllegianceReinforcementView.prefab");
        var spawn=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),213).objectReferenceValue=spawn;Save(reinforcement);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/AllegianceMatchups.asset";
        if(!File.Exists(MATCHUPS))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/VictoryMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="AllegianceMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=2;
        string[] labels={"Cruisers","Capital ships","Bombers","Fighter swarms"};
        string[] iconPaths={"Assets/Art/Textures/Ui/Icons/ShipIcon/MonCalCruiserIcon.png","Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIcon.png","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEFighterIcon.png"};
        for(int i=0;i<4;i++){var row=(i<2?strong:weak).GetArrayElementAtIndex(i%2);var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(iconPaths[i]);AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long fileId);row.FindPropertyRelative("label").stringValue=labels[i];row.FindPropertyRelative("iconKey").stringValue=guid+":"+fileId;}Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),213);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="Allegiance-class Battlecruiser";
        unit.FindPropertyRelative("role").stringValue="Heavy Battlecruiser / Anti-capital";
        unit.FindPropertyRelative("description").stringValue="Heavy anti-capital battlecruiser: 20,000 hull, 16,000 shields; three heavy dual turbolasers, six heavy quad turbolasers, six medium dual turbolasers and six medium ion cannons. Three engines, two shield generators, tractor beam and command array. Power to Main Batteries and Tractor Beam. No fighter complement.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;unit.FindPropertyRelative("isHero").boolValue=false;
        foreach(var field in new[]{new{key="MaxCount",value=3},new{key="AvailableLevel",value=4},new{key="Price",value=9200},new{key="BuildTime",value=85},new{key="UnitCapacity",value=12}})
            unit.FindPropertyRelative("<"+field.key+">k__BackingField").intValue=field.value;
        Save(faction);AssetDatabase.SaveAssets();
        return "Registered ship 213, own icon/preview/data/Addressables/faction entry, existing weapon profiles and abilities.";
    }
    static SerializedProperty Entry(SerializedProperty array,object key)
    {
        for(int i=0;i<array.arraySize;i++){var row=array.GetArrayElementAtIndex(i);var k=row.FindPropertyRelative("key");if(key is int n?k.intValue==n:k.stringValue==(string)key)return row.FindPropertyRelative("value");}
        int index=array.arraySize;array.arraySize++;var entry=array.GetArrayElementAtIndex(index);if(key is int number)entry.FindPropertyRelative("key").intValue=number;else entry.FindPropertyRelative("key").stringValue=(string)key;return entry.FindPropertyRelative("value");
    }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    static void Save(SerializedObject data){data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);}
}
