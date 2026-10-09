using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildISDIRemakeStripes
{
    private const float SURFACE_OFFSET = 0.02f;
    private const float STRIPE_WIDTH = 2.6f;
    private const float STRIPE_SLOPE = 0.32f;
    private struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
    }

    public static string Main()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ISDI.prefab");
        var renderers = source.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && !r.name.StartsWith("TeamStripes") && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
        var materials = renderers.SelectMany(r => r.sharedMaterials).Distinct().ToArray();
        var stripes = new List<(Mesh Mesh, Material Material)>();
        foreach (var material in materials)
        {
            var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            foreach (var renderer in renderers.Where(r => r.sharedMaterials.Contains(material)))
            {
                var mesh = renderer.GetComponents<MeshFilter>().Single().sharedMesh;
                var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                var positions = mesh.vertices; var sourceNormals = mesh.normals; var sourceUvs = mesh.uv;
                for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                {
                    if (renderer.sharedMaterials[slot] != material) continue;
                    var indices = mesh.GetTriangles(slot);
                    for (int index = 0; index < indices.Length; index += 3)
                    {
                        var face = Enumerable.Range(0, 3).Select(c => indices[index + c]).Select(i => new Vertex { Position = matrix.MultiplyPoint3x4(positions[i]), Normal = normalMatrix.MultiplyVector(sourceNormals[i]).normalized, Uv = sourceUvs[i] }).ToList();
                        if (face.Average(v => v.Normal.y) < 0.1f) continue;
                        foreach (int side in new[] { -1, 1 }) foreach (float start in new[] { -5f, 5f, 15f, 25f })
                        {
                            var polygon = Clip(face, v => side * v.Position.x - 4f);
                            polygon = Clip(polygon, v => 22f - side * v.Position.x);
                            polygon = Clip(polygon, v => v.Position.z - start - STRIPE_SLOPE * side * v.Position.x);
                            polygon = Clip(polygon, v => start + STRIPE_WIDTH + STRIPE_SLOPE * side * v.Position.x - v.Position.z);
                            if (polygon.Count < 3) continue;
                            int offset = vertices.Count;
                            foreach (var vertex in polygon) { vertices.Add(vertex.Position + vertex.Normal * SURFACE_OFFSET); normals.Add(vertex.Normal); uvs.Add(vertex.Uv); }
                            for (int corner = 1; corner < polygon.Count - 1; corner++)
                                triangles.AddRange(matrix.determinant < 0 ? new[] { offset, offset + corner + 1, offset + corner } : new[] { offset, offset + corner, offset + corner + 1 });
                        }
                    }
                }
            }
            if (triangles.Count == 0) continue;
            string name = "ISDI_TeamStripes_" + material.name;
            string meshPath = "Assets/Art/Models/EmpireShips/ISDI/" + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) { saved = new Mesh { name = name }; AssetDatabase.CreateAsset(saved, meshPath); }
            saved.Clear(); saved.SetVertices(vertices); saved.SetNormals(normals); saved.SetUVs(0, uvs); saved.SetTriangles(triangles, 0); saved.RecalculateTangents(); saved.RecalculateBounds(); saved.UploadMeshData(false); EditorUtility.SetDirty(saved);
            string materialPath = "Assets/Art/Materials/Models/EmpireShips/ISDI/" + name + ".mat";
            var paint = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (paint == null) { paint = new Material(material); AssetDatabase.CreateAsset(paint, materialPath); }
            paint.CopyPropertiesFromMaterial(material); paint.name = name;
            paint.SetTexture("_TeamMaskMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/Models/EmpireShips/ISDI/ISDI_TeamStripes_TeamMask.png"));
            paint.SetFloat("_TeamMaskStrength", 1); paint.SetFloat("_TeamRimStrength", 0); EditorUtility.SetDirty(paint);
            stripes.Add((saved, paint));
        }
        if (stripes.Count == 0) throw new InvalidOperationException("No source foredeck surfaces intersect team stripes.");
        foreach (string path in new[] { "Assets/Prefabs/Models/Ships/ISDI.prefab", "Assets/Prefabs/Models/Wrecks/Source/ISDIShipView.prefab" })
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("TeamStripes")).ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
                foreach (var stripe in stripes)
                {
                    var obj = new GameObject("TeamStripes_" + stripe.Material.name); obj.transform.SetParent(root.transform, false);
                    obj.AddComponent<MeshFilter>().sharedMesh = stripe.Mesh; var renderer = obj.AddComponent<MeshRenderer>(); renderer.sharedMaterial = stripe.Material; renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return "Saved " + stripes.Count + " source-albedo/UV stripe surfaces, " + stripes.Sum(s => s.Mesh.triangles.Length / 3) + " triangles.";
    }
    private static List<Vertex> Clip(List<Vertex> polygon, Func<Vertex, float> distance)
    {
        var output = new List<Vertex>();
        for (int index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index];
            var previous = polygon[(index + polygon.Count - 1) % polygon.Count];
            float currentDistance = distance(current);
            float previousDistance = distance(previous);
            if ((currentDistance >= 0) != (previousDistance >= 0))
            {
                float weight = previousDistance / (previousDistance - currentDistance);
                output.Add(new Vertex {Position = Vector3.Lerp(previous.Position, current.Position, weight), Normal = Vector3.Lerp(previous.Normal, current.Normal, weight).normalized, Uv = Vector2.Lerp(previous.Uv, current.Uv, weight)});
            }
            if (currentDistance >= 0) output.Add(current);
        }
        return output;
    }

}
