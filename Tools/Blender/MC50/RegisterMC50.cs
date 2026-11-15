using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterMC50
{
    const int SHIP_ID=311;
    const string DATA="Assets/Settings/Data/Ship/Rebellion/MC50ShipData.asset";
    const string VIEW="Assets/Prefabs/Models/Ships/Rebellion/MC50ShipView.prefab";
    const string ICON="Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/MC50Icon.png";
    const string MATCHUPS="Assets/Settings/Data/Tooltip/Matchups/MC50Matchups.asset";

    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new System.InvalidOperationException("Ship registration requires Edit Mode.");
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>(ICON);
        string iconKey=IconKey(icon);
        if(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(MATCHUPS)==null)
            AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/MonCalCruiserMatchups.asset",MATCHUPS);
        var matchups=Load(MATCHUPS);matchups.targetObject.name="MC50Matchups";
        var strong=matchups.FindProperty("strongAgainst");strong.arraySize=2;
        Matchup(strong.GetArrayElementAtIndex(0),"Light cruisers","Assets/Art/Textures/Ui/Icons/ShipIcon/Rebellion/CorellianCorvetteIcon.png");
        Matchup(strong.GetArrayElementAtIndex(1),"Frigates","Assets/Art/Textures/Ui/Icons/ShipIcon/Republic/ArquitensIcon.png");
        var weak=matchups.FindProperty("weakAgainst");weak.arraySize=1;
        Matchup(weak.GetArrayElementAtIndex(0),"Heavy capital ships","Assets/Art/Textures/Ui/Icons/ShipIcon/Empire/ISDIIcon.png");Save(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var row=Upsert(faction.FindProperty("ships.keyValue"),SHIP_ID);var value=row.FindPropertyRelative("value");
        value.FindPropertyRelative("matchups").objectReferenceValue=matchups.targetObject;
        value.FindPropertyRelative("description").stringValue="General-purpose medium cruiser with two heavy turbolasers, four medium dual turbolasers, two medium ion cannons, four laser cannons and two point-defense lasers. Boost Weapon Power halves weapon reload and time between shots and halves speed and shield regeneration for 7 s. Recovers in 50 s.";
        value.FindPropertyRelative("role").stringValue="General-purpose combat ship";
        value.FindPropertyRelative("iconKey").stringValue=iconKey;value.FindPropertyRelative("isHero").boolValue=false;
        value.FindPropertyRelative("<Name>k__BackingField").stringValue="MC50 Medium Cruiser";
        value.FindPropertyRelative("<MaxCount>k__BackingField").intValue=3;
        value.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=2;
        value.FindPropertyRelative("<Price>k__BackingField").intValue=3400;
        value.FindPropertyRelative("<BuildTime>k__BackingField").intValue=42;
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
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Ui/Reinforcement/MC50ReinforcementView.prefab");
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
        AssetDatabase.SaveAssets();return "MC50=311 registered: Rebellion, ship data/view/placement/icons/matchups.";
    }

    static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path));
    static SerializedProperty Append(SerializedProperty array){array.InsertArrayElementAtIndex(array.arraySize);return array.GetArrayElementAtIndex(array.arraySize-1);}
    static SerializedProperty Upsert(SerializedProperty array,int id){var row=Enumerable.Range(0,array.arraySize).Select(i=>array.GetArrayElementAtIndex(i)).SingleOrDefault(p=>p.FindPropertyRelative("key").intValue==id)??Append(array);row.FindPropertyRelative("key").intValue=id;return row;}
    static void Save(SerializedObject so){so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(so.targetObject);}
    static string IconKey(Sprite sprite){AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long id);return guid+":"+id;}
    static void Matchup(SerializedProperty row,string label,string path){row.FindPropertyRelative("label").stringValue=label;row.FindPropertyRelative("iconKey").stringValue=IconKey(AssetDatabase.LoadAssetAtPath<Sprite>(path));}
}
