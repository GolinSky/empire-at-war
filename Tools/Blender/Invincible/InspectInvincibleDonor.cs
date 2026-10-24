using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public static class InspectInvincibleDonor
{
    const string DONOR = "Assets/Prefabs/Models/Ships/Separatist/ProvidenceShipView.prefab";
    public static string Main()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(DONOR);
        var nodes = Transforms(root.transform).ToArray();
        var components = nodes.SelectMany(t => Components(t.gameObject)).ToArray();
        var meshes = new List<object>();
        var vertices = new List<Vector3>();
        foreach (var filter in components.OfType<MeshFilter>())
        {
            var renderer = Components(filter.gameObject).OfType<MeshRenderer>().Single();
            var mesh = filter.sharedMesh;
            var points = mesh.vertices.Select(v => root.transform.InverseTransformPoint(filter.transform.TransformPoint(v))).ToArray();
            if (renderer.enabled && renderer.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")) vertices.AddRange(points);
            meshes.Add(new { path = PathOf(filter.transform), mesh = mesh.name, meshPath = AssetDatabase.GetAssetPath(mesh), enabled = renderer.enabled, vertices = mesh.vertexCount, triangles = mesh.triangles.Length / 3, materials = renderer.sharedMaterials.Select(m => AssetDatabase.GetAssetPath(m)).ToArray() });
        }
        var bounds = new Bounds(vertices[0], Vector3.zero);
        foreach (var point in vertices) bounds.Encapsulate(point);
        var data = new {
            visibleSize = Values(bounds.size),
            center = Values(bounds.center),
            components = components.Where(c => c is MonoBehaviour || c is Collider).Select(c => new { id = c.GetInstanceID(), type = c.GetType().FullName, path = PathOf(c.transform), position = Values(root.transform.InverseTransformPoint(c.transform.position)), json = EditorJsonUtility.ToJson(c) }).ToArray(),
            meshes,
            transforms = nodes.Select(t => new { id = t.GetInstanceID(), path = PathOf(t), position = Values(root.transform.InverseTransformPoint(t.position)), scale = Values(t.localScale) }).ToArray()
        };
        Directory.CreateDirectory("Temp/InvincibleImport");
        File.WriteAllText("Temp/InvincibleImport/ProvidenceSnapshot.json", JsonConvert.SerializeObject(data, Formatting.Indented, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        return JsonConvert.SerializeObject(new { data.visibleSize, data.center, targetLength = bounds.size.z * 39 / 14, components = data.components.Select(c => new { c.type, c.path, c.id }).ToArray() });
    }
    static float[] Values(Vector3 value) => new[] { value.x, value.y, value.z };
    static string PathOf(Transform node) => node.parent == null ? node.name : PathOf(node.parent) + "/" + node.name;
    static IEnumerable<Transform> Transforms(Transform node)
    {
        yield return node;
        foreach (Transform child in node)
            foreach (var descendant in Transforms(child)) yield return descendant;
    }
    static IEnumerable<Component> Components(GameObject obj)
    {
        var entries = new SerializedObject(obj).FindProperty("m_Component");
        for (int i = 0; i < entries.arraySize; i++) yield return (Component)entries.GetArrayElementAtIndex(i).FindPropertyRelative("component").objectReferenceValue;
    }
}
