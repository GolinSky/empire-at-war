using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class VerifyTibannaArt
{
    public static string Main()
    {
        string[] paths = {"Assets/Prefabs/Models/MiningFacilities/TibannaGasStation.prefab", "Assets/Prefabs/Models/MiningFacilities/TibannaGasStationView.prefab", "Assets/Prefabs/Ui/Reinforcement/TibannaGasStationReinforcementView.prefab", "Assets/Prefabs/Models/Wrecks/TibannaGasStationWreckView.prefab"};
        var rows = paths.Select(path =>
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) throw new InvalidOperationException("Missing prefab " + path);
            if (root.transform.localScale != Vector3.one || root.transform.localPosition != Vector3.zero || root.transform.localRotation != Quaternion.identity) throw new InvalidOperationException("Nonidentity root " + path);
            foreach (var component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null) throw new InvalidOperationException("Missing script " + path);
                var config = new SerializedObject(component); var property = config.GetIterator();
                while (property.Next(true)) if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0) throw new InvalidOperationException("Missing reference " + path + ":" + property.propertyPath);
            }
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                if (renderer.GetComponents<MeshFilter>().Single().sharedMesh == null || renderer.sharedMaterials.Any(m => m == null || m.shader == null)) throw new InvalidOperationException("Missing mesh/material " + path);
                if (renderer.name == "Shadow" || renderer.name == "Col_Main") throw new InvalidOperationException("Helper renderer retained " + path);
            }
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var config = new SerializedObject(component);
                foreach (string name in new[] {"renderers", "meshRenderers", "meshFilters", "explosionHullRenderers"})
                {
                    var array = config.FindProperty(name); if (array == null) continue;
                    if (array.arraySize == 0) throw new InvalidOperationException("Empty binding " + path + ":" + name);
                    for (int i = 0; i < array.arraySize; i++) if (array.GetArrayElementAtIndex(i).objectReferenceValue == null) throw new InvalidOperationException("Unbound renderer " + path + ":" + name);
                }
            }
            if (path.Contains("Reinforcement"))
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
                if (renderers.Any(r => r.sharedMaterials.Any(m => m != material))) throw new InvalidOperationException("Placement hologram mismatch");
            }
            return new {path, renderers = renderers.Length, triangles = renderers.Sum(r => r.GetComponents<MeshFilter>().Single().sharedMesh.triangles.Length / 3)};
        }).ToArray();
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Textures/Ui/Icons/Ui/TibannaGasStationIcon.png");
        if (sprite == null || sprite.texture.width != 512 || sprite.texture.height != 512) throw new InvalidOperationException("Invalid icon");
        string report = JsonConvert.SerializeObject(rows, Formatting.Indented); File.WriteAllText("output/TibannaGasStationImport/ArtVerification.json", report); return report;
    }
}
