using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildPlanetMeshes
{
    private const string OUTPUT = "Assets/Art/Models/Planets/Shared/";

    public static string Main()
    {
        // Preserve the existing sphere's exact UV convention, sampling every second grid point.
        var source = AssetDatabase.LoadAssetAtPath<Mesh>(OUTPUT + "PlanetSphereHighPoly.asset");
        if (source.vertexCount != 193 * 97) throw new InvalidOperationException("Shared sphere topology changed.");
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        var sourceVertices = source.vertices;
        var sourceNormals = source.normals;
        var sourceUv = source.uv;
        for (int y = 0; y <= 96; y += 2)
        for (int x = 0; x <= 192; x += 2)
        {
            int index = y * 193 + x;
            vertices.Add(sourceVertices[index]); normals.Add(sourceNormals[index]); uv.Add(sourceUv[index]);
        }
        for (int y = 0; y < 48; y++)
        for (int x = 0; x < 96; x++)
        {
            int a = y * 97 + x, b = a + 1, c = a + 97, d = c + 1;
            AddTriangle(a, b, c); AddTriangle(b, d, c);
        }
        void AddTriangle(int a, int b, int c)
        {
            Vector3 cross = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            if (cross.sqrMagnitude < 1e-14f) return;
            if (Vector3.Dot(cross, vertices[a] + vertices[b] + vertices[c]) < 0) (b, c) = (c, b);
            triangles.Add(a); triangles.Add(b); triangles.Add(c);
        }
        var sphere = new Mesh { name = "PlanetSphere" };
        sphere.SetVertices(vertices); sphere.SetNormals(normals); sphere.SetUVs(0, uv); sphere.SetTriangles(triangles, 0);
        sphere.RecalculateTangents(); sphere.RecalculateBounds();
        Save(sphere, OUTPUT + "PlanetSphere.asset");
        foreach (string planet in new[] { "Geonosis", "Kuat" })
        foreach (JToken part in JArray.Parse(File.ReadAllText("Temp/PlanetImport/" + planet + "-Rings.json")))
        {
            var mesh = new Mesh { name = planet + "_" + (string)part["name"] };
            mesh.vertices = part["vertices"].Select(v => new Vector3((float)v[0], (float)v[1], (float)v[2])).ToArray();
            mesh.normals = part["normals"].Select(v => new Vector3((float)v[0], (float)v[1], (float)v[2])).ToArray();
            mesh.uv = part["uv"].Select(v => new Vector2((float)v[0], (float)v[1])).ToArray();
            mesh.triangles = part["triangles"].Select(v => (int)v).ToArray();
            mesh.RecalculateTangents(); mesh.RecalculateBounds();
            Save(mesh, OUTPUT + mesh.name + ".asset");
        }
        AssetDatabase.SaveAssets();
        return "Shared sphere: " + vertices.Count + " vertices, " + triangles.Count / 3 + " triangles; original Geonosis/Kuat rings saved.";
    }

    private static void Save(Mesh mesh, string path)
    {
        mesh.UploadMeshData(true);
        var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (existing == null) AssetDatabase.CreateAsset(mesh, path);
        else { EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(mesh); }
    }
}
