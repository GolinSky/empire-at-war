using UnityEditor;
using UnityEngine;

public static class PreserveExistingPlanetDetail
{
    public static string Main()
    {
        foreach (string name in new[] { "Coruscant", "Kamino" })
        {
            string path = "Assets/Art/Textures/Models/Planets/" + name + "/" + name + "_Surface_Albedo.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.ignoreMipmapLimit = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            var platform = importer.GetPlatformTextureSettings("Standalone");
            platform.overridden = true;
            platform.format = TextureImporterFormat.BC7;
            platform.textureCompression = TextureImporterCompression.CompressedHQ;
            platform.compressionQuality = 100;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
        return "Preserved Coruscant/Kamino native detail across quality presets; high-quality BC7 and trilinear filtering.";
    }
}
