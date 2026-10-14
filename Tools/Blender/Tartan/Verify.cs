using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyTartan
{
    public static object Main()
    {
        var reports = JArray.Parse(File.ReadAllText("Temp/TartanImport/ConversionReport.json"));
        int meshes = 0, triangles = 0, bones = 0;
        foreach (var report in reports)
        {
            string name = (string)report["name"];
            var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/EmpireShips/" + name + "/" + name + ".fbx");
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                var row = report["source"]["meshes"][filter.name];
                if (row == null || filter.sharedMesh.triangles.Length != (int)row["triangles"] * 3)
                    throw new InvalidOperationException("Geometry mismatch: " + filter.name);
                var renderer = filter.GetComponents<MeshRenderer>().Single();
                if (renderer.sharedMaterials.Length != 1 || renderer.sharedMaterials[0] == null)
                    throw new InvalidOperationException("Material-slot mismatch: " + filter.name);
                meshes++; triangles += filter.sharedMesh.triangles.Length / 3;
            }
            foreach (var pair in ((JObject)report["source"]["bones"]).Properties())
            {
                var bone = model.GetComponentsInChildren<Transform>(true).Single(t => t.name == pair.Name && t.GetComponents<MeshFilter>().Length == 0);
                var head = (JArray)pair.Value["head"];
                // Blender Z-up / FBX Unity Y-up conversion, importer scale 0.02.
                var expected = new Vector3(-(float)head[0], (float)head[2], -(float)head[1]) * .02f;
                if (Vector3.Distance(bone.position, expected) > .0001f)
                    throw new InvalidOperationException("Bone position mismatch: " + pair.Name + " " + bone.position + " expected " + expected);
                bones++;
            }
        }
        foreach (string path in new[] {"Assets/Prefabs/Models/Ships/Empire/Tartan.prefab", "Assets/Prefabs/Models/Ships/Empire/TartanShipView.prefab", "Assets/Prefabs/Models/Wrecks/TartanWreckView.prefab", "Assets/Prefabs/Ui/Reinforcement/TartanReinforcementView.prefab"})
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing script: " + path);
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None)
                        throw new InvalidOperationException("Missing reference: " + path + " " + property.propertyPath);
            }
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                if (renderer.sharedMaterials.Any(m => m == null))
                    throw new InvalidOperationException("Missing material: " + path + " " + renderer.name);
        }
        var result = new {meshes, triangles, bones, Prefabs = 4, SavedReferences = "passed"};
        File.WriteAllText("Temp/TartanImport/UnityVerification.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
