using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyHeavyDreadnoughtUnityGeometry
{
    const string TASK = "Temp/HeavyDreadnoughtImport/";
    const string MODEL = "Assets/Art/Models/RepublicShips/HeavyDreadnought/HeavyDreadnought.fbx";
    const string MATERIALS = "Assets/Art/Materials/Models/RepublicShips/HeavyDreadnought/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/Republic/HeavyDreadnought.prefab";
    const float TOLERANCE = .00001f;

    public static string Main()
    {
        var report = JObject.Parse(File.ReadAllText(TASK + "ConversionReport.json"));
        var expected = (JObject)report["before"];
        var sourceGeometry = JObject.Parse(File.ReadAllText(TASK + "SourceGeometry.json"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL);
        var filters = model.GetComponentsInChildren<MeshFilter>(true);
        var transforms = model.GetComponentsInChildren<Transform>(true);
        var sourceMeshes = (JObject)expected["meshes"];
        var sourceBones = (JObject)expected["bones"];
        Check(filters.Length == 63 && sourceMeshes.Count == 63 && sourceGeometry.Count == 63, "Complete transport FBX must contain 63 authored meshes");
        Check(sourceBones.Count == 141, "Source contains 141 uniquely named bones");
        Check(Mathf.Abs(((ModelImporter)AssetImporter.GetAtPath(MODEL)).globalScale - .02f) < TOLERANCE, "Expected source-to-Unity import scale");
        var rows = new JArray();
        foreach (var source in sourceMeshes.Properties())
        {
            var filter = filters.Single(f => f.name == (string)source.Value["exported_name"]);
            var mesh = filter.sharedMesh;
            var expectedMaterials = source.Value["materials"].Values<string>().ToArray();
            var actualMaterials = filter.GetComponents<MeshRenderer>().Single().sharedMaterials;
            Check(mesh.subMeshCount == expectedMaterials.Length && actualMaterials.Select(m => m.name).SequenceEqual(expectedMaterials), "Source material slots: " + source.Name);
            for (int slot = 0; slot < expectedMaterials.Length; slot++)
            {
                Check(AssetDatabase.GetAssetPath(actualMaterials[slot]) == MATERIALS + expectedMaterials[slot] + ".mat", "Explicit material remap: " + source.Name);
                Check(mesh.GetTriangles(slot).Length / 3 == (int)source.Value["slot_triangles"][slot], "Per-slot triangles: " + source.Name + ":" + slot);
            }
            int triangles = mesh.triangles.Length / 3;
            Check(triangles == (int)source.Value["triangles"], "Triangle count: " + source.Name);
            Check(mesh.uv.Length == mesh.vertexCount, "UV channel: " + source.Name);
            var corners = (JArray)sourceGeometry[source.Name];
            Check(corners.Count == (triangles * 3 + 31) / 32, "Every 32nd source corner is represented: " + source.Name);
            var error = GeometryError(filter, corners);
            Check(error.x <= TOLERANCE && error.y <= TOLERANCE, "Source corner/UV displacement: " + source.Name + " " + error);
            rows.Add(new JObject { ["name"] = source.Name, ["triangles"] = triangles, ["vertices"] = mesh.vertexCount, ["sampledCorners"] = corners.Count, ["maximumPositionError"] = error.x, ["maximumUvError"] = error.y, ["materials"] = new JArray(expectedMaterials) });
        }
        int totalTriangles = rows.Sum(row => (int)row["triangles"]);
        Check(totalTriangles == 40221, "Complete transport FBX must contain 40221 triangles");
        float boneError = 0;
        foreach (var source in sourceBones.Properties())
        {
            var bone = transforms.Single(t => t.name == source.Name && t.GetComponents<MeshFilter>().Length == 0);
            boneError = Mathf.Max(boneError, Vector3.Distance(bone.position, Position(source.Value["head"])));
            string parent = (string)source.Value["parent"];
            Check(parent == null || bone.parent.name == parent, "Bone parent: " + source.Name);
        }
        Check(boneError <= TOLERANCE, "Bone positions preserve source coordinates: " + boneError);
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        var visibleNames = sourceMeshes.Properties().Where(m => !(bool)m.Value["hidden"] && (string)m.Value["shader"] == "MeshBumpColorize.fx").Select(m => (string)m.Value["exported_name"]).OrderBy(n => n).ToArray();
        Check(visible.Select(r => r.name).OrderBy(n => n).SequenceEqual(visibleNames) && visible.Length == 25, "Runtime visual retains 25 authored opaque meshes");
        Check(visible.All(r => r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")), "Runtime source helpers/effects are stripped");
        int visibleTriangles = visible.Sum(r => r.GetComponents<MeshFilter>().Single().sharedMesh.triangles.Length / 3);
        Check(visibleTriangles == 8110, "Runtime visible triangle count is 8110");
        Check(visual.transform.localScale == Vector3.one && visual.transform.localPosition == Vector3.zero && visual.transform.localRotation == Quaternion.identity, "Visual root is identity");
        var result = new JObject {
            ["success"] = true, ["meshes"] = rows, ["meshCount"] = filters.Length, ["totalTriangles"] = totalTriangles,
            ["bones"] = sourceBones.Count, ["maximumBoneError"] = boneError, ["sampleStride"] = 32,
            ["sampledCorners"] = rows.Sum(row => (int)row["sampledCorners"]), ["tolerance"] = TOLERANCE,
            ["visibleHullTriangles"] = visibleTriangles, ["visibleMeshes"] = visible.Length, ["rootIdentity"] = true
        };
        File.WriteAllText(TASK + "UnityGeometryReport.json", result.ToString());
        return result.ToString();
    }

    static Vector2 GeometryError(MeshFilter filter, JArray corners)
    {
        var mesh = filter.sharedMesh; var vertices = mesh.vertices; var uv = mesh.uv;
        var byUv = new Dictionary<string, List<int>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            string key = UvKey(uv[i]);
            if (!byUv.TryGetValue(key, out var list)) byUv[key] = list = new List<int>();
            list.Add(i);
        }
        var error = Vector2.zero;
        foreach (var corner in corners)
        {
            var expected = Position(corner); var expectedUv = new Vector2((float)corner[3], (float)corner[4]);
            if (!byUv.TryGetValue(UvKey(expectedUv), out var candidates)) candidates = Enumerable.Range(0, uv.Length).Where(i => Vector2.Distance(uv[i], expectedUv) <= TOLERANCE).ToList();
            var matching = candidates.Where(i => Vector2.Distance(uv[i], expectedUv) <= TOLERANCE).ToArray();
            Check(matching.Length > 0, "UV corner missing: " + filter.name);
            int nearest = matching.OrderBy(i => Vector3.SqrMagnitude(expected - filter.transform.TransformPoint(vertices[i]))).First();
            error.x = Mathf.Max(error.x, Vector3.Distance(expected, filter.transform.TransformPoint(vertices[nearest])));
            error.y = Mathf.Max(error.y, Vector2.Distance(expectedUv, uv[nearest]));
        }
        return error;
    }

    static Vector3 Position(JToken p) => new Vector3(-(float)p[0], (float)p[2], -(float)p[1]) * .02f;
    static string UvKey(Vector2 uv) => Math.Round(uv.x, 5).ToString("F5", CultureInfo.InvariantCulture) + "/" + Math.Round(uv.y, 5).ToString("F5", CultureInfo.InvariantCulture);
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
