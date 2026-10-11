using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public static class DesktopCompressionBatchTextures
{
    public static string Main(int offset, int count)
    {
        const string OUTPUT = "output/DesktopAssetCompression";
        var inventory = JObject.Parse(File.ReadAllText(OUTPUT + "/Inventory.json"));
        var pilotPaths = JArray.Parse(File.ReadAllText("Tools/DesktopAssetCompression/PilotSelection.json")).Select(r => (string)r["path"]).ToArray();
        var candidates = inventory["assets"].Where(r => (bool)r["dependency"] && (string)r["importer"] == "TextureImporter" && ((string)r["path"]).StartsWith("Assets/Art/Textures/Models/") && !pilotPaths.Contains((string)r["path"]) && (string)r["settings"]["compression"] == "Uncompressed" && (string)r["settings"]["type"] == "Default" && (bool)r["settings"]["sRGBTexture"] && (bool)r["settings"]["mipmapEnabled"] && !(bool)r["settings"]["isReadable"] && !(bool)r["settings"]["overridden"] && r["uses"].Any() && r["uses"].All(u => ((string)u).Contains(":EmpireAtWar/Ship Lit:_BaseMap") || ((string)u).Contains(":EmpireAtWar/Ship Wreck:_BaseMap") || ((string)u).Contains(":EmpireAtWar/Ship Lit:_EmissionMap") || ((string)u).Contains(":EmpireAtWar/Ship Wreck:_EmissionMap"))).OrderBy(r => (string)r["path"]).ToArray();
        var results = new JArray();
        foreach (var record in candidates.Skip(offset).Take(count))
        {
            string path = (string)record["path"];
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (EditorJsonUtility.ToJson(importer) != (string)record["rollback"]) throw new InvalidOperationException("Importer changed since inventory: " + path);
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            long beforeMemory = Profiler.GetRuntimeMemorySizeLong(source);
            var before = Readback(source);
            var result = new JObject { ["path"] = path, ["guid"] = record["guid"], ["uses"] = record["uses"].DeepClone(), ["before"] = record["settings"].DeepClone(), ["beforeEditorMemoryBytes"] = beforeMemory };
            try
            {
                importer.textureCompression = TextureImporterCompression.Compressed;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var after = Readback(texture);
                double sum = 0;
                int maxAlpha = 0;
                if (before.Length != after.Length) throw new InvalidOperationException("Dimensions changed: " + path);
                for (int i = 0; i < before.Length; i++)
                {
                    sum += Math.Pow(before[i].r - after[i].r, 2) + Math.Pow(before[i].g - after[i].g, 2) + Math.Pow(before[i].b - after[i].b, 2);
                    maxAlpha = Math.Max(maxAlpha, Math.Abs(before[i].a - after[i].a));
                }
                double rmse = Math.Sqrt(sum / (before.Length * 3));
                long afterMemory = Profiler.GetRuntimeMemorySizeLong(texture);
                bool keep = rmse <= 2 && maxAlpha <= 8 && afterMemory < beforeMemory;
                result["rgbRmse8Bit"] = rmse;
                result["maxAlphaError8Bit"] = maxAlpha;
                result["candidateFormat"] = texture.format.ToString();
                result["candidateEditorMemoryBytes"] = afterMemory;
                result["retained"] = keep;
                result["reason"] = keep ? "Normal compression; conservative pixel screening passed. Runtime/Mac acceptance pending." : "Restored uncompressed: pixel screening or memory reduction gate failed.";
                if (!keep)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }
            catch
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                AssetDatabase.SaveAssets();
                throw;
            }
            results.Add(result);
            AssetDatabase.SaveAssets();
            File.WriteAllText(OUTPUT + "/Batch" + offset + ".json", results.ToString(Formatting.Indented));
        }
        return JsonConvert.SerializeObject(new { candidates = candidates.Length, processed = results.Count, retained = results.Count(r => (bool)r["retained"]), offset });
    }

    private static Color32[] Readback(Texture2D source)
    {
        var previous = RenderTexture.active;
        var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            var pixels = copy.GetPixels32();
            UnityEngine.Object.DestroyImmediate(copy);
            return pixels;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
