using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectImperialVenatorEngines
{
    public static string Main()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialVenator.prefab");
        var rows = root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.Contains("Engine")).Select(r =>
        {
            var mesh = r.GetComponents<MeshFilter>().Single().sharedMesh;
            return new {
                r.name, r.enabled, active = r.gameObject.activeSelf, bounds = r.bounds.ToString(),
                vertices = mesh.vertexCount, triangles = mesh.triangles.Length / 3,
                colorCount = mesh.colors.Length,
                alpha = mesh.colors.Select(c => c.a).Distinct().ToArray(),
                rgb = mesh.colors.Take(4).Select(c => c.ToString()).ToArray(),
                uvMin = new [] { mesh.uv.Min(u => u.x), mesh.uv.Min(u => u.y) },
                uvMax = new [] { mesh.uv.Max(u => u.x), mesh.uv.Max(u => u.y) },
                materials = r.sharedMaterials.Select(m => new {m.name, shader = m.shader.name, texture = AssetDatabase.GetAssetPath(m.mainTexture), keywords = m.shaderKeywords,
                    cull = m.HasProperty("_Cull") ? m.GetFloat("_Cull") : -1,
                    blend = m.HasProperty("_SrcBlend") ? m.GetFloat("_SrcBlend") : -1,
                    color = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "none"}).ToArray()
            };
        }).ToArray();
        var output = Newtonsoft.Json.JsonConvert.SerializeObject(rows, Newtonsoft.Json.Formatting.Indented);
        File.WriteAllText("Temp/ImperialVenatorImport/EngineInspection.json", output);
        return output;
    }
}
