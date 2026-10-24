using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// Mesh pass (2026-10-24): shipped models with mesh compression Off, excluding MeshCollider sources and blend-shape
// meshes. Tries Medium, then Low, and restores Off when neither passes. Gate per model: same mesh/vertex counts,
// max position error <= 0.05 % of the model bounds diagonal, max UV error <= 1/4096, max normal error <= 2 degrees.
// Results append to output/DesktopAssetCompression/Pass2/Mesh<offset>.json.
public static class DesktopCompressionMeshPass2
{
    private const string OUTPUT = "output/DesktopAssetCompression";
    private const double MAX_RELATIVE_POSITION_ERROR = 0.0005;
    private const double MAX_UV_ERROR = 1.0 / 4096;
    private const double MAX_NORMAL_ANGLE = 2;

    public static string Main(int offset, int count)
    {
        JArray candidates = Candidates();
        string resultPath = OUTPUT + "/Pass2/Mesh" + offset + ".json";
        JArray results = new JArray();
        foreach (JToken record in candidates.Skip(offset).Take(count))
        {
            string path = (string)record["path"];
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            if (EditorJsonUtility.ToJson(importer) != (string)record["rollback"])
                throw new InvalidOperationException("Importer changed since inventory: " + path);
            List<Geometry> before = Read(path);
            JObject result = new JObject { ["path"] = path, ["guid"] = record["guid"], ["meshes"] = before.Count,
                ["vertices"] = before.Sum(g => g.Positions.Length), ["retained"] = false };
            try
            {
                foreach (ModelImporterMeshCompression compression in new[] { ModelImporterMeshCompression.Medium, ModelImporterMeshCompression.Low })
                {
                    importer.meshCompression = compression;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    JObject attempt = Compare(before, Read(path));
                    result[compression.ToString()] = attempt;
                    if (!(bool)attempt["pass"]) continue;
                    result["retained"] = true;
                    result["compression"] = compression.ToString();
                    break;
                }
                if (!(bool)result["retained"]) Restore(importer);
            }
            catch
            {
                Restore(importer);
                AssetDatabase.SaveAssets();
                throw;
            }
            results.Add(result);
            AssetDatabase.SaveAssets();
            File.WriteAllText(resultPath, results.ToString(Formatting.Indented));
        }
        return JsonConvert.SerializeObject(new { candidates = candidates.Count, offset, processed = results.Count,
            retained = results.Count(r => (bool)r["retained"]), result = resultPath });
    }

    public static string Count() => JsonConvert.SerializeObject(new { candidates = Candidates().Count });

    private static JArray Candidates()
    {
        JObject inventory = JObject.Parse(File.ReadAllText(OUTPUT + "/Inventory.json"));
        HashSet<string> colliders = new HashSet<string>(JArray.Parse(File.ReadAllText(OUTPUT + "/Pass2/collider-source-guids.json")).Select(g => (string)g));
        return new JArray(inventory["assets"].Where(r => (bool)r["dependency"] && (string)r["importer"] == "ModelImporter"
            && (string)r["settings"]["compression"] == "Off" && !colliders.Contains((string)r["guid"])
            && r["meshes"].Any() && r["meshes"].All(m => (int)m["blendShapeCount"] == 0))
            .OrderBy(r => (string)r["path"]));
    }

    private static void Restore(ModelImporter importer)
    {
        importer.meshCompression = ModelImporterMeshCompression.Off;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private sealed class Geometry
    {
        public string Name;
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector2[] Uvs;
    }

    private static List<Geometry> Read(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>()
        .OrderBy(m => m.name).Select(m => new Geometry { Name = m.name, Positions = m.vertices, Normals = m.normals, Uvs = m.uv }).ToList();

    private static JObject Compare(List<Geometry> before, List<Geometry> after)
    {
        bool sameTopology = before.Count == after.Count && before.Zip(after, (a, b) => a.Name == b.Name
            && a.Positions.Length == b.Positions.Length && a.Normals.Length == b.Normals.Length && a.Uvs.Length == b.Uvs.Length).All(x => x);
        if (!sameTopology) return new JObject { ["pass"] = false, ["reason"] = "Mesh or vertex layout changed." };
        Bounds bounds = new Bounds(before[0].Positions.FirstOrDefault(), Vector3.zero);
        foreach (Geometry g in before) foreach (Vector3 p in g.Positions) bounds.Encapsulate(p);
        double diagonal = Math.Max(bounds.size.magnitude, 1e-6), position = 0, uv = 0, normal = 0;
        for (int m = 0; m < before.Count; m++)
        {
            Geometry a = before[m], b = after[m];
            for (int i = 0; i < a.Positions.Length; i++) position = Math.Max(position, (a.Positions[i] - b.Positions[i]).magnitude);
            for (int i = 0; i < a.Uvs.Length; i++) uv = Math.Max(uv, Math.Max(Math.Abs(a.Uvs[i].x - b.Uvs[i].x), Math.Abs(a.Uvs[i].y - b.Uvs[i].y)));
            for (int i = 0; i < a.Normals.Length; i++) normal = Math.Max(normal, Vector3.Angle(a.Normals[i], b.Normals[i]));
        }
        double relative = position / diagonal;
        return new JObject { ["relativePositionError"] = relative, ["maxUvError"] = uv, ["maxNormalAngleDeg"] = normal,
            ["pass"] = relative <= MAX_RELATIVE_POSITION_ERROR && uv <= MAX_UV_ERROR && normal <= MAX_NORMAL_ANGLE };
    }
}
