using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterDauntless
{
    const int SHIP_ID=310;
    const int ABILITY_ID=39;
    const string DATA="Assets/Settings/Data/Ship/DauntlessShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/DauntlessShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/DauntlessIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/DauntlessMatchups.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Ship registration requires Edit Mode.");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="DauntlessMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Small ships","Assets/Art/Textures/Ui/Icons/ShipIcon/DauntlessCorvetteIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Frigates","Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Bomber strikes","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="Heavy frontline cruiser with eight heavy turbolasers, four medium dual turbolasers, four heavy ion cannons and two point-defense lasers. Deploys X-Wing and Y-Wing squadrons. Sensor Jamming reduces nearby enemy accuracy by 20% for 8 seconds.";
        value.FindPropertyRelative("role").stringValue="Frontline capital combat";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="Dauntless Heavy Cruiser";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=3;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=5600;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=60;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=3;
        value.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;Save(faction);
        var ships=Load("Assets/Settings/Data/Ship/ShipsData.asset");
        row=Upsert(ships.FindProperty("shipsData.keyValue"),SHIP_ID);
        row.FindPropertyRelative("value.m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Upsert(ui.FindProperty("shipIconWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue=icon;Save(ui);
        var tooltips=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons=tooltips.FindProperty("icons");row=Enumerable.Range(0,icons.arraySize).Select(i=>icons.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").stringValue==iconKey)??Append(icons);
        row.FindPropertyRelative("key").stringValue=iconKey;row.FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltips);
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/DauntlessReinforcementView.prefab");
        Upsert(reinforcement.FindProperty("spawnShipWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue=preview.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="UnitSpawnView");Save(reinforcement);
        var mapping=Load("Assets/Settings/AssetMappingData.asset");
        foreach(var asset in new[]{VIEW,DATA})
        {
            var dictionary=mapping.FindProperty("assetMappings.keyValue");string key=System.IO.Path.GetFileNameWithoutExtension(asset);
            row=Enumerable.Range(0,dictionary.arraySize).Select(i=>dictionary.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").stringValue==key)??Append(dictionary);
            row.FindPropertyRelative("key").stringValue=key;row.FindPropertyRelative("value.m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(asset);
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(asset),settings.FindGroup(asset==VIEW?"View":"Data"));entry.address=key;EditorUtility.SetDirty(settings);
        }
        Save(mapping);
        var catalog=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var definitions=catalog.FindProperty("definitions.keyValue");
        row=Enumerable.Range(0,definitions.arraySize).Select(i=>definitions.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==ABILITY_ID);
        if(row==null)
        {
            int donor=Enumerable.Range(0,definitions.arraySize).Single(i=>definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue==3);
            definitions.InsertArrayElementAtIndex(donor); row=definitions.GetArrayElementAtIndex(donor); row.FindPropertyRelative("key").intValue=ABILITY_ID;
        }
        value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("displayName").stringValue="Sensor Jamming";
        value.FindPropertyRelative("description").stringValue="Reduce enemy accuracy by 20% within 500 game units for 8 seconds. Sensor destruction ends the effect. Recovers in 40 seconds; overlapping jammers do not stack.";
        value.FindPropertyRelative("duration").floatValue=8;
        value.FindPropertyRelative("recoveryDelay").floatValue=40;
        value.FindPropertyRelative("range").floatValue=500;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;
        value.FindPropertyRelative("aiUse").intValue=0;
        value.FindPropertyRelative("settings").managedReferenceValue=new SensorJammingSettings();
        Save(catalog);
        var overlay=Load("Assets/Settings/Data/Ship/HardPointOverlayData.asset");
        var entries=overlay.FindProperty("entries");
        var sensor=Enumerable.Range(0,entries.arraySize).Select(i=>entries.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("hardPointType").intValue==10)??Append(entries);
        sensor.FindPropertyRelative("hardPointType").intValue=10;
        sensor.FindPropertyRelative("displayName").stringValue="Sensor Array";
        sensor.FindPropertyRelative("description").stringValue="Destroying it disables Sensor Jamming.";
        sensor.FindPropertyRelative("icon").objectReferenceValue=value.FindPropertyRelative("icon").objectReferenceValue;
        Save(overlay);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {
            int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==3);
            abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;
        }
        Save(audio);AssetDatabase.SaveAssets();return "Dauntless=310 registered: Rebellion, views/data/placement/icons/matchups, Sensor Jamming=39 with audio and sensor overlay.";
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
