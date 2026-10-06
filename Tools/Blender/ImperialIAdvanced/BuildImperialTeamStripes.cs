using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BuildImperialTeamStripes
{
    private const string MESH_PATH = "Assets/Art/Models/EmpireShips/ImperialIAdvanced/ImperialIAdvanced_TeamStripes.asset";
    private const string MATERIAL_PATH = "Assets/Art/Materials/Models/EmpireShips/ImperialIAdvanced/ImperialIAdvanced_TeamStripes.mat";
    private const string WRECK_MATERIAL_PATH = "Assets/Art/Materials/Wrecks/ImperialIAdvanced/ImperialIAdvanced_TeamStripes_Wreck.mat";
    private const string HULL_MATERIAL_PATH = "Assets/Art/Materials/Models/EmpireShips/ImperialIAdvanced/ImperialIAdvanced_Hullplates_ISD_diffuse_MeshBumpColorize.mat";
    private const string HULL_WRECK_PATH = "Assets/Art/Materials/Wrecks/ImperialIAdvanced/ImperialIAdvanced_Hullplates_ISD_diffuse_MeshBumpColorize_Wreck.mat";
    private const float SURFACE_OFFSET = 0.015f;
    private const float STRIPE_WIDTH = 2.6f;
    private const float STRIPE_SLOPE = 0.32f;
    private const string STRIPE_NAME = "TeamStripes";

    private struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 Uv;
    }

    public static string Main()
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/ImperialIAdvancedShipView.prefab");
        var hullMaterial = AssetDatabase.LoadAssetAtPath<Material>(HULL_MATERIAL_PATH);
        var filters = source.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.transform);
        var renderers = source.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled && r.sharedMaterials.Contains(hullMaterial)).ToArray();
        var bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        foreach (var renderer in renderers)
        {
            var mesh = filters[renderer.transform].sharedMesh;
            var matrix = source.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            var normalMatrix = matrix.inverse.transpose;
            var positions = mesh.vertices;
            var sourceNormals = mesh.normals;
            var sourceUvs = mesh.uv;
            var indices = mesh.triangles;
            for (int index = 0; index < indices.Length; index += 3)
            {
                var face = Enumerable.Range(0, 3).Select(corner => indices[index + corner]).Select(i => new Vertex
                {
                    Position = matrix.MultiplyPoint3x4(positions[i]),
                    Normal = normalMatrix.MultiplyVector(sourceNormals[i]).normalized,
                    Uv = sourceUvs[i]
                }).ToList();
                if (face.Average(v => v.Normal.y) < 0.5f) continue;
                foreach (int side in new[] {-1, 1})
                foreach (float start in new[] {-5f, 5f, 15f, 25f})
                {
                    var polygon = Clip(face, v => side * (v.Position.x - bounds.center.x) - 4f);
                    polygon = Clip(polygon, v => 22f - side * (v.Position.x - bounds.center.x));
                    polygon = Clip(polygon, v => v.Position.z - start - STRIPE_SLOPE * side * (v.Position.x - bounds.center.x));
                    polygon = Clip(polygon, v => start + STRIPE_WIDTH + STRIPE_SLOPE * side * (v.Position.x - bounds.center.x) - v.Position.z);
                    if (polygon.Count < 3) continue;
                    int offset = vertices.Count;
                    foreach (var vertex in polygon)
                    {
                        vertices.Add(vertex.Position + vertex.Normal * SURFACE_OFFSET);
                        normals.Add(vertex.Normal);
                        uvs.Add(vertex.Uv);
                    }
                    for (int corner = 1; corner < polygon.Count - 1; corner++) triangles.AddRange(new[] {offset, offset + corner, offset + corner + 1});
                }
            }
        }
        if (triangles.Count == 0) throw new InvalidOperationException("No Imperial foredeck surfaces intersect the team stripes.");
        var stripeMesh = new Mesh {name = "ImperialIAdvanced_TeamStripes"};
        stripeMesh.SetVertices(vertices);
        stripeMesh.SetNormals(normals);
        stripeMesh.SetUVs(0, uvs);
        stripeMesh.SetTriangles(triangles, 0);
        stripeMesh.RecalculateTangents();
        stripeMesh.RecalculateBounds();
        var savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MESH_PATH);
        if (savedMesh == null) AssetDatabase.CreateAsset(stripeMesh, MESH_PATH);
        else
        {
            EditorUtility.CopySerialized(stripeMesh, savedMesh);
            UnityEngine.Object.DestroyImmediate(stripeMesh);
            stripeMesh = savedMesh;
            EditorUtility.SetDirty(stripeMesh);
        }
        var material = CreateMaterial(hullMaterial, MATERIAL_PATH);
        var wreckMaterial = CreateMaterial(AssetDatabase.LoadAssetAtPath<Material>(HULL_WRECK_PATH), WRECK_MATERIAL_PATH);
        var materialFolders = new[] {"Assets/Art/Materials/Models/EmpireShips/ImperialIAdvanced", "Assets/Art/Materials/Models/EmpireShips/ImperialII", "Assets/Art/Materials/Wrecks/ImperialIAdvanced", "Assets/Art/Materials/Wrecks/ImperialII"};
        foreach (string path in AssetDatabase.FindAssets("t:Material", materialFolders).Select(AssetDatabase.GUIDToAssetPath))
        {
            if (path == MATERIAL_PATH || path == WRECK_MATERIAL_PATH || path.Contains("_Helper_")) continue;
            var surface = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!surface.HasProperty("_TeamMaskStrength")) continue;
            surface.SetFloat("_TeamMaskStrength", 0);
            surface.SetFloat("_TeamRimStrength", 0);
            EditorUtility.SetDirty(surface);
        }
        foreach (string name in new[] {"ImperialIAdvanced", "ImperialII"})
        {
            SavePrefab("Assets/Prefabs/Models/Ships/" + name + "ShipView.prefab", stripeMesh, material, false);
            SavePrefab("Assets/Prefabs/Models/Wrecks/" + name + "WreckView.prefab", stripeMesh, wreckMaterial, true);
        }
        AssetDatabase.SaveAssets();
        return "Saved fitted Imperial I/II team stripes: " + vertices.Count + " vertices / " + triangles.Count / 3 + " triangles; ownership, fog, banking, explosion and wreck bindings updated.";
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

    private static Material CreateMaterial(Material source, string path)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source);
            AssetDatabase.CreateAsset(material, path);
        }
        material.name = System.IO.Path.GetFileNameWithoutExtension(path);
        material.SetTexture("_TeamMaskMap", source.GetTexture("_TeamMaskMap"));
        material.SetFloat("_TeamMaskStrength", 1);
        material.SetFloat("_TeamRimStrength", 0);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void SavePrefab(string path, Mesh mesh, Material material, bool wreck)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var parent = wreck ? root.transform : root.GetComponentsInChildren<Transform>(true).Single(t => t.name == "BankingBody");
            var existing = root.GetComponentsInChildren<Transform>(true).SingleOrDefault(t => t.name == STRIPE_NAME);
            var stripe = existing == null ? new GameObject(STRIPE_NAME) : existing.gameObject;
            stripe.transform.SetParent(parent, false);
            var filter = existing == null ? stripe.AddComponent<MeshFilter>() : stripe.GetComponents<MeshFilter>().Single();
            filter.sharedMesh = mesh;
            var renderer = existing == null ? stripe.AddComponent<MeshRenderer>() : stripe.GetComponents<MeshRenderer>().Single();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                var serialized = new SerializedObject(component);
                string type = component.GetType().Name;
                if (type == "TeamColorView" || type == "UnitWreckView") Append(serialized.FindProperty("meshRenderers"), renderer);
                if (type == "FogVisibilityComponent") Append(serialized.FindProperty("renderers"), renderer);
                if (type == "Ship") Append(serialized.FindProperty("explosionHullRenderers"), renderer);
                if (type == "UnitWreckView") Append(serialized.FindProperty("meshFilters"), filter);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }

    private static void Append(SerializedProperty array, UnityEngine.Object value)
    {
        for (int element = 0; element < array.arraySize; element++)
            if (array.GetArrayElementAtIndex(element).objectReferenceValue == value) return;
        int index = array.arraySize;
        array.arraySize++;
        array.GetArrayElementAtIndex(index).objectReferenceValue = value;
    }
}
