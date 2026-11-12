using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class ImportPlanetTextures
{
    private const string MANIFEST_PATH = "Temp/PlanetImport/manifest.json";
    private const string OUTPUT_PATH = "Temp/PlanetImport/imported-textures.json";
    private const string MODEL_FOLDER = "Assets/Art/Textures/Models/Planets/Original";
    private const string PREVIEW_FOLDER = "Assets/Art/Textures/Ui/PlanetPreviews/Original";
    private const string HIGH_RESOLUTION_FOLDER = "Tools/PlanetTextures/HighResolution";

    public static string Main(bool highResolutionOnly = false)
    {
        if (highResolutionOnly)
        {
            foreach (string source in Directory.GetFiles(HIGH_RESOLUTION_FOLDER, "*.png"))
                ImportTexture(source, MODEL_FOLDER + "/" + Path.GetFileName(source), false, false);
            AssetDatabase.SaveAssets();
            return "Imported and saved high-resolution planet albedos with existing GUIDs.";
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Import planet textures in Edit Mode.");

        JObject manifest = JObject.Parse(File.ReadAllText(MANIFEST_PATH));
        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var previews = new JObject();
        EnsureFolder(MODEL_FOLDER);
        EnsureFolder(PREVIEW_FOLDER);
        foreach (JObject planet in manifest["planets"])
        {
            if ((bool)planet["preserve_existing"]) continue;
            sources.Add((string)planet["surface"]);
            string cloud = (string)planet["cloud"];
            if (cloud.Length > 0) sources.Add(cloud);
            foreach (JObject ring in planet["rings"])
                sources.Add((string)ring["texture"]);

            string previewPath = PREVIEW_FOLDER + "/" + (string)planet["name"] + "_Preview.png";
            // Existing previews are authored widescreen renders; never restore the old source thumbnails.
            if (!File.Exists(previewPath)) ImportTexture((string)planet["preview"], previewPath, true, true);
            previews[(string)planet["name"]] = previewPath;
        }

        var textures = new JObject();
        foreach (string source in sources)
        {
            JToken texture = manifest["textures"][source];
            string role = (string)texture["role"];
            string suffix = role == "surface" ? "_Albedo" : role == "cloud" ? "_Clouds" : "_Rings";
            string path = MODEL_FOLDER + "/" + Path.GetFileNameWithoutExtension(source) + suffix + ".png";
            ImportTexture((string)texture["png"], path, role != "surface", false);
            textures[source] = path;
        }

        AssetDatabase.SaveAssets();
        File.WriteAllText(OUTPUT_PATH, new JObject
        {
            ["textures"] = textures,
            ["previews"] = previews
        }.ToString(Formatting.Indented));
        return $"Imported {textures.Count} shared textures and {previews.Count} planet previews; saved {OUTPUT_PATH}.";
    }

    private static void ImportTexture(string source, string target, bool preserveAlpha, bool preview)
    {
        string replacement = HIGH_RESOLUTION_FOLDER + "/" + Path.GetFileName(target);
        bool highResolution = !preview && File.Exists(replacement);
        if (highResolution) source = replacement;
        File.Copy(source, target, true);
        AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(target);
        var imported = AssetDatabase.LoadAssetAtPath<Texture2D>(target);
        bool lossless = !preview && imported.width <= 256 && imported.height <= 256;
        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = true;
        importer.alphaSource = preserveAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
        importer.alphaIsTransparency = preserveAlpha;
        importer.isReadable = false;
        importer.mipmapEnabled = !preview;
        importer.ignoreMipmapLimit = !preview;
        importer.wrapMode = preview ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.filterMode = preview ? FilterMode.Bilinear : FilterMode.Trilinear;
        importer.anisoLevel = preview ? 1 : 4;
        importer.maxTextureSize = preview ? 512 : highResolution ? 1024 : 2048;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = lossless ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        importer.crunchedCompression = false;
        importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
        {
            name = "Standalone",
            overridden = true,
            maxTextureSize = importer.maxTextureSize,
            format = lossless ? (preserveAlpha ? TextureImporterFormat.RGBA32 : TextureImporterFormat.RGB24) : TextureImporterFormat.BC7,
            textureCompression = importer.textureCompression,
            compressionQuality = 100
        });
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Substring("Assets/".Length).Split('/'))
        {
            string next = current + "/" + part;
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }
}
