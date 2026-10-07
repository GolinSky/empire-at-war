using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class PaintMC80TeamStripes
{
    const int SIZE = 2048;
    const string TEXTURES = "Assets/Art/Textures/Models/RebellionShips/MC80Independence/";
    const string VISUAL = "Assets/Prefabs/Models/Ships/MC80Independence.prefab";

    public static string Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stripe authoring requires Edit Mode.");
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(VISUAL);
        var masks = new Dictionary<string, Color32[]>();
        var materials = new HashSet<Material>();
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.enabled) continue;
            var mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            var vertices = mesh.vertices;
            var uv = mesh.uv;
            for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
            {
                var material = renderer.sharedMaterials[slot];
                if (material.shader.name != "EmpireAtWar/Ship Lit") continue;
                string name = material.GetTexture("_BaseMap").name;
                if (!name.Contains("Rebel_Mon_Cal_Home_One_")) continue;
                materials.Add(material);
                if (!masks.ContainsKey(name)) masks.Add(name, new Color32[SIZE * SIZE]);
                var indices = mesh.GetTriangles(slot);
                var points = vertices.Select(v => root.transform.InverseTransformPoint(renderer.transform.TransformPoint(v))).ToArray();
                for (int i = 0; i < indices.Length; i += 3)
                {
                    int a = indices[i], b = indices[i + 1], c = indices[i + 2];
                    PaintTriangle(masks[name], uv[a], uv[b], uv[c], points[a], points[b], points[c]);
                }
            }
        }
        var records = new JArray();
        foreach (var pair in masks)
        {
            int painted = pair.Value.Count(p => p.r > 0);
            if (painted == 0) throw new InvalidOperationException("Stripe mask is empty: " + pair.Key);
            var texture = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false, true);
            string path = TEXTURES + pair.Key + "_TeamMask.png";
            try
            {
                texture.SetPixels32(pair.Value); texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                File.WriteAllBytes("Temp/MC80IndependenceImport/Textures/" + pair.Key + "_TeamMask.png", texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default; importer.sRGBTexture = false;
            importer.wrapMode = TextureWrapMode.Repeat; importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true; importer.maxTextureSize = SIZE;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            foreach (var material in materials.Where(m => m.GetTexture("_BaseMap").name == pair.Key))
            {
                material.SetTexture("_TeamMaskMap", mask); material.SetFloat("_TeamMaskStrength", 1);
                material.SetFloat("_TeamLiveryStrength", 0); EditorUtility.SetDirty(material);
            }
            records.Add(new JObject { ["mask"] = path, ["paintedPixels"] = painted, ["coverage"] = (float)painted / (SIZE * SIZE) });
        }
        AssetDatabase.SaveAssets();
        File.WriteAllText("Temp/MC80IndependenceImport/TeamStripeReport.json", new JObject {
            ["design"] = "Paired tapered longitudinal stripes, aft transverse band and bow accent; geometry projected into original UVs.",
            ["resolution"] = SIZE, ["materials"] = new JArray(materials.Select(m => AssetDatabase.GetAssetPath(m))), ["masks"] = records
        }.ToString());
        return "Saved four linear team masks on " + materials.Count + " hull materials; original geometry, UVs and albedo unchanged.";
    }

    static float Stripe(Vector3 point)
    {
        float x = Mathf.Abs(point.x), z = point.z;
        float center = 7.5f + (70 - z) * .035f;
        float longitudinal = Mathf.Clamp01((1.8f - Mathf.Abs(x - center)) / .2f)
            * Mathf.Clamp01((z + 34) / 2) * Mathf.Clamp01((80 - z) / 3);
        float aft = Mathf.Clamp01((2.8f - Mathf.Abs(z + 55)) / .2f) * Mathf.Clamp01((24 - x) / 1);
        float bow = Mathf.Clamp01((1.3f - Mathf.Abs(z - 86)) / .2f) * Mathf.Clamp01((9 - x) / 1);
        return Mathf.Max(longitudinal, aft, bow);
    }

    static void PaintTriangle(Color32[] pixels, Vector2 a, Vector2 b, Vector2 c, Vector3 pa, Vector3 pb, Vector3 pc)
    {
        var minimum = Vector2.Min(a, Vector2.Min(b, c));
        var maximum = Vector2.Max(a, Vector2.Max(b, c));
        for (int tx = -Mathf.FloorToInt(maximum.x); tx <= -Mathf.FloorToInt(minimum.x); tx++)
        for (int ty = -Mathf.FloorToInt(maximum.y); ty <= -Mathf.FloorToInt(minimum.y); ty++)
        {
            Vector2 offset = new Vector2(tx, ty);
            Vector2 u = (a + offset) * SIZE, v = (b + offset) * SIZE, w = (c + offset) * SIZE;
            float determinant = (v.y - w.y) * (u.x - w.x) + (w.x - v.x) * (u.y - w.y);
            if (Mathf.Abs(determinant) < .0001f) continue;
            int xmin = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(u.x, v.x, w.x)));
            int xmax = Mathf.Min(SIZE - 1, Mathf.CeilToInt(Mathf.Max(u.x, v.x, w.x)));
            int ymin = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(u.y, v.y, w.y)));
            int ymax = Mathf.Min(SIZE - 1, Mathf.CeilToInt(Mathf.Max(u.y, v.y, w.y)));
            for (int y = ymin; y <= ymax; y++)
            for (int x = xmin; x <= xmax; x++)
            {
                float ba = ((v.y - w.y) * (x + .5f - w.x) + (w.x - v.x) * (y + .5f - w.y)) / determinant;
                float bb = ((w.y - u.y) * (x + .5f - w.x) + (u.x - w.x) * (y + .5f - w.y)) / determinant;
                float bc = 1 - ba - bb;
                if (ba < 0 || bb < 0 || bc < 0) continue;
                byte strength = (byte)Mathf.RoundToInt(255 * Stripe(pa * ba + pb * bb + pc * bc));
                int index = y * SIZE + x;
                if (strength > pixels[index].r) pixels[index] = new Color32(strength, strength, strength, 255);
            }
        }
    }
}
