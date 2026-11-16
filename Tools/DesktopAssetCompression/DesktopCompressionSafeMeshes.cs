using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// November 16: Low only, preserving every Read/Write setting and known exception.
public static class DesktopCompressionSafeMeshes
{
    private const string OUTPUT = "output/DesktopAssetCompression/Audit20261116";
    private const double MAX_POSITION_ERROR = 0.0005;
    private const double MAX_UV_ERROR = 1.0 / 4096;
    private const double MAX_ANGLE = 3;

    public static string Main(int offset, int count)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Editor must be idle and outside Play Mode.");
        JToken[] candidates = JObject.Parse(File.ReadAllText(OUTPUT + "/compression-classification.json"))["models"]
            .Where(r => (string)r["status"] == "candidate-not-yet-screened").OrderBy(r => (string)r["path"]).ToArray();
        Directory.CreateDirectory(OUTPUT + "/Meshes");
        HashSet<string> colliders = ColliderGuids();
        JArray results = new JArray();
        foreach (JToken candidate in candidates.Skip(offset).Take(count))
        {
            string path = (string)candidate["path"], guid = (string)candidate["guid"];
            string resultPath = OUTPUT + "/Meshes/" + guid + ".json";
            if (File.Exists(resultPath)) { results.Add(JObject.Parse(File.ReadAllText(resultPath))); continue; }
            if (AssetDatabase.AssetPathToGUID(path) != guid)
                throw new InvalidOperationException("Candidate GUID changed: " + path);
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(path);
            bool readable = importer.isReadable;
            bool attempted = false, retained = false;
            JObject result = new JObject { ["path"] = path, ["guid"] = guid, ["retained"] = false,
                ["beforeCompression"] = importer.meshCompression.ToString(), ["beforeReadable"] = readable };
            try
            {
                if (importer.meshCompression != ModelImporterMeshCompression.Off)
                    result["skipped"] = "Already compressed; unchanged.";
                else if (colliders.Contains(guid) || importer.addCollider)
                    result["skipped"] = "Current MeshCollider reference; unchanged.";
                else
                {
                    UnityEngine.Object[] objects = AssetDatabase.LoadAllAssetsAtPath(path);
                    GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    Mesh[] meshes = objects.OfType<Mesh>().ToArray();
                    if (objects.OfType<AnimationClip>().Any(c => c.length > 0) ||
                        root.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length > 0 ||
                        meshes.Any(m => m.blendShapeCount > 0 || m.bindposes.Length > 0 || m.GetAllBoneWeights().Length > 0))
                        result["skipped"] = "Animation, skin, blend shape or bone data requires separate review.";
                    else if (meshes.Length == 0 || meshes.All(m => m.vertexCount == 0))
                        result["skipped"] = "No nonempty mesh geometry.";
                    else
                    {
                        List<Geometry> before = Read(path);
                        result["meshCount"] = before.Count;
                        result["vertices"] = before.Sum(m => m.Positions.Length);
                        File.Copy(path + ".meta", OUTPUT + "/Meshes/" + guid + ".before.meta", false);
                        attempted = true;
                        importer.meshCompression = ModelImporterMeshCompression.Low;
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                        JObject comparison = Compare(before, Read(path));
                        result["comparison"] = comparison;
                        if (importer.isReadable != readable)
                            throw new InvalidOperationException("Read/Write changed unexpectedly: " + path);
                        retained = (bool)comparison["pass"];
                        result["retained"] = retained;
                    }
                }
            }
            catch (Exception exception)
            {
                result["error"] = exception.ToString();
                throw;
            }
            finally
            {
                try
                {
                    if (attempted && !retained)
                    {
                        importer.meshCompression = ModelImporterMeshCompression.Off;
                        EditorUtility.SetDirty(importer);
                        importer.SaveAndReimport();
                    }
                    if (attempted) AssetDatabase.SaveAssets();
                    result["finalCompression"] = importer.meshCompression.ToString();
                    result["finalReadable"] = importer.isReadable;
                    result["readableUnchanged"] = importer.isReadable == readable;
                    if (importer.isReadable != readable)
                        throw new InvalidOperationException("Read/Write verification failed: " + path);
                }
                finally { File.WriteAllText(resultPath, result.ToString(Formatting.Indented)); }
            }
            results.Add(result);
        }
        return JsonConvert.SerializeObject(new { candidates = candidates.Length, offset, processed = results.Count,
            retained = results.Count(r => (bool)r["retained"]), output = OUTPUT + "/Meshes" });
    }

    private static HashSet<string> ColliderGuids()
    {
        HashSet<string> guids = new HashSet<string>();
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/") &&
                     (p.EndsWith(".prefab") || p.EndsWith(".unity"))))
        {
            string text = File.ReadAllText(path);
            foreach (Match block in Regex.Matches(text, @"(?ms)^--- !u!64 .*?(?=^--- !u!|\z)"))
            {
                Match mesh = Regex.Match(block.Value, @"m_Mesh: \{[^}]*guid: ([a-f0-9]{32})");
                if (mesh.Success) guids.Add(mesh.Groups[1].Value);
            }
            // An unresolved prefab mesh override may target a collider; preserve it conservatively.
            foreach (Match mesh in Regex.Matches(text, @"propertyPath: m_Mesh\r?\n[^\n]*\r?\n\s*objectReference: \{[^}]*guid: ([a-f0-9]{32})"))
                guids.Add(mesh.Groups[1].Value);
        }
        return guids;
    }

    private sealed class Geometry
    {
        public string Name;
        public long LocalId;
        public Vector3[] Positions;
        public Vector3[] Normals;
        public Vector4[] Tangents;
        public Color[] Colors;
        public Vector4[][] Uvs;
        public int[][] Indices;
        public MeshTopology[] Topologies;
        public Matrix4x4[] Bindposes;
        public BoneWeight[] BoneWeights;
    }

    private static List<Geometry> Read(string path)
    {
        List<Geometry> geometry = new List<Geometry>();
        foreach (Mesh mesh in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>())
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string guid, out long localId);
            Vector4[][] uvs = new Vector4[8][];
            for (int channel = 0; channel < 8; channel++)
            {
                List<Vector4> values = new List<Vector4>();
                mesh.GetUVs(channel, values);
                uvs[channel] = values.ToArray();
            }
            geometry.Add(new Geometry { Name = mesh.name, LocalId = localId, Positions = mesh.vertices,
                Normals = mesh.normals, Tangents = mesh.tangents, Colors = mesh.colors, Uvs = uvs,
                Indices = Enumerable.Range(0, mesh.subMeshCount).Select(i => mesh.GetIndices(i)).ToArray(),
                Topologies = Enumerable.Range(0, mesh.subMeshCount).Select(i => mesh.GetTopology(i)).ToArray(),
                Bindposes = mesh.bindposes, BoneWeights = mesh.boneWeights });
        }
        return geometry.OrderBy(m => m.LocalId).ToList();
    }

    private static JObject Compare(List<Geometry> before, List<Geometry> after)
    {
        bool sameLayout = before.Count == after.Count && before.Zip(after, (a, b) =>
            a.LocalId == b.LocalId && a.Name == b.Name && a.Positions.Length == b.Positions.Length &&
            a.Normals.Length == b.Normals.Length && a.Tangents.Length == b.Tangents.Length &&
            a.Topologies.SequenceEqual(b.Topologies) && a.Indices.Length == b.Indices.Length &&
            a.Indices.Zip(b.Indices, (x, y) => x.SequenceEqual(y)).All(x => x) &&
            a.Colors.SequenceEqual(b.Colors) && a.Bindposes.SequenceEqual(b.Bindposes) &&
            a.BoneWeights.SequenceEqual(b.BoneWeights) &&
            a.Uvs.Zip(b.Uvs, (x, y) => x.Length == y.Length).All(x => x)).All(x => x);
        if (!sameLayout) return new JObject { ["pass"] = false, ["reason"] = "Layout, identity, topology, colors or bones changed." };
        Bounds bounds = new Bounds(before.First(m => m.Positions.Length > 0).Positions[0], Vector3.zero);
        foreach (Geometry mesh in before) foreach (Vector3 point in mesh.Positions) bounds.Encapsulate(point);
        double diagonal = Math.Max(bounds.size.magnitude, 1e-6), position = 0, uv = 0, normal = 0, tangent = 0;
        bool tangentSignsUnchanged = true;
        for (int m = 0; m < before.Count; m++)
        {
            Geometry a = before[m], b = after[m];
            for (int i = 0; i < a.Positions.Length; i++) position = Math.Max(position, (a.Positions[i] - b.Positions[i]).magnitude);
            for (int channel = 0; channel < 8; channel++)
                for (int i = 0; i < a.Uvs[channel].Length; i++)
                    for (int component = 0; component < 4; component++)
                        uv = Math.Max(uv, Math.Abs(a.Uvs[channel][i][component] - b.Uvs[channel][i][component]));
            for (int i = 0; i < a.Normals.Length; i++) normal = Math.Max(normal, Vector3.Angle(a.Normals[i], b.Normals[i]));
            for (int i = 0; i < a.Tangents.Length; i++)
            {
                tangent = Math.Max(tangent, Vector3.Angle((Vector3)a.Tangents[i], (Vector3)b.Tangents[i]));
                tangentSignsUnchanged &= a.Tangents[i].w == b.Tangents[i].w;
            }
        }
        double relative = position / diagonal;
        return new JObject { ["relativePositionError"] = relative, ["maxUvErrorAllChannels"] = uv,
            ["maxNormalAngleDeg"] = normal, ["maxTangentAngleDeg"] = tangent, ["tangentSignsUnchanged"] = tangentSignsUnchanged,
            ["pass"] = relative <= MAX_POSITION_ERROR && uv <= MAX_UV_ERROR && normal <= MAX_ANGLE && tangent <= MAX_ANGLE && tangentSignsUnchanged };
    }
}
