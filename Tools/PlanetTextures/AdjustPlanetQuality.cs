using System.IO;
using UnityEditor;
using UnityEngine;

public static class AdjustPlanetQuality
{
    public static string Main()
    {
        const string MATERIALS = "Assets/Art/Materials/Models/Planets/";
        foreach (string path in Directory.GetFiles(MATERIALS + "Original", "*.mat"))
        {
            bool surface = path.EndsWith("_Surface.mat"), clouds = path.EndsWith("_Clouds.mat");
            if (!surface && !clouds) continue;
            float brightness = clouds ? 0.35f : path.EndsWith("w_planet_gas_Surface.mat") ? 0.4f : 0.55f;
            Tint(path, brightness);
        }
        Tint(MATERIALS + "Coruscant/Coruscant_Surface.mat", 0.65f);
        Tint(MATERIALS + "Kamino/Kamino_Surface.mat", 0.45f);
        var atmosphere = AssetDatabase.LoadAssetAtPath<Material>(MATERIALS + "PlanetAtmosphere.mat");
        atmosphere.SetFloat("_Brightness", 0.7f);
        EditorUtility.SetDirty(atmosphere);
        foreach (string path in Directory.GetFiles("Assets/Art/Textures/Models/Planets/Original", "*.png"))
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool lossless = texture.width <= 256 && texture.height <= 256;
            bool alpha = importer.alphaSource != TextureImporterAlphaSource.None;
            importer.ignoreMipmapLimit = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = lossless ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
            var platform = importer.GetPlatformTextureSettings("Standalone");
            platform.maxTextureSize = 2048;
            platform.format = lossless ? (alpha ? TextureImporterFormat.RGBA32 : TextureImporterFormat.RGB24) : TextureImporterFormat.BC7;
            platform.textureCompression = importer.textureCompression;
            platform.compressionQuality = 100;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return "Darkened all planet surfaces/clouds and atmosphere; Bespin 0.4 tint; source-resolution textures with lossless small maps.";
    }

    private static void Tint(string path, float brightness)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        material.SetColor("_BaseColor", new Color(brightness, brightness, brightness, 1));
        EditorUtility.SetDirty(material);
    }
}
