using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterMC30c
{
    const int SHIP_ID=307;
    const int ABILITY_ID=36;
    const string DATA="Assets/Settings/Data/Ship/MC30cShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/MC30cShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/MC30cIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/MC30cMatchups.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Ship registration requires Edit Mode.");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="MC30cMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Cruisers","Assets/Art/Textures/Ui/Icons/ShipIcon/CorellianCorvetteIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Anti-frigate fire","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="Fast torpedo frigate: 1,800 hull, 2,400 shields. Two heavy proton torpedoes, four medium turbolasers, four dual lasers, two point defense lasers and two burst cluster-bomb launchers. Torpedo Salvo increases torpedo firing rate by 50% for 6 seconds.";
        value.FindPropertyRelative("role").stringValue="Anti-capital strike ship";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="MC30c Frigate";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=2;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=2500;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=32;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=2;
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
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/MC30cReinforcementView.prefab");
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
        row=Upsert(definitions,ABILITY_ID); value=row.FindPropertyRelative("value");
        var donor=Enumerable.Range(0,definitions.arraySize).Select(i=>definitions.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("key").intValue==19).FindPropertyRelative("value");
        value.FindPropertyRelative("icon").objectReferenceValue=donor.FindPropertyRelative("icon").objectReferenceValue;
        value.FindPropertyRelative("displayName").stringValue="Torpedo Salvo";
        value.FindPropertyRelative("description").stringValue="Torpedoes fire 50% faster for 6 seconds. 30 second recovery. Other weapons remain unchanged.";
        value.FindPropertyRelative("duration").floatValue=6; value.FindPropertyRelative("recoveryDelay").floatValue=30;
        value.FindPropertyRelative("canCancel").boolValue=false; value.FindPropertyRelative("isToggle").boolValue=false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;
        value.FindPropertyRelative("aiUse").intValue=donor.FindPropertyRelative("aiUse").intValue;
        value.FindPropertyRelative("settings").managedReferenceValue=new TorpedoSalvoSettings(); Save(catalog);
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset"); var profiles=weapons.FindProperty("weapons");
        if(!Enumerable.Range(0,profiles.arraySize).Any(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==81))
        {
            int index=Enumerable.Range(0,profiles.arraySize).Single(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==35);
            profiles.InsertArrayElementAtIndex(index); row=profiles.GetArrayElementAtIndex(index); row.FindPropertyRelative("weaponType").intValue=81;
        }
        row=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("weaponType").intValue==81);
        row.FindPropertyRelative("displayName").stringValue="Cluster Bomb Launcher";
        row.FindPropertyRelative("pattern").intValue=1; row.FindPropertyRelative("burstShots").intValue=6;
        row.FindPropertyRelative("scope").intValue=2; row.FindPropertyRelative("unit").stringValue="MC30c";
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==19);abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;}
        Save(audio);AssetDatabase.SaveAssets();return "MC30c=307 registered: Rebellion, views/data/placement/icons/matchups, Torpedo Salvo=36 and burst cluster-bomb profile=81 with audio.";
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
