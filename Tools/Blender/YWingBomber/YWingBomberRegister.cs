using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using EmpireAtWar.Services.Audio;
using EmpireAtWar.Views.Reinforcement;

public static class YWingBomberRegister
{
    private const string VIEW = "Assets/Prefabs/Models/Squadrons/YWingBomberSquadronView.prefab";
    private const string DATA = "Assets/Settings/Data/Squadron/YWingBomberSquadronData.asset";
    private const string PREVIEW = "Assets/Prefabs/Ui/Reinforcement/YWingBomberReinforcementView.prefab";
    private const string MATCHUPS = "Assets/Settings/Data/Tooltip/Matchups/YWingBomberSquadronMatchups.asset";
    private const int UNIT = 301;

    public static object Main()
    {
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/ShipIcon/YWingBomberIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string guid,out long id); string iconKey=guid+":"+id;
        AssetDatabase.CopyAsset("Assets/Settings/Data/Tooltip/Matchups/YWingSquadronMatchups.asset",MATCHUPS);
        var matchups=AssetDatabase.LoadAssetAtPath<ScriptableObject>(MATCHUPS); matchups.name="YWingBomberSquadronMatchups"; EditorUtility.SetDirty(matchups);
        var faction=Load("Assets/Settings/Data/Factions/Rebellion/RebellionFaction.asset");
        var roster=AddKey(faction.FindProperty("squadrons.keyValue"),UNIT);
        roster.FindPropertyRelative("matchups").objectReferenceValue=matchups;
        roster.FindPropertyRelative("description").stringValue="Six BTL-A4 Y-Wing bombers. Each bomber has 30 hull, 30 slow-charging anti-laser shields and tactical speed 30. One light dual laser, one heavy dual ion stunner and two medium proton-torpedo launchers per craft. Ion Shot disables the target for 3 s on arrival; 20 s recovery. Weapons are not separately targetable.";
        roster.FindPropertyRelative("role").stringValue="Bomber / Anti-ship";
        roster.FindPropertyRelative("iconKey").stringValue=iconKey;
        roster.FindPropertyRelative("isHero").boolValue=false;
        roster.FindPropertyRelative("<Name>k__BackingField").stringValue="BTL-A4 Y-Wing Bomber Squadron";
        roster.FindPropertyRelative("<MaxCount>k__BackingField").intValue=10;
        roster.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=1;
        roster.FindPropertyRelative("<Price>k__BackingField").intValue=500;
        roster.FindPropertyRelative("<BuildTime>k__BackingField").intValue=15;
        roster.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=1;
        roster.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon; Save(faction);

        var mapping=Load("Assets/Settings/AssetMappingData.asset"); var entries=mapping.FindProperty("assetMappings.keyValue");
        foreach(string path in new[]{VIEW,DATA})
        {
            string name=System.IO.Path.GetFileNameWithoutExtension(path);
            AddKey(entries,name).FindPropertyRelative("m_AssetGUID").stringValue=AssetDatabase.AssetPathToGUID(path);
            var settings=AddressableAssetSettingsDefaultObject.Settings;
            string donor=path==VIEW?"Assets/Prefabs/Models/Squadrons/YWingSquadronView.prefab":"Assets/Settings/Data/Squadron/YWingSquadronData.asset";
            var entry=settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path),settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address=name; EditorUtility.SetDirty(settings);
        }
        Save(mapping);
        var ui=Load("Assets/Settings/Data/Models/ShipUi/ShipUiData.asset"); AddKey(ui.FindProperty("squadronIconWrapper.keyValue"),UNIT).objectReferenceValue=icon; Save(ui);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset"); var icons=tooltip.FindProperty("icons");
        icons.InsertArrayElementAtIndex(icons.arraySize); var iconEntry=icons.GetArrayElementAtIndex(icons.arraySize-1);
        iconEntry.FindPropertyRelative("sprite").objectReferenceValue=icon; iconEntry.FindPropertyRelative("key").stringValue=iconKey; Save(tooltip);
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        AddKey(reinforcement.FindProperty("spawnSquadronWrapper.keyValue"),UNIT).objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW).GetComponent<UnitSpawnView>(); Save(reinforcement);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CopyAudio(audio.FindProperty("weapons"),15,46); CopyAudio(audio.FindProperty("weapons"),10,47); CopyAudio(audio.FindProperty("weapons"),6,48); Save(audio);
        AssetDatabase.SaveAssets();
        return new { faction="Rebellion",unit=UNIT,icon=iconKey,view=VIEW,data=DATA,preview=PREVIEW };
    }

    private static void CopyAudio(SerializedProperty list,int source,int target)
    {
        int index=Enumerable.Range(0,list.arraySize).Single(i=>list.GetArrayElementAtIndex(i).FindPropertyRelative("weaponType").intValue==source);
        list.InsertArrayElementAtIndex(index); list.GetArrayElementAtIndex(index).FindPropertyRelative("weaponType").intValue=target;
    }
    private static SerializedObject Load(string path) => new SerializedObject(AssetDatabase.LoadAssetAtPath<ScriptableObject>(path));
    private static void Save(SerializedObject data) { data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(data.targetObject); }
    private static SerializedProperty AddKey(SerializedProperty list,object key)
    {
        for(int i=0;i<list.arraySize;i++)
        {
            var entry=list.GetArrayElementAtIndex(i); var current=entry.FindPropertyRelative("key");
            if(key is int number?current.intValue==number:current.stringValue==(string)key) return entry.FindPropertyRelative("value");
        }
        list.InsertArrayElementAtIndex(list.arraySize); var added=list.GetArrayElementAtIndex(list.arraySize-1); var property=added.FindPropertyRelative("key");
        if(key is int value) property.intValue=value; else property.stringValue=(string)key;
        return added.FindPropertyRelative("value");
    }
}
