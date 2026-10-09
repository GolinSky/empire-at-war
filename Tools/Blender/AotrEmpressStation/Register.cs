using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterEmpress
{
    private const string VIEW="Assets/Prefabs/Models/DefendStation/AotrEmpressDefensePlatformView.prefab";
    private const string DATA="Assets/Settings/Data/Models/DefendPlatform/AotrEmpressDefensePlatformData.asset";
    private const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/AotrEmpressDefensePlatformReinforcementView.prefab";
    private const int EMPRESS_TYPE=3;
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Registration requires Edit Mode.");
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,49,67,"Heavy Long-Range 3-Burst Turbolaser",150,3,7.5f,15.25f,450);
        Profile(weapons,44,68,"Heavy Long-Range 3-Burst Turbo-Ion Cannon",157.5f,3,8.5f,17.25f,450);
        Profile(weapons,28,69,"Heavy Dual Laser Cannon",17.5f,2,.08f,3.8705f,150);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("weapons"),"weaponType",49,67);
        CloneRow(audio.FindProperty("weapons"),"weaponType",44,68);
        CloneRow(audio.FindProperty("weapons"),"weaponType",28,69); Save(audio);
        var mapping=Load("Assets/Settings/AssetMappingData.asset");
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        foreach(string path in new[]{VIEW,DATA})
        {
            string guid=AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"),Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue=guid;
            string donor=path==VIEW?"Assets/Prefabs/Models/DefendStation/DefendPlatformView.prefab":"Assets/Settings/Data/Models/DefendPlatform/DefendPlatformData.asset";
            var entry=settings.CreateOrMoveEntry(guid,settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address=Path.GetFileNameWithoutExtension(path); EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping); EditorUtility.SetDirty(settings);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/Ui/AotrEmpressDefensePlatformIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey=iconGuid+":"+iconId;
        var catalog=Load("Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
        var unit=CloneRow(catalog.FindProperty("entries.keyValue"),"key",0,EMPRESS_TYPE).FindPropertyRelative("value");
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="XQ-3 Empress";
        unit.FindPropertyRelative("role").stringValue="Shield-heavy ion defense platform";
        unit.FindPropertyRelative("description").stringValue="6,000 hull; 24,000 shields. 3 heavy three-burst long-range turbolasers, 3 heavy three-burst long-range turbo-ion cannons, 6 medium long-range dual turbolasers, 12 light dual turbolasers and 6 heavy dual laser cannons. Emphasizes shields and ion weaponry; Golan III emphasizes hull armor, heavy turbolasers and torpedoes.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue=2;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=1;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue=7500;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue=250;
        unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=3;
        Save(catalog);
        foreach(string faction in new[]{"Empire","Rebellion","Republic","Separatist"})
        {
            var data=Load("Assets/Settings/Data/Factions/"+faction+"/"+faction+"Faction.asset");
            var list=data.FindProperty("defendPlatforms");
            if(!Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).intValue==EMPRESS_TYPE))
            { list.arraySize++; list.GetArrayElementAtIndex(list.arraySize-1).intValue=EMPRESS_TYPE; }
            Save(data);
        }
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var entryPreview=CloneRow(reinforcement.FindProperty("defendPlatformWrapper.keyValue"),"key",0,EMPRESS_TYPE);
        entryPreview.FindPropertyRelative("value").objectReferenceValue=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        Save(reinforcement);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon; Save(tooltip);
        AssetDatabase.SaveAssets();
        return "Registered XQ-3 Empress type 3 in shared catalog and four faction rosters; own gameplay/data/placement mappings, sprite, Addressables and three dedicated weapon profiles.";
    }
    private static void Profile(SerializedObject data,int source,int type,string name,float damage,int shots,float interval,float reload,float range)
    {
        var row=CloneRow(data.FindProperty("weapons"),"weaponType",source,type);
        row.FindPropertyRelative("displayName").stringValue=name;
        row.FindPropertyRelative("damage").floatValue=damage;
        row.FindPropertyRelative("shotsPerSalvo").intValue=shots;
        row.FindPropertyRelative("shotInterval").floatValue=interval;
        row.FindPropertyRelative("reload").floatValue=reload;
        row.FindPropertyRelative("range").floatValue=range;
    }
    private static SerializedObject Load(string path)=>new SerializedObject(AssetDatabase.LoadMainAssetAtPath(path));
    private static void Save(SerializedObject data){data.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(data.targetObject);}
    private static SerializedProperty CloneRow(SerializedProperty list,string key,int source,int target)
    {
        int index=Enumerable.Range(0,list.arraySize).Where(i=>list.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue==target).DefaultIfEmpty(-1).First();
        if(index<0)
        {
            int donor=Enumerable.Range(0,list.arraySize).Single(i=>list.GetArrayElementAtIndex(i).FindPropertyRelative(key).intValue==source);
            list.InsertArrayElementAtIndex(donor); index=donor;
            list.GetArrayElementAtIndex(index).FindPropertyRelative(key).intValue=target;
        }
        return list.GetArrayElementAtIndex(index);
    }
    private static SerializedProperty Entry(SerializedProperty list,string key)
    {
        int index=Enumerable.Range(0,list.arraySize).Where(i=>list.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==key).DefaultIfEmpty(-1).First();
        if(index<0){index=list.arraySize;list.arraySize++;list.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=key;}
        return list.GetArrayElementAtIndex(index).FindPropertyRelative("value");
    }
}
