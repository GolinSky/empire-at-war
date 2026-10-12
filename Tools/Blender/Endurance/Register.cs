using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterEndurance
{
    const int SHIP_ID=322;
    const int ABILITY_ID=52;
    const string DATA="Assets/Settings/Data/Ship/Rebellion/EnduranceShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/Rebellion/EnduranceShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/EnduranceIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/EnduranceMatchups.asset";

    public static string Main()
    {
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="EnduranceMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Squadrons","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Corvettes","Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianCorvetteIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Heavy capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="Fleet carrier with six active and six replacement X-Wing squadrons, four medium turbolasers, two medium ion cannons and six point-defense mounts. Fighter Coordination boosts nearby allied squadron accuracy and speed.";
        value.FindPropertyRelative("role").stringValue="Fleet carrier / Squadron support";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="Endurance Fleet Carrier";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=4;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=6000;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=65;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=8;
        value.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;Save(faction);
        var ships=Load("Assets/Settings/Data/Ship/Shared/ShipsData.asset");
        row=Upsert(ships.FindProperty("shipsData.keyValue"),SHIP_ID);
        row.FindPropertyRelative("value.m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");
        Upsert(ui.FindProperty("shipIconWrapper.keyValue"),SHIP_ID).FindPropertyRelative("value").objectReferenceValue=icon;Save(ui);
        var tooltips=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons=tooltips.FindProperty("icons");row=Enumerable.Range(0,icons.arraySize).Select(i=>icons.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").stringValue==iconKey)??Append(icons);
        row.FindPropertyRelative("key").stringValue=iconKey;row.FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltips);
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/EnduranceReinforcementView.prefab");
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
        row=Upsert(catalog.FindProperty("definitions.keyValue"),ABILITY_ID);value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("settings").managedReferenceValue=new FighterCoordinationSettings();
        value.FindPropertyRelative("icon").objectReferenceValue=icon;
        value.FindPropertyRelative("displayName").stringValue="Fighter Coordination";
        value.FindPropertyRelative("description").stringValue="Nearby allied squadrons gain +20% accuracy and +15% movement speed for 10 seconds. Command radius: 750. Recovery: 45 seconds.";
        value.FindPropertyRelative("duration").floatValue=10;value.FindPropertyRelative("recoveryDelay").floatValue=45;
        value.FindPropertyRelative("isToggle").boolValue=false;value.FindPropertyRelative("canCancel").boolValue=false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;value.FindPropertyRelative("range").floatValue=0;value.FindPropertyRelative("aiUse").intValue=2;Save(catalog);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {
            int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==5);
            abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;
        }
        Save(audio);AssetDatabase.SaveAssets();return "Endurance=322 registered: Rebellion roster, ship data/view Addressables, preview, three icon consumers, matchups, Fighter Coordination=52 and audio.";
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
