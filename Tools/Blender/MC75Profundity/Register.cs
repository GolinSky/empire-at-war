using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterMC75
{
    const int SHIP_ID=304;
    const int ABILITY_ID=19;
    const string DATA="Assets/Settings/Data/Ship/MC75ProfundityShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/MC75ProfundityShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/MC75ProfundityIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/MC75ProfundityMatchups.asset";

    public static string Main()
    {
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="MC75ProfundityMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Frigates","Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Bomber strikes","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="MC75 capital ship armed with proton and ion torpedoes, close-range turbolasers, heavy lasers and point defense. Full Salvo triples torpedo fire rate while slowing its other weapons for 20 seconds. Carries X-Wing and Y-Wing squadrons.";
        value.FindPropertyRelative("role").stringValue="Torpedo capital ship / Shield disabler";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="MC75 Profundity";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=3;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=13000;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=260;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=8;
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
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/MC75ProfundityReinforcementView.prefab");
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
        value.FindPropertyRelative("settings").managedReferenceValue=new FullSalvoSettings();
        value.FindPropertyRelative("icon").objectReferenceValue=icon;
        value.FindPropertyRelative("displayName").stringValue="Full Salvo";
        value.FindPropertyRelative("description").stringValue="For 20 seconds, missiles, rockets and torpedoes fire about three times faster, while other weapons fire at one-third their normal rate. Recharge: 60 seconds after the salvo ends.";
        value.FindPropertyRelative("duration").floatValue=20;value.FindPropertyRelative("recoveryDelay").floatValue=60;
        value.FindPropertyRelative("isToggle").boolValue=false;value.FindPropertyRelative("canCancel").boolValue=false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;value.FindPropertyRelative("range").floatValue=0;value.FindPropertyRelative("aiUse").intValue=2;Save(catalog);
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var profiles=weapons.FindProperty("weapons");
        Profile(profiles,25,6,"Med Lrg Proton TRP",6,80,1,3.75f,7.5f,150,true);
        Profile(profiles,26,6,"Medium Ion Torpedo",4,80,1,7.5f,15,150,true);
        Profile(profiles,27,1,"Light Close-Range Turbolaser",2,18,3,.2f,3.75f,150,false);
        Profile(profiles,28,8,"Heavy Laser Cannon",0,17.5f,1,1,2.25f,200,false);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(var pair in new[]{new[]{25,6},new[]{26,6},new[]{27,1},new[]{28,8}})
        {
            var sounds=audio.FindProperty("weapons");
            if(Enumerable.Range(0,sounds.arraySize).Any(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==pair[0]))continue;
            int index=Enumerable.Range(0,sounds.arraySize).Single(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==pair[1]);
            sounds.InsertArrayElementAtIndex(index);sounds.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=pair[0];
        }
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {
            int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==5);
            abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;
        }
        Save(audio);AssetDatabase.SaveAssets();return "MC75=304 registered in Rebellion, ship data/view Addressables, placement, icons, matchups, Full Salvo=19, four weapon profiles and audio.";
    }

    static void Profile(SerializedProperty profiles,int id,int donor,string name,int damageType,float damage,int shots,float interval,float reload,float range,bool interceptable)
    {
        var row=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("weaponType").intValue==id);
        if(row==null){int index=Enumerable.Range(0,profiles.arraySize).Single(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==donor);profiles.InsertArrayElementAtIndex(index);row=profiles.GetArrayElementAtIndex(index);}
        row.FindPropertyRelative("weaponType").intValue=id;row.FindPropertyRelative("displayName").stringValue=name;row.FindPropertyRelative("damageType").intValue=damageType;
        row.FindPropertyRelative("damage").floatValue=damage;row.FindPropertyRelative("shotsPerSalvo").intValue=shots;row.FindPropertyRelative("shotInterval").floatValue=interval;row.FindPropertyRelative("reload").floatValue=reload;row.FindPropertyRelative("range").floatValue=range;
        row.FindPropertyRelative("interceptable").boolValue=interceptable;row.FindPropertyRelative("strikecraftOnly").boolValue=false;
        row.FindPropertyRelative("color").colorValue=id==26?new Color(.3f,.7f,1):new Color(1,.15f,.05f);
    }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
