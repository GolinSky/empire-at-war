using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildGladiatorLivery
{
    private const string MASK_PATH = "Assets/Art/Textures/Models/EmpireShips/ISDI/ISDI_TeamStripes_TeamMask.png";
    private const float WIDTH_FRACTION = .005f;
    private const float RING_VISIBLE_FRACTION = 1038f / 1254f;
    private struct Vertex { public Vector3 Position; public Vector3 Normal; public Vector2 Uv; }

    public static string Main()
    {
        return Build("Gladiator");
    }

    private static string Build(string ship)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build in Edit Mode.");
        var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(MASK_PATH);
        string modelPath = "Assets/Prefabs/Models/Ships/Empire/Models/" + ship + ".prefab";
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        var stripes = new List<(Mesh Mesh, Material Material)>();
        var renderers = source.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.name == "Hull").ToArray();
        foreach (var material in renderers.SelectMany(r => r.sharedMaterials).Distinct())
        {
            material.SetFloat("_TeamMaskStrength", 0);
            material.SetFloat("_TeamLiveryStrength", 0);
            material.SetFloat("_TeamRimStrength", 0);
            EditorUtility.SetDirty(material);
        }
        var filters = source.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.transform);
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        float length = bounds.size.z;
        float stripeWidth = length * WIDTH_FRACTION;
        foreach (var material in renderers.SelectMany(r => r.sharedMaterials).Distinct())
        {
            var positions = new List<Vector3>(); var normals = new List<Vector3>(); var uvs = new List<Vector2>(); var triangles = new List<int>();
            foreach (var renderer in renderers.Where(r => r.sharedMaterials.Contains(material)))
            {
                var mesh = filters[renderer.transform].sharedMesh;
                var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                var normalMatrix = matrix.inverse.transpose;
                var vertices = mesh.vertices; var sourceNormals = mesh.normals; var sourceUvs = mesh.uv;
                for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                {
                    if (renderer.sharedMaterials[slot] != material) continue;
                    var indices = mesh.GetTriangles(slot);
                    for (int index = 0; index < indices.Length; index += 3)
                    {
                        var face = Enumerable.Range(0, 3).Select(c => indices[index + c]).Select(i => new Vertex { Position = matrix.MultiplyPoint3x4(vertices[i]), Normal = normalMatrix.MultiplyVector(sourceNormals[i]).normalized, Uv = sourceUvs[i] }).ToList();
                        if (face.Average(v => v.Normal.y) < .4f) continue;
                        foreach (int side in new[] { -1, 1 }) foreach (float start in new[] { .08f, .15f, .22f })
                        {
                            var polygon = Clip(face, v => side * v.Position.x - length * .06f);
                            polygon = Clip(polygon, v => length * .14f - side * v.Position.x);
                            polygon = Clip(polygon, v => v.Position.z - length * start - .32f * side * v.Position.x);
                            polygon = Clip(polygon, v => length * start + stripeWidth + .32f * side * v.Position.x - v.Position.z);
                            if (polygon.Count < 3) continue;
                            int offset = positions.Count;
                            foreach (var vertex in polygon) { positions.Add(vertex.Position + vertex.Normal * length * .00005f); normals.Add(vertex.Normal); uvs.Add(vertex.Uv); }
                            for (int corner = 1; corner < polygon.Count - 1; corner++) triangles.AddRange(matrix.determinant < 0 ? new[] { offset, offset + corner + 1, offset + corner } : new[] { offset, offset + corner, offset + corner + 1 });
                        }
                    }
                }
            }
            if (triangles.Count == 0) continue;
            string name = ship + "_TeamStripes_" + material.name;
            string meshPath = "Assets/Art/Models/EmpireShips/" + ship + "/" + name + ".asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (saved == null) { saved = new Mesh { name = name }; AssetDatabase.CreateAsset(saved, meshPath); }
            saved.Clear(); saved.SetVertices(positions); saved.SetNormals(normals); saved.SetUVs(0, uvs); saved.SetTriangles(triangles, 0); saved.RecalculateTangents(); saved.RecalculateBounds(); EditorUtility.SetDirty(saved);
            string materialPath = "Assets/Art/Materials/Models/EmpireShips/" + ship + "/" + name + ".mat";
            var paint = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (paint == null) { paint = new Material(material) { name = name }; AssetDatabase.CreateAsset(paint, materialPath); }
            ApplyPaint(paint, material, mask);
            stripes.Add((saved, paint));
        }
        if (stripes.Count == 0) throw new InvalidOperationException("No hull surfaces intersect the small stripes.");
        var model = PrefabUtility.LoadPrefabContents(modelPath);
        try { ReplaceStripes(model.transform, stripes); PrefabUtility.SaveAsPrefabAsset(model, modelPath); }
        finally { PrefabUtility.UnloadPrefabContents(model); }
        string viewPath = "Assets/Prefabs/Models/Ships/Empire/View/" + ship + "ShipView.prefab";
        var view = PrefabUtility.LoadPrefabContents(viewPath);
        try
        {
            var move = Component(view, "ShipMoveComponent");
            var body = (Transform)new SerializedObject(move).FindProperty("bodyTransform").objectReferenceValue;
            ReplaceStripes(body, stripes);
            var hull = view.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.All(m => m.shader.name == "EmpireAtWar/Ship Lit")).ToArray();
            Assign(Component(view, "Ship"), "explosionHullRenderers", hull);
            var visibleBounds = view.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.GetComponents<EmpireAtWar.ViewComponents.Health.Shield>().Length == 0).Select(r => r.bounds).ToArray();
            var selectionBounds = visibleBounds[0];
            foreach (var item in visibleBounds.Skip(1)) selectionBounds.Encapsulate(item);
            var selection = new SerializedObject(Component(view, "SelectionComponent"));
            selection.FindProperty("ringSize").vector2Value = Vector2.one * Mathf.Max(selectionBounds.size.x, selectionBounds.size.z) * 1.15f / RING_VISIBLE_FRACTION;
            var marker = (SpriteRenderer)selection.FindProperty("selectedRenderer").objectReferenceValue;
            marker.transform.position = new Vector3(selectionBounds.center.x, marker.transform.position.y, selectionBounds.center.z);
            selection.ApplyModifiedPropertiesWithoutUndo();
            AppendRenderers(Component(view, "UnitRenderStateComponent"), "meshRenderers", view.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("TeamStripes")).ToArray());
            AppendRenderers(Component(view, "FogVisibilityComponent"), "renderers", view.GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("TeamStripes")).ToArray());
            PrefabUtility.SaveAsPrefabAsset(view, viewPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(view); }
        AssetDatabase.SaveAssets();
        UpdateWreck(ship, viewPath);
        UpdatePreview(ship, stripes);
        AssetDatabase.SaveAssets();
        string result = ship + ": " + stripes.Count + " stripe surfaces, " + stripes.Sum(s => s.Mesh.triangles.Length / 3) + " triangles; width=" + stripeWidth + ".";
        System.IO.File.WriteAllText("Temp/GladiatorImport/LiveryReport.txt", result);
        return result;
    }

    private static void ApplyPaint(Material paint, Material source, Texture2D mask)
    {
        paint.SetTexture("_BaseMap", source.GetTexture("_BaseMap"));
        paint.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
        paint.SetColor("_BaseColor", source.GetColor("_BaseColor"));
        paint.SetTexture("_TeamMaskMap", mask); paint.SetFloat("_TeamMaskStrength", 1);
        paint.SetFloat("_TeamLiveryStrength", 0); paint.SetFloat("_TeamRimStrength", 0); paint.SetFloat("_TeamEmissionTint", 0);
        EditorUtility.SetDirty(paint);
    }

    private static void UpdateWreck(string ship, string viewPath)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(viewPath);
        var filters = source.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.transform);
        string path = "Assets/Prefabs/Models/Wrecks/" + ship + "WreckView.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var material in root.GetComponentsInChildren<MeshRenderer>(true).SelectMany(r => r.sharedMaterials).Distinct())
            {
                material.SetFloat("_TeamMaskStrength", 0);
                material.SetFloat("_TeamLiveryStrength", 0);
                material.SetFloat("_TeamRimStrength", 0);
                EditorUtility.SetDirty(material);
            }
            foreach (var old in root.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("TeamStripes")).ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
            foreach (var renderer in source.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("TeamStripes")))
            {
                var go = new GameObject(renderer.name); go.transform.SetParent(root.transform, false);
                var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                go.transform.localPosition = matrix.GetColumn(3); go.transform.localRotation = matrix.rotation; go.transform.localScale = matrix.lossyScale;
                go.AddComponent<MeshFilter>().sharedMesh = filters[renderer.transform].sharedMesh;
                string materialPath = "Assets/Art/Materials/Wrecks/Empire/" + ship + "/" + renderer.sharedMaterial.name + "_Wreck.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    material = new Material(renderer.sharedMaterial) { name = renderer.sharedMaterial.name + "_Wreck", shader = Shader.Find("EmpireAtWar/Ship Wreck") };
                    AssetDatabase.CreateAsset(material, materialPath);
                }
                ApplyPaint(material, renderer.sharedMaterial, AssetDatabase.LoadAssetAtPath<Texture2D>(MASK_PATH));
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
            var view = Component(root, "UnitWreckView");
            Assign(view, "meshRenderers", root.GetComponentsInChildren<MeshRenderer>(true));
            Assign(view, "meshFilters", root.GetComponentsInChildren<MeshFilter>(true));
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void UpdatePreview(string ship, List<(Mesh Mesh, Material Material)> stripes)
    {
        string path = "Assets/Prefabs/Ui/Reinforcement/" + ship + "ReinforcementView.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var hologram = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Vfx/Hologram.mat");
            var previewStripes = stripes.Select(s => (s.Mesh, hologram)).ToList();
            ReplaceStripes(root.transform, previewStripes);
            Assign(Component(root, "UnitSpawnView"), "meshRenderers", root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.Contains(hologram)).ToArray());
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void ReplaceStripes(Transform parent, List<(Mesh Mesh, Material Material)> stripes)
    {
        foreach (var old in parent.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("TeamStripes")).ToArray()) UnityEngine.Object.DestroyImmediate(old.gameObject);
        foreach (var stripe in stripes)
        {
            var go = new GameObject("TeamStripes_" + stripe.Material.name); go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = stripe.Mesh;
            var renderer = go.AddComponent<MeshRenderer>(); renderer.sharedMaterial = stripe.Material; renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private static MonoBehaviour Component(GameObject root, string name) => root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c.GetType().Name == name);
    private static void AppendRenderers(MonoBehaviour component, string field, UnityEngine.Object[] added)
    {
        var config = new SerializedObject(component);
        var array = config.FindProperty(field);
        // Remove only stale entries from replacing our previous stripe objects.
        var retained = Enumerable.Range(0, array.arraySize).Select(i => array.GetArrayElementAtIndex(i).objectReferenceValue).Where(r => r != null);
        Assign(component, field, retained.Concat(added).Distinct().ToArray());
    }

    private static void Assign(MonoBehaviour component, string field, UnityEngine.Object[] values)
    {
        var serialized = new SerializedObject(component); var list = serialized.FindProperty(field); list.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static List<Vertex> Clip(List<Vertex> polygon, Func<Vertex, float> distance)
    {
        var output = new List<Vertex>();
        for (int index = 0; index < polygon.Count; index++)
        {
            var current = polygon[index]; var previous = polygon[(index + polygon.Count - 1) % polygon.Count];
            float currentDistance = distance(current), previousDistance = distance(previous);
            if ((currentDistance >= 0) != (previousDistance >= 0))
            {
                float weight = previousDistance / (previousDistance - currentDistance);
                output.Add(new Vertex { Position = Vector3.Lerp(previous.Position, current.Position, weight), Normal = Vector3.Lerp(previous.Normal, current.Normal, weight).normalized, Uv = Vector2.Lerp(previous.Uv, current.Uv, weight) });
            }
            if (currentDistance >= 0) output.Add(current);
        }
        return output;
    }
}
