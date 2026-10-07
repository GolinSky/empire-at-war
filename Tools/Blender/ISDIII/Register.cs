using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.ShipAbilities.Abilities;

public static class RegisterISDIII
{
    const string VIEW="Assets/Prefabs/Models/Ships/ISDIIIShipView.prefab";
    const string DATA="Assets/Settings/Data/Ship/ISDIIIShipData.asset";
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("ISD III registration requires Edit Mode.");
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,3,43,"Heavy Dual Turbolaser",100,2,.08f,10.91f,500,40,3);
        Profile(weapons,31,44,"Heavy Long-Range 2-Burst Turbo-Ion Cannon",210,2,6,12.25f,500,40,4);
        Profile(weapons,12,45,"Composite Beam",1000,1,0,45,375,0,12);
        var beam=CloneRow(weapons.FindProperty("weapons"),"weaponType",12,45);
        beam.FindPropertyRelative("color").colorValue=new Color(1,.4f,.05f);
        beam.FindPropertyRelative("size").vector3Value=Vector3.one*1.5f;
        Save(weapons);
        var matrix=Load("Assets/Settings/Data/Models/Weapon/DamageMatrixData.asset");
        var damage=CloneRow(matrix.FindProperty("damageTypes"),"damageType",7,12);
        foreach(string name in new[]{"fighter","bomber","interceptor","corvette","frigate","capital","heavyCapital","structure"})
            damage.FindPropertyRelative("damage."+name).floatValue=1;
        damage.FindPropertyRelative("vsShield").floatValue=.1f;
        damage.FindPropertyRelative("shieldPiercing").boolValue=false;Save(matrix);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        foreach(var pair in new[]{new{source=3,target=43},new{source=31,target=44},new{source=12,target=45}})
            CloneRow(audio.FindProperty("weapons"),"weaponType",pair.source,pair.target);
        CloneRow(audio.FindProperty("abilities"),"abilityId",1,25);Save(audio);
        var abilities=Load("Assets/Settings/Data/Models/ShipAbilities/ShipAbilityCatalog.asset");
        var ability=CloneRow(abilities.FindProperty("definitions.keyValue"),"key",1,25).FindPropertyRelative("value");
        ability.FindPropertyRelative("settings").managedReferenceValue=new CompositeBeamSettings();
        abilities.ApplyModifiedPropertiesWithoutUndo();abilities.Update();
        ability=Entry(abilities.FindProperty("definitions.keyValue"),25);
        ability.FindPropertyRelative("settings.damage").floatValue=1000;
        ability.FindPropertyRelative("displayName").stringValue="Fire Composite Beam";
        ability.FindPropertyRelative("description").stringValue="Fire the composite beam for 6 seconds at an enemy ship larger than a corvette. Deals 1,000 damage before armor/shield modifiers, with only 10% damage against shields. Recovers in 45 seconds.";
        ability.FindPropertyRelative("duration").floatValue=6;ability.FindPropertyRelative("recoveryDelay").floatValue=45;
        ability.FindPropertyRelative("range").floatValue=375;ability.FindPropertyRelative("requiresEnemyTarget").boolValue=true;
        ability.FindPropertyRelative("canCancel").boolValue=false;ability.FindPropertyRelative("isToggle").boolValue=false;
        ability.FindPropertyRelative("aiUse").intValue=2;Save(abilities);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/ISDIIIIcon.png");
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
        Entry(ships.FindProperty("shipsData.keyValue"),206).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(DATA);Save(ships);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset");Entry(ui.FindProperty("shipIconWrapper.keyValue"),206).objectReferenceValue=icon;Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon;Save(tooltip);
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ISDIIIReinforcementView.prefab");
        var spawn=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");Entry(reinforcement.FindProperty("spawnShipWrapper.keyValue"),206).objectReferenceValue=spawn;Save(reinforcement);
        const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/ISDIIIMatchups.asset";
        if(!File.Exists(MATCHUPS))AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/ISDIShipMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="ISDIIIMatchups";Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var unit=Entry(faction.FindProperty("ships.keyValue"),206);
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="ISD III";
        unit.FindPropertyRelative("role").stringValue="Ship of the Line / Capital Ship";
        unit.FindPropertyRelative("description").stringValue="28,000 hull; 18,000 shields; speed 25. Ten targetable heavy dual turbolasers and two heavy long-range 2-burst turbo-ion cannons; two shield generators, three engines, hangar and tractor beam. Five medium 3-burst turbolasers, four medium turbolasers, four medium turbo-ions, six heavy lasers and composite beam are not separately targetable. Fire Composite Beam targets ships larger than corvettes and has poor shield damage. Tractor Beam restricts enemy movement. TIE Avenger and TIE Punisher complement.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;unit.FindPropertyRelative("isHero").boolValue=false;
        foreach(var field in new[]{new{key="MaxCount",value=10},new{key="AvailableLevel",value=4},new{key="Price",value=26000},new{key="BuildTime",value=520},new{key="UnitCapacity",value=9}})
            unit.FindPropertyRelative("<"+field.key+">k__BackingField").intValue=field.value;
        Save(faction);AssetDatabase.SaveAssets();
        return "Registered Empire ship 206, own icons/data/placement/Addressables, three weapon profiles, Composite Beam 25, armor/shield damage profile and audio.";
    }
    static void Profile(SerializedObject data,int source,int type,string name,float damage,int shots,float interval,float reload,float range,float speed,int damageType)
    {
        var row=CloneRow(data.FindProperty("weapons"),"weaponType",source,type);
        row.FindPropertyRelative("displayName").stringValue=name;row.FindPropertyRelative("damage").floatValue=damage;
        row.FindPropertyRelative("damageType").intValue=damageType;row.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        row.FindPropertyRelative("shotInterval").floatValue=interval;row.FindPropertyRelative("reload").floatValue=reload;
        row.FindPropertyRelative("range").floatValue=range;row.FindPropertyRelative("projectileSpeed").floatValue=speed;
        row.FindPropertyRelative("interceptable").boolValue=false;row.FindPropertyRelative("color").colorValue=type==44?new Color(.4f,.87f,.99f):new Color(.2f,1,.25f);
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
