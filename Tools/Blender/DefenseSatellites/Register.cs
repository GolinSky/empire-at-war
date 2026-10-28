using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

public static class RegisterSatellites
{
    public static string Main()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Satellite registration requires Edit Mode.");
        string[] names={"QuadLaserSatellite","TurbolaserSatellite","IonSatellite","MassDriverSatellite","MissileSatellite"};
        int[] costs={300,500,400,1600,1000}; int[] times={21,25,23,47,35};
        for(int n=0;n<names.Length;n++)
        {
        string name=names[n];
        string view="Assets/Prefabs/Models/DefendStation/"+name+"View.prefab";
        string dataPath="Assets/Settings/Data/Models/DefendPlatform/"+name+"Data.asset";
        var mapping=Load("Assets/Settings/AssetMappingData.asset");
        var settings=AddressableAssetSettingsDefaultObject.Settings;
        foreach(string path in new[]{view,dataPath})
        {
            string guid=AssetDatabase.AssetPathToGUID(path);
            Entry(mapping.FindProperty("assetMappings.keyValue"),Path.GetFileNameWithoutExtension(path)).FindPropertyRelative("m_AssetGUID").stringValue=guid;
            string donor=path==view?"Assets/Prefabs/Models/DefendStation/DefendPlatformView.prefab":"Assets/Settings/Data/Models/DefendPlatform/DefendPlatformData.asset";
            var entry=settings.CreateOrMoveEntry(guid,settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(donor)).parentGroup);
            entry.address=Path.GetFileNameWithoutExtension(path); EditorUtility.SetDirty(entry.parentGroup);
        }
        Save(mapping); EditorUtility.SetDirty(settings);
        var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/DefendPlatform/"+name+"Icon.png");
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(icon,out string iconGuid,out long iconId);
        string iconKey=iconGuid+":"+iconId;
        var catalog=Load("Assets/Settings/Data/Factions/Shared/DefendPlatformCatalog.asset");
        var unit=CloneRow(catalog.FindProperty("entries.keyValue"),"key",0,(n+4)).FindPropertyRelative("value");
        unit.FindPropertyRelative("<Name>k__BackingField").stringValue=name.Replace("Satellite"," Satellite");
        unit.FindPropertyRelative("role").stringValue="Stationary mining-point defense";
        unit.FindPropertyRelative("description").stringValue="1,000 hull, 1,000 shields; six targetable turrets. Built on captured satellite pads.";
        unit.FindPropertyRelative("iconKey").stringValue=iconKey;
        unit.FindPropertyRelative("<Icon>k__BackingField").objectReferenceValue=icon;
        unit.FindPropertyRelative("<MaxCount>k__BackingField").intValue=0;
        unit.FindPropertyRelative("<AvailableLevel>k__BackingField").intValue=1;
        unit.FindPropertyRelative("<Price>k__BackingField").intValue=costs[n];
        unit.FindPropertyRelative("<BuildTime>k__BackingField").intValue=times[n];
        unit.FindPropertyRelative("<UnitCapacity>k__BackingField").intValue=0;
        Save(catalog);
        var tooltip=Load("Assets/Settings/Data/Tooltip/TooltipIconData.asset");
        var icons=tooltip.FindProperty("icons");
        int index=Enumerable.Range(0,icons.arraySize).Where(i=>icons.GetArrayElementAtIndex(i).FindPropertyRelative("key").stringValue==iconKey).DefaultIfEmpty(-1).First();
        if(index<0){index=icons.arraySize;icons.arraySize++;}
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("key").stringValue=iconKey;
        icons.GetArrayElementAtIndex(index).FindPropertyRelative("sprite").objectReferenceValue=icon; Save(tooltip);
        AssetDatabase.SaveAssets();
        }
        return "Registered five satellites in catalog, asset mapping, existing Addressables groups and tooltip icons.";
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
