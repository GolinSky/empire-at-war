using System;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Station;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildRebelStationAttachments
{
    private const string TASK = "Temp/RebelStationImport/";
    private const string MODELS = "Assets/Art/Models/SpaceStations/RebelSpaceStation/Attachments/";
    private const string PREFABS = "Assets/Prefabs/Models/Stations/";
    private const string MATERIALS = "Assets/Art/Materials/Models/SpaceStations/RebelSpaceStation/RebelSpaceStation_RebelStationSlot";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Station attachment import requires Edit Mode.");
        AssetDatabase.Refresh();
        var reports = JArray.Parse(File.ReadAllText(TASK + "AttachmentConversionReport.json"));
        var levels = JObject.Parse(File.ReadAllText("Tools/Blender/RebelSpaceStation/Attachments.json"));
        var source = JObject.Parse(File.ReadAllText(TASK + "SourceAudit.json"));
        foreach (var report in reports)
        {
            string name = (string)report["name"];
            var importer = (ModelImporter)AssetImporter.GetAtPath(MODELS + name + ".fbx");
            importer.globalScale = .02f;
            importer.importAnimation = false;
            importer.isReadable = true;
            importer.weldVertices = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.optimizeGameObjects = false;
            importer.preserveHierarchy = true;
            foreach (var material in report["materials"])
            {
                string shader = (string)material["shader"];
                string slot = shader == "MeshBumpColorize.fx" ? "01" : shader == "MeshAdditive.fx" ? "02" : "03";
                importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), (string)material["name"]),
                    AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + slot + ".mat"));
            }
            importer.SaveAndReimport();
        }
        var result = new JArray();
        for (int level = 1; level <= 5; level++)
        {
            string name = "RebelSpaceStationLevel" + level;
            string path = PREFABS + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var model = root.transform.Find(name);
                var oldArt = root.GetComponentsInChildren<Transform>(true)
                    .Where(t => reports.Any(r => (string)r["name"] == t.name)).ToArray();
                foreach (var art in oldArt) UnityEngine.Object.DestroyImmediate(art.gameObject);
                var bones = (JArray)source["models"][name]["bones"];
                var matrices = new Matrix4x4[bones.Count];
                for (int index = 0; index < bones.Count; index++)
                {
                    var matrix = Matrix4x4.identity;
                    for (int row = 0; row < 3; row++)
                        for (int column = 0; column < 4; column++) matrix[row, column] = (float)bones[index]["matrix"][row * 4 + column];
                    long parent = (long)bones[index]["parent_index"];
                    matrices[index] = parent == uint.MaxValue ? matrix : matrices[parent] * matrix;
                }
                var transforms = model.GetComponentsInChildren<Transform>(true);
                foreach (var attachment in levels[level.ToString()])
                {
                    string asset = (string)attachment["model"];
                    string anchorName = (string)attachment["bone"];
                    var anchor = transforms.Single(t => t.name.Equals(anchorName, StringComparison.OrdinalIgnoreCase));
                    int index = Enumerable.Range(0, bones.Count).Single(i => ((string)bones[i]["name"]).Equals(anchorName, StringComparison.OrdinalIgnoreCase));
                    var pose = ToUnity(matrices[index]);
                    var attachmentRoot = new GameObject(asset).transform;
                    attachmentRoot.SetParent(model, false);
                    attachmentRoot.SetLocalPositionAndRotation(pose.GetColumn(3), pose.rotation);
                    attachmentRoot.localScale = pose.lossyScale;
                    attachmentRoot.SetParent(anchor, true);
                    var raw = AssetDatabase.LoadAssetAtPath<GameObject>(MODELS + asset + ".fbx");
                    var report = reports.Single(r => (string)r["name"] == asset);
                    foreach (var renderer in raw.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        if ((bool)report["source"]["meshes"][renderer.name]["hidden"]) continue;
                        var geometry = new GameObject(renderer.name);
                        geometry.transform.SetParent(attachmentRoot, false);
                        geometry.transform.SetLocalPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                        geometry.transform.localScale = renderer.transform.lossyScale;
                        geometry.AddComponent<MeshFilter>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                        geometry.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                    }
                }
                var renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
                var points = renderers.SelectMany(r => r.GetComponent<MeshFilter>().sharedMesh.vertices.Select(r.transform.TransformPoint)).ToArray();
                Bounds bounds = new Bounds(points[0], Vector3.zero);
                foreach (var point in points.Skip(1)) bounds.Encapsulate(point);
                model.localPosition -= bounds.center;
                PrefabUtility.RecordPrefabInstancePropertyModifications(model);
                bounds.center = Vector3.zero;
                var serialized = new SerializedObject(root.GetComponent<StationLevelModel>());
                var hull = renderers.Where(r => r.sharedMaterials.Any(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
                var property = serialized.FindProperty("<HullRenderers>k__BackingField");
                property.arraySize = hull.Length;
                for (int i = 0; i < hull.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = hull[i];
                serialized.FindProperty("<HullBounds>k__BackingField").boundsValue = bounds;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                result.Add(new JObject { ["level"] = level, ["attachments"] = ((JArray)levels[level.ToString()]).Count,
                    ["hullRenderers"] = hull.Length, ["size"] = new JArray(bounds.size.x, bounds.size.y, bounds.size.z) });
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText(TASK + "AttachmentArtReport.json", result.ToString());
        return result.ToString();
    }

    private static Matrix4x4 ToUnity(Matrix4x4 source)
    {
        var basis = new Matrix4x4(new Vector4(-1, 0, 0, 0), new Vector4(0, 0, -1, 0), new Vector4(0, 1, 0, 0), new Vector4(0, 0, 0, 1));
        var result = basis * source * basis.inverse;
        Vector4 position = result.GetColumn(3);
        result.SetColumn(3, new Vector4(position.x * .02f, position.y * .02f, position.z * .02f, 1));
        return result;
    }
}
