using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

// Scoped November 12 retry: explicit desktop BC7, original strict pixel gates.
public static class DesktopCompressionSelectedTextures
{
    private const string OUTPUT = "output/DesktopAssetCompression/Selected20261112";
    private static readonly string[] Paths =
    {
        "RebellionShips/AssaultFrigateMkII/AssaultFrigateMkII_Rebel_Assault_Frigate_MK2_Albedo.png",
        "RepublicShips/Pelta/Pelta_Hull_Albedo.png",
        "EmpireShips/Gladiator/Gladiator_Empire_Gladiator.png",
        "SeparatistShips/Recalcitrant/Recalcitrant_SeV_Commerce_Guild_LC_Front.png",
        "SeparatistShips/Recalcitrant/Recalcitrant_SeV_Commerce_Guild_LC_Rear.png",
        "SeparatistShips/Peleset/Peleset_SeV_Techno_Union_Ord_Cruiser.png",
        "RebellionShips/Liberator/Liberator_Hangar_Imp_Light_Additive.png",
        "RebellionShips/Endurance/Endurance_Rebel_Mon_Cal_Home_One_1_Additive.png",
        "EmpireShips/Gladiator/Gladiator_Empire_Gladiator_TeamMask.png",
        "EmpireShips/Titan/Titan_WornPaint_Titan_M024.png"
    };

    public static string Main(int index)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Editor must be idle and outside Play Mode.");
        string path = "Assets/Art/Textures/Models/" + Paths[index];
        string output = OUTPUT + "/" + index;
        Directory.CreateDirectory(OUTPUT);
        if (File.Exists(output + ".json")) throw new InvalidOperationException("Result already exists: " + output);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        TextureImporterPlatformSettings original = importer.GetPlatformTextureSettings("Standalone");
        TextureImporterPlatformSettings defaults = importer.GetDefaultPlatformTextureSettings();
        if (importer.textureType != TextureImporterType.Default || original.overridden || defaults.textureCompression != TextureImporterCompression.Uncompressed)
            throw new InvalidOperationException("Unexpected importer baseline: " + path);
        File.Copy(path + ".meta", output + ".before.meta", false);
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int width = texture.width, height = texture.height, mips = texture.mipmapCount;
        Color32[] before = Readback(texture);
        long beforeBytes = Profiler.GetRuntimeMemorySizeLong(texture);
        JObject result = new JObject { ["path"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path),
            ["beforeFormat"] = texture.format.ToString(), ["width"] = width, ["height"] = height,
            ["mips"] = mips, ["beforeEditorBytes"] = beforeBytes, ["originalStandalone"] = JObject.FromObject(original) };
        bool retained = false;
        try
        {
            TextureImporterPlatformSettings candidate = importer.GetDefaultPlatformTextureSettings();
            candidate.name = "Standalone";
            candidate.overridden = true;
            candidate.format = TextureImporterFormat.BC7;
            candidate.textureCompression = TextureImporterCompression.CompressedHQ;
            candidate.compressionQuality = 100;
            candidate.crunchedCompression = false;
            importer.SetPlatformTextureSettings(candidate);
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture.width != width || texture.height != height || texture.mipmapCount != mips || texture.format != TextureFormat.BC7)
                throw new InvalidOperationException("Unexpected imported dimensions/mips/format: " + path);
            Color32[] after = Readback(texture);
            double sum = 0;
            int maxAlpha = 0;
            for (int i = 0; i < before.Length; i++)
            {
                sum += Math.Pow(before[i].r - after[i].r, 2) + Math.Pow(before[i].g - after[i].g, 2) + Math.Pow(before[i].b - after[i].b, 2);
                maxAlpha = Math.Max(maxAlpha, Math.Abs(before[i].a - after[i].a));
            }
            double rmse = Math.Sqrt(sum / (before.Length * 3.0));
            long afterBytes = Profiler.GetRuntimeMemorySizeLong(texture);
            retained = rmse <= 2 && maxAlpha <= 8 && afterBytes < beforeBytes;
            result["rgbRmse8Bit"] = rmse;
            result["maxAlphaError8Bit"] = maxAlpha;
            result["candidateEditorBytes"] = afterBytes;
            result["candidateFormat"] = texture.format.ToString();
            result["retained"] = retained;
        }
        finally
        {
            if (!retained)
            {
                importer.SetPlatformTextureSettings(original);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            result["finalStandalone"] = JObject.FromObject(importer.GetPlatformTextureSettings("Standalone"));
            result["finalFormat"] = AssetDatabase.LoadAssetAtPath<Texture2D>(path).format.ToString();
            File.WriteAllText(output + ".json", result.ToString(Formatting.Indented));
        }
        return result.ToString(Formatting.None);
    }

    public static string Verify()
    {
        JArray results = new JArray();
        for (int index = 0; index < Paths.Length; index++)
        {
            JObject result = JObject.Parse(File.ReadAllText(OUTPUT + "/" + index + ".json"));
            string path = (string)result["path"];
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!JToken.DeepEquals(JObject.FromObject(importer.GetPlatformTextureSettings("Standalone")), result["finalStandalone"])
                || texture.format.ToString() != (string)result["finalFormat"]
                || texture.width != (int)result["width"] || texture.height != (int)result["height"]
                || texture.mipmapCount != (int)result["mips"] || AssetDatabase.AssetPathToGUID(path) != (string)result["guid"])
                throw new InvalidOperationException("Persisted readback mismatch: " + path);
            results.Add(new JObject { ["path"] = path, ["retained"] = result["retained"],
                ["format"] = texture.format.ToString(), ["editorBytes"] = Profiler.GetRuntimeMemorySizeLong(texture) });
        }
        return results.ToString(Formatting.None);
    }

    private static Color32[] Readback(Texture2D source)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            return copy.GetPixels32();
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(copy);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
