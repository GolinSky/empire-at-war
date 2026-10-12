using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterNebula
{
    const int SHIP_ID=330;
    const int ABILITY_ID=38;
    const string DATA="Assets/Settings/Data/Ship/Rebellion/NebulaShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/Rebellion/NebulaShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/NebulaIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/NebulaMatchups.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Ship registration requires Edit Mode.");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="NebulaMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Heavy cruisers","Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianCorvetteIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Star destroyers","Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Bomber strikes","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="Heavy frontline star destroyer. Eight dual heavy turbolasers, six medium turbolasers, four medium ion cannons, two assault concussion launchers and four point defense lasers. Supports three active X-Wing squadrons and two replacements. Overcharge Weapons increases firing rate by 25% and disables shield regeneration for 10 seconds; 50 second cooldown.";
        value.FindPropertyRelative("role").stringValue="Heavy frontline combat";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="Nebula Star Destroyer";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=5;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=7200;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=72;
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
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/NebulaReinforcementView.prefab");
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
        int index=Enumerable.Range(0,definitions.arraySize).Single(i=>definitions.GetArrayElementAtIndex(i).FindPropertyRelative("key").intValue==5);
        row=Enumerable.Range(0,definitions.arraySize).Select(i=>definitions.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==ABILITY_ID);
        if(row==null){definitions.InsertArrayElementAtIndex(index);row=definitions.GetArrayElementAtIndex(index);row.FindPropertyRelative("key").intValue=ABILITY_ID;}
        value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("displayName").stringValue="Overcharge Weapons";
        value.FindPropertyRelative("description").stringValue="Weapons fire 25% faster; shield regeneration stops for 10 seconds. 50 second cooldown.";
        value.FindPropertyRelative("duration").floatValue=10;value.FindPropertyRelative("recoveryDelay").floatValue=50;
        value.FindPropertyRelative("canCancel").boolValue=false;value.FindPropertyRelative("isToggle").boolValue=false;value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;
        value.FindPropertyRelative("settings").managedReferenceValue=new BoostWeaponPowerSettings();
        var modifier=value.FindPropertyRelative("settings.statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue=1;modifier.FindPropertyRelative("fireDelayMultiplier").floatValue=.8f;
        modifier.FindPropertyRelative("speedMultiplier").floatValue=1;modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue=0;modifier.FindPropertyRelative("damageTakenMultiplier").floatValue=1;Save(catalog);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==5);abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;}
        Save(audio);AssetDatabase.SaveAssets();return "Nebula=330 registered with Rebellion, data/view/placement/icons/matchups and Overcharge Weapons=38.";
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
