using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterQuasarFire
{
    const int SHIP_ID=(int)EmpireAtWar.Models.Factions.ShipType.QuasarFire;
    const int ABILITY_ID=(int)EmpireAtWar.Services.ShipAbilities.ShipAbilityId.FighterReinforcements;
    const string DATA="Assets/Settings/Data/Ship/QuasarFireShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/QuasarFireShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/QuasarFireIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/QuasarFireMatchups.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Ship registration requires Edit Mode.");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="QuasarFireMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=1;
        Matchup(strong.GetArrayElementAtIndex(0),"Squadron support","Assets/Art/Textures/Ui/Icons/SquadronIcon/XWingIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Heavy capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="Light carrier with two light turbolasers and two point-defense lasers. Carries one active and one reserve X-Wing and Y-Wing squadron each. Fighter Reinforcements launches one available replacement from reserve; cooldown 30 seconds. Requires escorts.";
        value.FindPropertyRelative("role").stringValue="Light carrier / Squadron support";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="Quasar Fire";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=2;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=2400;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=35;
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
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/QuasarFireReinforcementView.prefab");
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
        row=Upsert(catalog.FindProperty("definitions.keyValue"),ABILITY_ID); value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("settings").managedReferenceValue=new FighterReinforcementsSettings();
        value.FindPropertyRelative("icon").objectReferenceValue=icon;
        value.FindPropertyRelative("displayName").stringValue="Fighter Reinforcements";
        value.FindPropertyRelative("description").stringValue="Launch one replacement X-Wing or Y-Wing squadron from an operational bay's reserve. Requires a vacant active slot after a squadron is lost. Cooldown: 30 seconds.";
        value.FindPropertyRelative("duration").floatValue=0; value.FindPropertyRelative("recoveryDelay").floatValue=30;
        value.FindPropertyRelative("isToggle").boolValue=false; value.FindPropertyRelative("canCancel").boolValue=false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false; value.FindPropertyRelative("range").floatValue=0;
        value.FindPropertyRelative("aiUse").intValue=0; Save(catalog);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {
            int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==3);
            abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;
        }
        Save(audio);AssetDatabase.SaveAssets();return "Quasar registered in Rebellion, ship data/view/placement/icons/matchups and Fighter Reinforcements with audio.";
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
