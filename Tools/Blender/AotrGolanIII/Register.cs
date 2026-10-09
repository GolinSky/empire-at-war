using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterGolan
{
    private const string VIEW="Assets/Prefabs/Models/DefendStation/AotrGolanIIIDefensePlatformView.prefab";
    private const string DATA="Assets/Settings/Data/Models/DefendPlatform/AotrGolanIIIDefensePlatformData.asset";
    private const string PREVIEW="Assets/Prefabs/Ui/Reinforcement/AotrGolanIIIDefensePlatformReinforcementView.prefab";
    private const int GOLAN_TYPE=2;
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Registration requires Edit Mode.");
        var weapons=Load("Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        Profile(weapons,63,65,"Heavy Assault Missile Launcher",50,3,1.5f,15,300);
        Profile(weapons,6,66,"Heavy Proton Torpedo Launcher",150,1,0,10,200);
        Save(weapons);
        var audio=Load("Assets/Settings/Data/Models/Audio/ShipSfxData.asset");
        CloneRow(audio.FindProperty("weapons"),"weaponType",63,65);
        CloneRow(audio.FindProperty("weapons"),"weaponType",6,66); Save(audio);
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
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/Ui/AotrGolanIIIDefensePlatformIcon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey=iconGuid+":"+iconId;
        var catalog=Load("Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
        var unit=CloneRow(catalog.FindProperty("entries.keyValue"),"key",0,GOLAN_TYPE).FindPropertyRelative("value");
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue="Golan III";
        unit.FindPropertyRelative("role").stringValue="Heavy anti-capital defensive station";
        unit.FindPropertyRelative("description").stringValue="10,000 hull; 12,000 shields. 12 heavy dual turbolasers, 4 heavy assault missile launchers, 12 heavy proton torpedo launchers and 8 laser cannons. Powerful long-range firepower and torpedoes; comparatively vulnerable to concentrated bomber attacks.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue=2;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=1;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue=8250;
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue=275;
        unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=3;
        Save(catalog);
        foreach(string faction in new[]{"Empire","Rebellion","Republic","Separatist"})
        {
            var data=Load("Assets/Settings/Data/Factions/"+faction+"/"+faction+"Faction.asset");
            var list=data.FindProperty("defendPlatforms");
            if(!Enumerable.Range(0,list.arraySize).Any(i=>list.GetArrayElementAtIndex(i).intValue==GOLAN_TYPE))
            { list.arraySize++; list.GetArrayElementAtIndex(list.arraySize-1).intValue=GOLAN_TYPE; }
            Save(data);
        }
        var preview=AssetDatabase.LoadAssetAtPath<GameObject>(PREVIEW);
        var reinforcement=Load("Assets/Settings/Data/Reinforcement/ReinforcementData.asset");
        var entryPreview=CloneRow(reinforcement.FindProperty("defendPlatformWrapper.keyValue"),"key",0,GOLAN_TYPE);
        entryPreview.FindPropertyRelative("value").objectReferenceValue=preview.GetComponents<MonoBehaviour>().Single(m=>m.GetType().Name=="UnitSpawnView");
        Save(reinforcement);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon; Save(tooltip);
        AssetDatabase.SaveAssets();
        return "Registered Golan III type 2 in shared catalog and four faction rosters; own gameplay/data/placement mappings, sprite, Addressables and two source-backed heavy launcher profiles. Existing entries retained.";
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
