using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ApplyAcclamatorAssaultLivery
{
    const int RESOLUTION = 2048;
    const string TEXTURE_FOLDER = "Assets/Art/Textures/Models/EmpireShips/AcclamatorAssault/";
    const string MASK = TEXTURE_FOLDER + "AcclamatorAssault_HullStripeMask.png";
    const string HULL_MATERIAL = "AcclamatorAssault_Ev_acclamator_diffuse_Material";

    public static string Main()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Models/Ships/AcclamatorAssault.prefab");
        var pixels = Enumerable.Repeat(new Color32(0, 0, 0, 255), RESOLUTION * RESOLUTION).ToArray();
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled))
        {
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices.Select(v => root.transform.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
            var normalMatrix = renderer.transform.localToWorldMatrix.inverse.transpose;
            var normals = mesh.normals.Select(n => normalMatrix.MultiplyVector(n).normalized).ToArray();
            // The imported hull uses V in [-1, 0]; the albedo sampler repeats that tile.
            var uv = mesh.uv.Select(v => new Vector2(v.x, v.y + 1)).ToArray();
            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                if (renderer.sharedMaterials[submesh].name != HULL_MATERIAL) continue;
                var triangles = mesh.GetTriangles(submesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                    if ((normals[a] + normals[b] + normals[c]).normalized.y < .5f) continue;
                    Rasterize(pixels, uv[a], uv[b], uv[c], vertices[a], vertices[b], vertices[c]);
                }
            }
        }
        int paintedPixels = pixels.Count(p => p.r > 0);
        if (paintedPixels == 0) throw new InvalidOperationException("No dorsal hull stripe pixels were generated.");
        var texture = new Texture2D(RESOLUTION, RESOLUTION, TextureFormat.RGBA32, false, true);
        try
        {
            texture.SetPixels32(pixels); texture.Apply();
            File.WriteAllBytes(MASK, texture.EncodeToPNG());
        }
        finally { UnityEngine.Object.DestroyImmediate(texture); }
        AssetDatabase.ImportAsset(MASK, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(MASK);
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = false; importer.alphaIsTransparency = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.SaveAndReimport();
        var stripeMask = AssetDatabase.LoadAssetAtPath<Texture2D>(MASK);
        string[] folders = Enumerable.Range(0, 7).Select(i => "Assets/Art/Materials/Models/EmpireShips/AcclamatorAssault" + (i == 0 ? "" : "Turret" + i.ToString("00")))
            .Concat(new[] {"Assets/Art/Materials/Wrecks/AcclamatorAssault"}).ToArray();
        int changed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", folders))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (material.shader.name != "EmpireAtWar/Ship Lit" && material.shader.name != "EmpireAtWar/Ship Wreck") continue;
            if (material.GetTexture("_BaseMap") == null) throw new InvalidOperationException("Missing Acclamator albedo: " + material.name);
            material.SetColor("_BaseColor", new Color(.5f, .5f, .5f, 1));
            material.SetFloat("_TeamRimStrength", 0);
            if (material.name == HULL_MATERIAL || material.name == HULL_MATERIAL + "_Wreck")
            {
                material.SetTexture("_TeamMaskMap", stripeMask);
                material.SetFloat("_TeamMaskStrength", 1);
            }
            EditorUtility.SetDirty(material); changed++;
        }
        AssetDatabase.SaveAssets();
        return "Saved two dorsal team-color stripes; " + paintedPixels + " mask pixels; tuned " + changed + " living/wreck materials; source albedo preserved, rim glow disabled.";
    }

    static void Rasterize(Color32[] pixels, Vector2 a, Vector2 b, Vector2 c, Vector3 pa, Vector3 pb, Vector3 pc)
    {
        float determinant = (b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y);
        if (Mathf.Abs(determinant) < 0.0000001f) return;
        int minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.x, Mathf.Min(b.x, c.x)) * RESOLUTION), 0, RESOLUTION - 1);
        int maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.x, Mathf.Max(b.x, c.x)) * RESOLUTION), 0, RESOLUTION - 1);
        int minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(a.y, Mathf.Min(b.y, c.y)) * RESOLUTION), 0, RESOLUTION - 1);
        int maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(a.y, Mathf.Max(b.y, c.y)) * RESOLUTION), 0, RESOLUTION - 1);
        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            float u = (x + .5f) / RESOLUTION, v = (y + .5f) / RESOLUTION;
            float wa = ((b.y - c.y) * (u - c.x) + (c.x - b.x) * (v - c.y)) / determinant;
            float wb = ((c.y - a.y) * (u - c.x) + (a.x - c.x) * (v - c.y)) / determinant;
            float wc = 1 - wa - wb;
            if (wa < 0 || wb < 0 || wc < 0) continue;
            Vector3 p = pa * wa + pb * wb + pc * wc;
            // Symmetric bands follow the wedge, clear of the bridge and engine block.
            float distance = Mathf.Abs(Mathf.Abs(p.x) - .22f * (35 - p.z));
            float edge = Mathf.Clamp01((.85f - distance) / .13f);
            float end = Mathf.Clamp01((p.z + 16) / .3f) * Mathf.Clamp01((28 - p.z) / .3f);
            byte value = (byte)Mathf.RoundToInt(edge * end * 255);
            int index = y * RESOLUTION + x;
            if (value > pixels[index].r) pixels[index] = new Color32(value, value, value, 255);
        }
    }
}
