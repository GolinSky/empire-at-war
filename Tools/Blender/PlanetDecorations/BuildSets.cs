using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using EmpireAtWar.Entities.Map;

public static class BuildPlanetDecorationSets
{
    const string ROOT = "Assets/Settings/Data/Models/Map/Decorations/";
    public static string Main()
    {
        Directory.CreateDirectory(ROOT);
        AssetDatabase.Refresh();
        var report = JArray.Parse(File.ReadAllText("Temp/PlanetDecorationsImport/UnityReport.json"));
        var docks = Set("ShipyardDocks", new[] {"EB_DOCK00","EB_DOCK_BREAK"}, new[] {3,1}, new[] {5,2}, .1f,.14f, report);
        var junk = Set("SpaceJunk", new[] {"W_SPACEJUNK","W_SPACEJUNK_MED","W_SPACEJUNK_SML"}, new[] {1,1,1}, new[] {2,3,3}, .06f,.1f, report);
        var containers = Set("ResourceContainers", new[] {"W_ORBITAL_RESOURCE_CONTAINER"}, new[] {3}, new[] {6}, .5f,.8f, report);
        var profile = AssetDatabase.LoadAssetAtPath<PlanetMapProfile>("Assets/Settings/Data/Models/Map/PlanetMapProfiles.asset");
        var so = new SerializedObject(profile);
        var entries = so.FindProperty("entries");
        for (int i=0;i<entries.arraySize;i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var planet = entry.FindPropertyRelative("<Planet>k__BackingField");
            string name = planet.enumNames[planet.enumValueIndex];
            PlanetDecorationSet set = null;
            if (name == "Kuat") set = docks;
            if (new[] {"Tatooine","NalHutta","Ryloth","Saleucami","Taris","Atzerri"}.Contains(name)) set = junk;
            if (new[] {"Kessel","Bonadan","Thyferra","Muunilinst","Sullust","Bespin"}.Contains(name)) set = containers;
            entry.FindPropertyRelative("<Decorations>k__BackingField").objectReferenceValue = set;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return "Saved three sets and planet assignments";
    }
    static PlanetDecorationSet Set(string name, string[] names, int[] min, int[] max, float minScale, float maxScale, JArray report)
    {
        string path = ROOT + name + ".asset";
        var set = AssetDatabase.LoadAssetAtPath<PlanetDecorationSet>(path);
        if (set == null) { set = ScriptableObject.CreateInstance<PlanetDecorationSet>(); AssetDatabase.CreateAsset(set,path); }
        var so = new SerializedObject(set);
        var entries = so.FindProperty("entries"); entries.arraySize = names.Length;
        for (int i=0;i<names.Length;i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("<Prefab>k__BackingField").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Other/PlanetDecorations/" + names[i] + ".prefab");
            entry.FindPropertyRelative("<MinimumCount>k__BackingField").intValue = min[i];
            entry.FindPropertyRelative("<MaximumCount>k__BackingField").intValue = max[i];
            entry.FindPropertyRelative("<MinimumScale>k__BackingField").floatValue = minScale;
            entry.FindPropertyRelative("<MaximumScale>k__BackingField").floatValue = maxScale;
            entry.FindPropertyRelative("<HeightJitter>k__BackingField").floatValue = 15;
            entry.FindPropertyRelative("<ClearanceRadius>k__BackingField").floatValue = (float)report.Single(r => (string)r["name"] == names[i])["radius"] + 5;
        }
        so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(set); return set;
    }
}
