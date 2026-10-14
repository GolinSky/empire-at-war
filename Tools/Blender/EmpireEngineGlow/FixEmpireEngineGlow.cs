using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class FixEmpireEngineGlow
{
    const string PREFABS = "Assets/Prefabs/Models/Ships/Empire/";
    const string MODELS = "Assets/Art/Models/EmpireShips/";
    const string MATERIALS = "Assets/Art/Materials/Models/EmpireShips/";

    public static string Main()
    {
        FixMaterial("Titan/Titan_M020.mat", "Assets/Art/Textures/Models/EmpireShips/Titan/Titan_Engine_Glow_Fancy_White_Additive.png");
        FixMaterial("Allegiance/Allegiance_Allegiance_Engine_Glow_0.mat", null);
        FixMaterial("Allegiance/Allegiance_Allegiance_Engine_Glow_3_0.mat", null);

        FixShip("Titan", new[] {
            new Vector3(-65.2f, -19.86f, -200f), new Vector3(-40.8f, -19.86f, -200f),
            new Vector3(-13.5f, -19.86f, -200f), new Vector3(13.5f, -19.86f, -200f),
            new Vector3(40.8f, -19.86f, -200f), new Vector3(65.2f, -19.86f, -200f)
        }, new[] { 8.5f, 11.5f, 11.5f, 11.5f, 11.5f, 8.5f }, null, null);

        FixShip("Allegiance", new[] {
            new Vector3(-37.3f, -10.74f, -111.5f), new Vector3(0, -10.74f, -111.5f),
            new Vector3(37.3f, -10.74f, -111.5f)
        }, new[] { 8f, 8f, 8f }, new[] {
            new Vector3(-19.8f, -4.1f, -108.8f), new Vector3(19.8f, -4.1f, -108.8f),
            new Vector3(-19.8f, -17.4f, -108.8f), new Vector3(19.8f, -17.4f, -108.8f)
        }, new[] { 3.5f, 3.5f, 3.5f, 3.5f });

        AssetDatabase.SaveAssets();
        return "Saved fitted engine glows for Titan and Allegiance.";
    }

    static void FixMaterial(string relativePath, string texturePath)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + relativePath);
        if (relativePath.StartsWith("Titan/"))
        {
            material.shader = Shader.Find("Universal Render Pipeline/Unlit");
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));
        }
        material.SetFloat("_Surface", 1);
        material.SetFloat("_Blend", 2);
        material.SetFloat("_SrcBlend", 1);
        material.SetFloat("_DstBlend", 1);
        material.SetFloat("_ZWrite", 0);
        material.SetFloat("_Cull", 0);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;
        EditorUtility.SetDirty(material);
    }

    static void FixShip(string ship, Vector3[] mainCenters, float[] mainRadii, Vector3[] secondaryCenters, float[] secondaryRadii)
    {
        string path = PREFABS + ship + ".prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)
                         .Where(renderer => renderer.name.IndexOf("Glow", StringComparison.OrdinalIgnoreCase) >= 0))
                renderer.enabled = false;

            AddGlow(root, "MainEngineGlow", MODELS + ship + "/" + ship + "_MainEngineGlow.asset",
                MATERIALS + (ship == "Titan" ? "Titan/Titan_M020.mat" : "Allegiance/Allegiance_Allegiance_Engine_Glow_0.mat"),
                mainCenters, mainRadii);
            if (secondaryCenters != null)
                AddGlow(root, "SecondaryEngineGlow", MODELS + ship + "/" + ship + "_SecondaryEngineGlow.asset",
                    MATERIALS + "Allegiance/Allegiance_Allegiance_Engine_Glow_3_0.mat", secondaryCenters, secondaryRadii);

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void AddGlow(GameObject root, string name, string meshPath, string materialPath, Vector3[] centers, float[] radii)
    {
        Transform previous;
        while ((previous = root.transform.Find(name)) != null)
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (int disc = 0; disc < centers.Length; disc++)
        {
            int start = vertices.Count;
            vertices.Add(centers[disc]);
            uvs.Add(new Vector2(.5f, 0));
            for (int point = 0; point <= 32; point++)
            {
                float angle = point * Mathf.PI * 2 / 32;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                vertices.Add(centers[disc] + direction * radii[disc] * .75f);
                uvs.Add(new Vector2(.5f, .15f));
                vertices.Add(centers[disc] + direction * radii[disc]);
                uvs.Add(new Vector2(.5f, 1));
                if (point > 0)
                {
                    int inner = start + 1 + point * 2;
                    triangles.Add(start); triangles.Add(inner - 2); triangles.Add(inner);
                    triangles.Add(inner - 2); triangles.Add(inner - 1); triangles.Add(inner + 1);
                    triangles.Add(inner - 2); triangles.Add(inner + 1); triangles.Add(inner);
                }
            }
        }
        var mesh = new Mesh { name = name };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
        else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }

        var glow = new GameObject(name);
        glow.transform.SetParent(root.transform, false);
        glow.AddComponent<MeshFilter>().sharedMesh = mesh;
        glow.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
    }
}
