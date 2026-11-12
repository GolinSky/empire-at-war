using System;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class AuditPlanetCompression
{
    public static string Main()
    {
        var rows = new JArray();
        foreach (string name in new[] { "w_planet_gas_Albedo", "w_planet_alderaan_Albedo", "w_planet_forest_Albedo", "w_planetclouds_thickist_Clouds" })
        {
            string path = "Assets/Art/Textures/Models/Planets/Original/" + name + ".png";
            var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            ImageConversion.LoadImage(source, File.ReadAllBytes(path));
            var original = Read(source);
            var compressed = Read(imported);
            double sum = 0; int maximum = 0;
            for (int i = 0; i < original.Length; i++)
            {
                int r = original[i].r - compressed[i].r, g = original[i].g - compressed[i].g, b = original[i].b - compressed[i].b;
                sum += r * r + g * g + b * b;
                maximum = Math.Max(maximum, Math.Max(Math.Abs(r), Math.Max(Math.Abs(g), Math.Abs(b))));
            }
            rows.Add(new JObject { ["texture"] = name, ["width"] = source.width, ["height"] = source.height,
                ["format"] = imported.format.ToString(), ["rgbRmse8Bit"] = Math.Sqrt(sum / (original.Length * 3)), ["maxRgbError8Bit"] = maximum });
            Object.DestroyImmediate(source);
        }
        Directory.CreateDirectory("Temp/PlanetQuality");
        File.WriteAllText("Temp/PlanetQuality/compression.json", rows.ToString());
        return rows.ToString();
    }

    private static Color32[] Read(Texture source)
    {
        var previous = RenderTexture.active;
        var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var image = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            image.Apply();
            return image.GetPixels32();
        }
        finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(image); }
    }
}
