using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class VerifyCentaxUnityGeometry
{
    const string TASK = "Temp/CentaxImport/";
    const float TOLERANCE = .00001f;

    public static string Main()
    {
        return VerifyModel("Centax") + "\n" + VerifyModel("CentaxWreck");
    }

    static string VerifyModel(string variant)
    {
        var report = JObject.Parse(File.ReadAllText(TASK + (variant == "Centax" ? "ConversionReport.json" : "WreckConversionReport.json")));
        var expected = (JObject)report["models"][variant]["before"];
        var sourceGeometry = JObject.Parse(File.ReadAllText(TASK + variant + "SourceGeometry.json"));
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/RepublicShips/Centax/" + variant + ".fbx");
        var filters = model.GetComponentsInChildren<MeshFilter>(true);
        var transforms = model.GetComponentsInChildren<Transform>(true);
        var rows = new JArray();
        foreach (var source in ((JObject)expected["meshes"]).Properties())
        {
            var filter = filters.Single(f => f.name == (string)source.Value["exported_name"]);
            var mesh = filter.sharedMesh;
            var expectedMaterials = source.Value["materials"].Values<string>().ToArray();
            var actualMaterials = filter.GetComponent<MeshRenderer>().sharedMaterials;
            if (mesh.subMeshCount != expectedMaterials.Length || !actualMaterials.Select(material => material.name).SequenceEqual(expectedMaterials)) throw new InvalidOperationException("Unity source material slots differ: " + source.Name);
            for (int slot = 0; slot < expectedMaterials.Length; slot++)
            {
                string expectedPath = "Assets/Art/Materials/Models/RepublicShips/Centax/" + expectedMaterials[slot] + ".mat";
                if (AssetDatabase.GetAssetPath(actualMaterials[slot]) != expectedPath) throw new InvalidOperationException("Unity material remap differs: " + source.Name);
                if (mesh.GetTriangles(slot).Length / 3 != (int)source.Value["slot_triangles"][slot]) throw new InvalidOperationException("Unity per-slot triangle count differs: " + source.Name + " slot " + slot);
            }
            int triangles = mesh.triangles.Length / 3;
            if (triangles != (int)source.Value["triangles"]) throw new InvalidOperationException("Unity triangle count differs: " + source.Name);
            if (mesh.uv.Length != mesh.vertexCount) throw new InvalidOperationException("Unity UVs missing: " + source.Name);
            float maximumError = GeometryError(filter, (JArray)sourceGeometry[source.Name]);
            if (maximumError > TOLERANCE) throw new InvalidOperationException("Unity corner displacement: " + source.Name + " " + maximumError);
            rows.Add(new JObject { ["name"] = source.Name, ["triangles"] = triangles, ["vertices"] = mesh.vertexCount, ["maximumPositionError"] = maximumError, ["materials"] = new JArray(expectedMaterials) });
        }
        if (filters.Length != ((JObject)expected["meshes"]).Count) throw new InvalidOperationException("Unity FBX mesh count differs.");
        float boneError = 0;
        foreach (var source in ((JObject)expected["bones"]).Properties())
        {
            var bone = transforms.Single(t => t.name == source.Name && t.GetComponents<MeshFilter>().Length == 0);
            var expectedPosition = Position(source.Value["head"]);
            float error = Vector3.Distance(bone.position, expectedPosition);
            boneError = Mathf.Max(boneError, error);
            string parent = (string)source.Value["parent"];
            if (parent != null && bone.parent.name != parent) throw new InvalidOperationException("Unity bone parent differs: " + source.Name);
        }
        if (boneError > TOLERANCE) throw new InvalidOperationException("Unity bone displacement: " + boneError);
        var visual = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/Republic/" + (variant == "Centax" ? "Centax" : "CentaxWreckVisual") + ".prefab");
        var visible = visual.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).ToArray();
        var visibleNames = ((JObject)expected["meshes"]).Properties().Where(mesh => !(bool)mesh.Value["hidden"] && (variant == "Centax" ? mesh.Name == "centax" || mesh.Name == "thrusters" : true)).Select(mesh => mesh.Name).OrderBy(name => name).ToArray();
        if (!visible.Select(r => r.name).OrderBy(name => name).SequenceEqual(visibleNames)) throw new InvalidOperationException("Centax visual differs from authored visible source meshes.");
        if (visual.transform.localScale != Vector3.one || visual.transform.localPosition != Vector3.zero || visual.transform.localRotation != Quaternion.identity) throw new InvalidOperationException("Centax visual root must be identity.");
        var result = new JObject {
            ["meshes"] = rows, ["totalTriangles"] = rows.Sum(row => (int)row["triangles"]),
            ["bones"] = ((JObject)expected["bones"]).Count, ["maximumBoneError"] = boneError,
            ["visibleHullTriangles"] = visible.Where(renderer => renderer.sharedMaterials.All(material => material.shader.name == "EmpireAtWar/Ship Lit")).Sum(renderer => renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3),
            ["visibleEffectTriangles"] = visible.Where(renderer => renderer.sharedMaterials.Any(material => material.shader.name != "EmpireAtWar/Ship Lit")).Sum(renderer => renderer.GetComponent<MeshFilter>().sharedMesh.triangles.Length / 3),
            ["visibleMeshes"] = visible.Length,
            ["rootIdentity"] = true
        };
        File.WriteAllText(TASK + variant + "UnityGeometryReport.json", result.ToString());
        return result.ToString();
    }

    static float GeometryError(MeshFilter filter, JArray corners)
    {
        var mesh = filter.sharedMesh; var vertices = mesh.vertices; var uv = mesh.uv;
        var byUv = new Dictionary<string, List<int>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            string key = UvKey(uv[i]);
            if (!byUv.TryGetValue(key, out var list)) byUv[key] = list = new List<int>();
            list.Add(i);
        }
        float error = 0;
        foreach (var corner in corners)
        {
            var expected = Position(corner); var expectedUv = new Vector2((float)corner[3], (float)corner[4]);
            if (!byUv.TryGetValue(UvKey(expectedUv), out var candidates)) candidates = Enumerable.Range(0, uv.Length).Where(i => Vector2.Distance(uv[i], expectedUv) < TOLERANCE).ToList();
            if (candidates.Count == 0) throw new InvalidOperationException("Unity UV corner missing: " + filter.name);
            error = Mathf.Max(error, candidates.Min(i => Vector3.Distance(expected, filter.transform.TransformPoint(vertices[i]))));
        }
        return error;
    }

    static Vector3 Position(JToken p) => new Vector3(-(float)p[0], (float)p[2], -(float)p[1]) * .02f;
    static string UvKey(Vector2 uv) => Math.Round(uv.x, 5).ToString("F5", CultureInfo.InvariantCulture) + "/" + Math.Round(uv.y, 5).ToString("F5", CultureInfo.InvariantCulture);
}
