using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEditor;
using Unity.Collections;

public static class ShipScaleAudit
{
    const string OUTPUT = "Logs/ShipScale";
    public static string Main() { return Capture("before"); }
    public static string Capture(string phase)
    {
        Directory.CreateDirectory(OUTPUT);
        var targets = JArray.Parse(File.ReadAllText("Tools/ShipScale/targets.json"));
        var mapping = File.ReadAllText("Assets/Settings/AssetMappingData.asset");
        var rows = new JArray();
        foreach (JObject target in targets)
        {
            string id = (string)target["id"];
            var match = Regex.Match(mapping, @"- key: " + id + @"ShipView\s+value:\s+m_AssetGUID: (\w+)");
            Require(match.Success, "Missing mapping: " + id);
            string path = AssetDatabase.GUIDToAssetPath(match.Groups[1].Value);
            string dataPath = Directory.GetFiles("Assets/Settings/Data/Ship", id + "ShipData.asset", SearchOption.AllDirectories).Single().Replace('\\', '/');
            var data = new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(dataPath));
            var wreckData = new SerializedObject(data.FindProperty("<Wreck>k__BackingField").objectReferenceValue);
            string wreckPath = AssetDatabase.GUIDToAssetPath(wreckData.FindProperty("prefab.m_AssetGUID").stringValue);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var bounds = HullBounds(root);
            float length = id == "Lucrehulk" ? Mathf.Max(bounds.size.x, bounds.size.z) : bounds.size.z;
            var row = new JObject { ["id"] = id, ["path"] = path, ["guid"] = match.Groups[1].Value,
                ["dataPath"] = dataPath, ["wreckPath"] = wreckPath, ["length"] = length,
                ["hullMin"] = Vector(bounds.min), ["hullMax"] = Vector(bounds.max),
                ["rootScale"] = Vector(root.transform.localScale),
                ["ship"] = Hierarchy(root), ["wreck"] = Hierarchy(AssetDatabase.LoadAssetAtPath<GameObject>(wreckPath)) };
            foreach (string field in new[] { "HullBottom", "HullTop", "NavigationRadius" })
                row[field] = data.FindProperty("<" + field + ">k__BackingField").floatValue;
            rows.Add(row);
            if (phase == "before")
                foreach (string file in new[] { path, dataPath, wreckPath })
                {
                    string backup = OUTPUT + "/before-files/" + file;
                    Directory.CreateDirectory(Path.GetDirectoryName(backup));
                    if (!File.Exists(backup)) File.Copy(file, backup);
                    if (!File.Exists(backup + ".meta")) File.Copy(file + ".meta", backup + ".meta");
                }
        }
        File.WriteAllText(OUTPUT + "/" + phase + ".json", rows.ToString());
        return "Captured " + rows.Count + " roster entries: " + OUTPUT + "/" + phase + ".json";
    }

    public static Bounds HullBounds(GameObject root)
    {
        bool found = false;
        var bounds = new Bounds();
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var renderer = filter.GetComponent<MeshRenderer>();
            if (renderer == null || !renderer.enabled || filter.sharedMesh == null) continue;
            bool active = true;
            for (var t = filter.transform; t != root.transform; t = t.parent) active &= t.gameObject.activeSelf;
            if (!active) continue;
            string name = filter.name.ToLowerInvariant();
            if (Regex.IsMatch(name, "shield|shadow|lamp|glow|thruster|muzzle|enginevfx")) continue;
            if (!renderer.sharedMaterials.Any(m => m != null && m.shader.name == "EmpireAtWar/Ship Lit")) continue;
            using (var meshData = MeshUtility.AcquireReadOnlyMeshData(filter.sharedMesh))
            using (var vertices = new NativeArray<Vector3>(meshData[0].vertexCount, Allocator.Temp))
            {
                meshData[0].GetVertices(vertices);
                foreach (var vertex in vertices)
                {
                    Vector3 point = filter.transform.TransformPoint(vertex) - root.transform.position;
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
        }
        Require(found, "No hull vertices: " + root.name);
        return bounds;
    }

    public static JArray Hierarchy(GameObject root)
    {
        var result = new JArray();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            var matrix = t.localToWorldMatrix;
            var values = new JArray();
            for (int i = 0; i < 16; i++) values.Add(matrix[i]);
            var row = new JObject { ["path"] = Key(t, root.transform), ["matrix"] = values,
                ["localPosition"] = Vector(t.localPosition), ["localScale"] = Vector(t.localScale),
                ["components"] = new JArray(t.GetComponents<Component>().Select(c => c == null ? "MISSING" : c.GetType().FullName)) };
            var particle = t.GetComponent<ParticleSystem>();
            if (particle != null)
            {
                var main = particle.main;
                row["particleMode"] = main.scalingMode.ToString();
                row["particleEffectiveScale"] = Vector(main.scalingMode == ParticleSystemScalingMode.Local ? t.localScale : t.lossyScale);
            }
            foreach (var component in t.GetComponents<MonoBehaviour>())
            {
                Require(component != null, "Missing script: " + root.name + "/" + t.name);
                var serialized = new SerializedObject(component);
                var scale = serialized.FindProperty("destroyedVfxScale");
                if (scale != null) row["destroyedVfxScale"] = scale.floatValue;
            }
            result.Add(row);
        }
        return result;
    }
    public static string Key(Transform t, Transform root)
    {
        if (t == root) return ".";
        return Key(t.parent, root) + "/" + t.GetSiblingIndex() + ":" + t.name;
    }
    public static JArray Vector(Vector3 v) { return new JArray(v.x, v.y, v.z); }
    public static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
