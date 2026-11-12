using System.IO;
using Newtonsoft.Json.Linq;
using EmpireAtWar.Services.VContainer;
using UnityEditor;
using UnityEngine;

public static class OrientPlanetSurfaces
{
    public static string Main()
    {
        int changed = 0;
        var manifest = JObject.Parse(File.ReadAllText("Tools/PlanetTextures/OriginalPlanetSources.json"));
        foreach (JToken planet in manifest["planets"])
        {
            string name = (string)planet["name"];
            if ((bool)planet["preserve_existing"] || name == "Bespin" || name == "Manaan") continue;
            string path = "Assets/Prefabs/View/Planets/" + name + "Planet.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                using (var view = new SerializedObject(root.GetComponent<PlanetScope>().View))
                {
                    Quaternion rotation = Quaternion.Inverse(root.transform.localRotation) * Quaternion.Euler(15, 0, 0);
                    foreach (string field in new[] { "planetRenderers", "cloudRenderers" })
                    {
                        var renderers = view.FindProperty(field);
                        for (int i = 0; i < renderers.arraySize; i++)
                            ((Renderer)renderers.GetArrayElementAtIndex(i).objectReferenceValue).transform.localRotation = rotation;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changed++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return "Saved equatorial surface/cloud orientation for " + changed + " planets; approved planets and authored existing planets preserved.";
    }
}
