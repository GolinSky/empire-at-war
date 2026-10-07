using System;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyRebelStations
{
    private const string TASK = "Temp/RebelStationImport/";
    private const string PREFABS = "Assets/Prefabs/Models/Stations/";

    public static string Main()
    {
        var reports = JArray.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var results = new JArray();
        foreach (var report in reports)
        {
            int level = (int)report["level"];
            string name = (string)report["name"];
            string modelPath = "Assets/Art/Models/SpaceStations/RebelSpaceStation/Level" + level + "/" + name + ".fbx";
            var raw = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            var bones = raw.GetComponentsInChildren<Transform>(true).Where(t => t.GetComponent<Renderer>() == null && report["source"]["bones"][t.name] != null).ToArray();
            if (bones.Length != ((JObject)report["source"]["bones"]).Count) throw new InvalidOperationException("Bone count changed: " + name);
            var boneReport = new JObject();
            foreach (var bone in bones)
            {
                var expected = report["source"]["bones"][bone.name];
                string parent = (string)expected["parent"];
                if (parent != null && bone.parent.name != parent) throw new InvalidOperationException("Bone parent changed: " + name + "/" + bone.name);
                boneReport[bone.name] = new JObject { ["parent"] = parent, ["position"] = V(bone.position) };
            }
            var meshReport = new JObject();
            foreach (var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (renderer.transform.parent.name != (string)report["source"]["meshes"][renderer.name]["parent"])
                    throw new InvalidOperationException("Mesh parent changed: " + name + "/" + renderer.name);
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                meshReport[renderer.name] = new JObject {
                    ["vertices"] = new JArray(mesh.vertices.Select(v => V(renderer.transform.TransformPoint(v)))),
                    ["uv"] = new JArray(mesh.uv.Select(v => new JArray(v.x, v.y))),
                    ["triangles"] = new JArray(mesh.triangles),
                    ["materials"] = new JArray(renderer.sharedMaterials.Select(AssetDatabase.GetAssetPath))
                };
            }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFABS + name + ".prefab");
            var model = prefab.GetComponent<StationLevelModel>();
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing script: " + name);
                var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(transform);
                if (original == null || transform.GetComponent<Renderer>() != null || report["source_bone_names"][original.name] == null) continue;
                if (transform.name != (string)report["source_bone_names"][original.name]) throw new InvalidOperationException("Source bone name lost: " + original.name);
            }
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (importer.optimizeGameObjects || importer.weldVertices || importer.meshCompression != ModelImporterMeshCompression.Off)
                throw new InvalidOperationException("Unexpected model import settings: " + name);
            if (model.Mounts.Any(m => m.Point == null || m.Art == null) || model.LaunchExit == null || model.ShieldMesh == null) throw new InvalidOperationException("Incomplete station bindings: " + name);
            results.Add(new JObject { ["level"] = level, ["bones"] = boneReport, ["meshes"] = meshReport });
        }
        foreach (string path in Enumerable.Range(1, 5).Select(i => PREFABS + "RebelSpaceStationLevel" + i + ".prefab").Append(PREFABS + "RebellionSpaceStationView.prefab"))
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new InvalidOperationException("Missing script: " + path);
                    var iterator = new SerializedObject(component).GetIterator();
                    while (iterator.Next(true))
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference && iterator.objectReferenceValue == null && iterator.objectReferenceInstanceIDValue != 0)
                            throw new InvalidOperationException("Broken reference: " + path + "/" + iterator.propertyPath);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        File.WriteAllText(TASK + "UnityGeometry.json", results.ToString(Newtonsoft.Json.Formatting.None));
        return "Five FBXs retain all converted bones/parents; six prefabs reload without missing scripts or broken references. Geometry exported for independent binary comparison.";
    }
    private static JArray V(Vector3 vector) => new JArray(vector.x, vector.y, vector.z);
}
