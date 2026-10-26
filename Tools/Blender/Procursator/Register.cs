using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterProcursator
{
    const int SHIP_ID=212;
    const string DATA="Assets/Settings/Data/Ship/Empire/ProcursatorShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/Empire/ProcursatorShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ProcursatorIcon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/ProcursatorMatchups.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Ship registration requires Edit Mode.");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="ProcursatorMatchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Cruisers","Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/CorellianCorvetteIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/Republic/ArquitensIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Bomber strikes","Assets/Art/Textures/Ui/Icons/SquadronIcon/Empire/TIEBomberIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Empire/EmpireFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="Combat destroyer: three quad heavy turbolasers, two medium dual turbolasers, eight light triple turbolasers and two medium ion cannons. No fighter complement. Boost Weapon Power increases battery damage and reload speed.";
        value.FindPropertyRelative("role").stringValue="Anti-frigate / Capital-ship support";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="Procursator-class Star Destroyer";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=3;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=5500;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=55;
        value.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=4;
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
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/ProcursatorReinforcementView.prefab");
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
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        var profiles=weapons.FindProperty("weapons");
        if(!Enumerable.Range(0,profiles.arraySize).Any(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==86))
        {
            int index=Enumerable.Range(0,profiles.arraySize).Single(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==1);
            profiles.InsertArrayElementAtIndex(index); profiles.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=86;
        }
        row=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("weaponType").intValue==86);
        row.FindPropertyRelative("displayName").stringValue="Light Triple Turbolaser";
        row.FindPropertyRelative("mount").intValue=3;
        row.FindPropertyRelative("reason").stringValue="Procursator requested eight light triple batteries; existing light turbolaser family and triple mount.";
        row.FindPropertyRelative("scope").intValue=2;row.FindPropertyRelative("unit").stringValue="Procursator";
        row.FindPropertyRelative("overrideColor").boolValue=true;row.FindPropertyRelative("color").colorValue=new Color(.15f,1,.25f,1);
        if(!Enumerable.Range(0,profiles.arraySize).Any(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==87))
        {
            int index=Enumerable.Range(0,profiles.arraySize).Single(i=>profiles.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==20);
            profiles.InsertArrayElementAtIndex(index);profiles.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=87;
        }
        row=Enumerable.Range(0,profiles.arraySize).Select(i=>profiles.GetArrayElementAtIndex(i)).Single(p=>p.FindPropertyRelative("weaponType").intValue==87);
        row.FindPropertyRelative("displayName").stringValue="Procursator Quad Heavy Turbolaser";
        row.FindPropertyRelative("unit").stringValue="Procursator";
        row.FindPropertyRelative("reason").stringValue="Procursator main axial battery; heavy long-range quad profile, isolated from Mandator ownership.";
        row.FindPropertyRelative("overrideColor").boolValue=true;row.FindPropertyRelative("color").colorValue=new Color(.15f,1,.25f,1);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");var sounds=audio.FindProperty("weapons");
        if(!Enumerable.Range(0,sounds.arraySize).Any(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==86))
        {int index=Enumerable.Range(0,sounds.arraySize).Single(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==1);sounds.InsertArrayElementAtIndex(index);sounds.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=86;}
        if(!Enumerable.Range(0,sounds.arraySize).Any(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==87))
        {int index=Enumerable.Range(0,sounds.arraySize).Single(i=>sounds.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==20);sounds.InsertArrayElementAtIndex(index);sounds.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=87;}
        Save(audio);AssetDatabase.SaveAssets();return "Procursator=212 registered with own assets, no hangar and existing Boost Weapon Power=5.";
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
