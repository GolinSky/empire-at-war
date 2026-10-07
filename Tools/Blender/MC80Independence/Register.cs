using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterMC80Independence
{
    const int SHIP_ID=305;
    const int ABILITY_ID=26;
    const string DATA="Assets/Settings/Data/Ship/MC80IndependenceShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/MC80IndependenceIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/MC80IndependenceMatchups.asset";

    public static string Main()
    {
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="MC80IndependenceMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/VictoryIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Frigates","Assets/Art/Textures/Ui/Icons/ShipIcon/ArquitensIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Bomber strikes","Assets/Art/Textures/Ui/Icons/SquadronIcon/TIEBomberIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="MC80 Independence dreadnought with eight medium dual turbolasers and six medium dual turbo-ion cannons. Three engines and three hangars are targetable. Eight light turbolasers, six light turbo-ions and eight heavy lasers remain operational until hull destruction. Power to Shields accelerates regeneration while reducing movement and firing rate. Carries X-Wing, Y-Wing and A-Wing squadrons.";
        value.FindPropertyRelative("role").stringValue="Dreadnought / Fleet anchor";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="MC80 Independence";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=3;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=34000;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=680;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=14;
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
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/MC80IndependenceReinforcementView.prefab");
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
        value.FindPropertyRelative("settings").managedReferenceValue=new BoostWeaponPowerSettings();
        value.FindPropertyRelative("icon").objectReferenceValue=icon;
        value.FindPropertyRelative("displayName").stringValue="Power to Shields";
        value.FindPropertyRelative("description").stringValue="For 30 seconds, shield regeneration increases eightfold (640/s), movement falls to 80% and weapon fire delays double. Recovers 60 seconds after expiry.";
        value.FindPropertyRelative("duration").floatValue=30;value.FindPropertyRelative("recoveryDelay").floatValue=60;
        var modifier=value.FindPropertyRelative("settings.statModifier");
        modifier.FindPropertyRelative("damageMultiplier").floatValue=1;
        modifier.FindPropertyRelative("fireDelayMultiplier").floatValue=2;
        modifier.FindPropertyRelative("speedMultiplier").floatValue=.8f;
        modifier.FindPropertyRelative("shieldRegenMultiplier").floatValue=8;
        modifier.FindPropertyRelative("damageTakenMultiplier").floatValue=1;
        value.FindPropertyRelative("isToggle").boolValue=false;value.FindPropertyRelative("canCancel").boolValue=false;
        value.FindPropertyRelative("requiresEnemyTarget").boolValue=false;value.FindPropertyRelative("range").floatValue=0;value.FindPropertyRelative("aiUse").intValue=0;Save(catalog);
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var profiles=weapons.FindProperty("weapons");
        Profile(profiles,55,5,"Medium Dual Turbolaser",2,70,2,.08f,8.91f,325,false);
        Profile(profiles,56,10,"Medium Dual Turbo-Ion Cannon",4,75,2,.08f,10.25f,325,false);
        Profile(profiles,57,10,"Light Turbo-Ion Cannon",4,50,1,3,6.25f,250,false);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(var pair in new[]{new[]{55,5},new[]{56,10},new[]{57,10}})
        {
            var sounds=audio.FindProperty("weapons");
            if(Enumerable.Range(0,sounds.arraySize).Any(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==pair[0]))continue;
            int index=Enumerable.Range(0,sounds.arraySize).Single(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==pair[1]);
            sounds.InsertArrayElementAtIndex(index);sounds.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=pair[0];
        }
        var abilitySounds=audio.FindProperty("abilities");
        if(!Enumerable.Range(0,abilitySounds.arraySize).Any(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==ABILITY_ID))
        {
            int index=Enumerable.Range(0,abilitySounds.arraySize).Single(i=>abilitySounds.GetArrayElementAtIndex(i).FindPropertyRelative("abilityId").intValue==17);
            abilitySounds.InsertArrayElementAtIndex(index);abilitySounds.GetArrayElementAtIndex(index).FindPropertyRelative("abilityId").intValue=ABILITY_ID;
        }
        Save(audio);AssetDatabase.SaveAssets();return "MC80 Independence=305 registered with Power to Shields=26, three source-backed weapon profiles, icons, placement and audio.";
    }

    static void Profile(SerializedProperty profiles,int id,int donor,string name,int damageType,float damage,int shots,float interval,float reload,float range,bool interceptable)
    {
        var row=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("weaponType").intValue==id);
        if(row==null){int index=Enumerable.Range(0,profiles.arraySize).Single(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==donor);profiles.InsertArrayElementAtIndex(index);row=profiles.GetArrayElementAtIndex(index);}
        row.FindPropertyRelative("weaponType").intValue=id;row.FindPropertyRelative("displayName").stringValue=name;row.FindPropertyRelative("damageType").intValue=damageType;
        row.FindPropertyRelative("damage").floatValue=damage;row.FindPropertyRelative("shotsPerSalvo").intValue=shots;row.FindPropertyRelative("shotInterval").floatValue=interval;row.FindPropertyRelative("reload").floatValue=reload;row.FindPropertyRelative("range").floatValue=range;
        row.FindPropertyRelative("interceptable").boolValue=interceptable;row.FindPropertyRelative("strikecraftOnly").boolValue=false;
        row.FindPropertyRelative("color").colorValue=(id==56 || id==57)?new Color(.3f,.7f,1):new Color(1,.15f,.05f);
    }
    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
