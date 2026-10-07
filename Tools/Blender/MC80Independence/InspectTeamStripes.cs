using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectMC80TeamStripes
{
    public static string Main()
    {
        var report = JObject.Parse(File.ReadAllText("Temp/MC80IndependenceImport/TeamStripeReport.json"));
        foreach (var record in report["masks"])
        {
            string path = (string)record["mask"];
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (mask.width != 2048 || mask.height != 2048 || importer.sRGBTexture || importer.wrapMode != TextureWrapMode.Repeat || !importer.mipmapEnabled)
                throw new InvalidOperationException("Stripe mask import settings differ: " + path);
            if ((float)record["coverage"] <= .01f || (float)record["coverage"] >= .2f)
                throw new InvalidOperationException("Stripe mask coverage differs: " + path);
        }
        foreach (var path in report["materials"].Values<string>())
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            string expected = material.GetTexture("_BaseMap").name + "_TeamMask";
            if (material.GetTexture("_TeamMaskMap").name != expected || material.GetFloat("_TeamMaskStrength") != 1 || material.GetFloat("_TeamLiveryStrength") != 0)
                throw new InvalidOperationException("Saved hull stripe material differs: " + path);
            var wreck = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Wrecks/MC80Independence/" + material.name + "_Wreck.mat");
            if (wreck.GetTexture("_TeamMaskMap") != material.GetTexture("_TeamMaskMap") || wreck.GetFloat("_TeamMaskStrength") != 1)
                throw new InvalidOperationException("Wreck stripe material differs: " + path);
        }
        var view = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/MC80IndependenceShipView.prefab");
        var binding = view.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == "TeamColorView");
        var array = new SerializedObject(binding).FindProperty("meshRenderers");
        if (array.arraySize != 8) throw new InvalidOperationException("Ownership requires all eight opaque hull renderers.");
        for (int i = 0; i < array.arraySize; i++)
            if (array.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new InvalidOperationException("Ownership renderer is missing.");
        report["savedLiveAndWreckAssignmentsVerified"] = true;
        report["ownershipRenderers"] = array.arraySize;
        File.WriteAllText("Temp/MC80IndependenceImport/TeamStripeReport.json", report.ToString());
        return "Verified four saved linear UV masks, eight live/wreck material pairs and all eight ownership renderers.";
    }
}
