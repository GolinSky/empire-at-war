using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

// Second texture pass (2026-10-24): every shipped, uncompressed, non-readable model texture with a material use and no
// Standalone override. Tries Normal compression, then High Quality, and restores the original when neither passes.
// Color/mask gate: RGB RMSE <= 2/255 and max alpha error <= 8/255. Normal-map gate: decoded mean angle <= 1.5 deg,
// p99 <= 6 deg. Results append to output/DesktopAssetCompression/Pass2/Batch<offset>.json.
public static class DesktopCompressionTexturePass2
{
    private const string OUTPUT = "output/DesktopAssetCompression";
    private const double MAX_RGB_RMSE = 2;
    private const int MAX_ALPHA_ERROR = 8;
    private const double MAX_MEAN_ANGLE = 1.5;
    private const double MAX_P99_ANGLE = 6;

    public static string Main(int offset, int count)
    {
        JArray candidates = Candidates();
        Directory.CreateDirectory(OUTPUT + "/Pass2");
        string resultPath = OUTPUT + "/Pass2/Batch" + offset + ".json";
        JArray results = new JArray();
        foreach (JToken record in candidates.Skip(offset).Take(count))
        {
            string path = (string)record["path"];
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (EditorJsonUtility.ToJson(importer) != (string)record["rollback"])
                throw new InvalidOperationException("Importer changed since inventory: " + path);
            bool normalMap = importer.textureType == TextureImporterType.NormalMap;
            Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            long beforeMemory = Profiler.GetRuntimeMemorySizeLong(source);
            Color32[] before = Readback(source);
            JObject result = new JObject { ["path"] = path, ["guid"] = record["guid"], ["kind"] = normalMap ? "normal" : "color-mask",
                ["uses"] = record["uses"].DeepClone(), ["before"] = record["settings"].DeepClone(), ["beforeEditorMemoryBytes"] = beforeMemory };
            try
            {
                result["retained"] = false;
                foreach (TextureImporterCompression compression in new[] { TextureImporterCompression.Compressed, TextureImporterCompression.CompressedHQ })
                {
                    importer.textureCompression = compression;
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Color32[] after = Readback(texture);
                    if (before.Length != after.Length) throw new InvalidOperationException("Dimensions changed: " + path);
                    long afterMemory = Profiler.GetRuntimeMemorySizeLong(texture);
                    JObject attempt = normalMap ? NormalError(before, after) : ColorError(before, after);
                    attempt["format"] = texture.format.ToString();
                    attempt["editorMemoryBytes"] = afterMemory;
                    bool pass = afterMemory < beforeMemory && (normalMap
                        ? (double)attempt["meanAngleDeg"] <= MAX_MEAN_ANGLE && (double)attempt["p99AngleDeg"] <= MAX_P99_ANGLE
                        : (double)attempt["rgbRmse8Bit"] <= MAX_RGB_RMSE && (int)attempt["maxAlphaError8Bit"] <= MAX_ALPHA_ERROR);
                    attempt["pass"] = pass;
                    result[compression.ToString()] = attempt;
                    if (!pass) continue;
                    result["retained"] = true;
                    result["compression"] = compression.ToString();
                    break;
                }
                if (!(bool)result["retained"]) Restore(importer);
            }
            catch
            {
                Restore(importer);
                AssetDatabase.SaveAssets();
                throw;
            }
            results.Add(result);
            AssetDatabase.SaveAssets();
            File.WriteAllText(resultPath, results.ToString(Formatting.Indented));
        }
        return JsonConvert.SerializeObject(new { candidates = candidates.Count, offset, processed = results.Count,
            retained = results.Count(r => (bool)r["retained"]), result = resultPath });
    }

    public static string Count() => JsonConvert.SerializeObject(new { candidates = Candidates().Count });

    private static JArray Candidates()
    {
        JObject inventory = JObject.Parse(File.ReadAllText(OUTPUT + "/Inventory.json"));
        return new JArray(inventory["assets"].Where(r => (bool)r["dependency"] && (string)r["importer"] == "TextureImporter"
            && ((string)r["path"]).StartsWith("Assets/Art/Textures/Models/") && r["uses"].Any()
            && (string)r["settings"]["compression"] == "Uncompressed" && !(bool)r["settings"]["isReadable"]
            && !(bool)r["settings"]["overridden"] && !(bool)r["settings"]["crunchedCompression"]
            && ((string)r["settings"]["type"] == "Default" || (string)r["settings"]["type"] == "NormalMap"))
            .OrderBy(r => (string)r["path"]));
    }

    private static void Restore(TextureImporter importer)
    {
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    private static JObject ColorError(Color32[] a, Color32[] b)
    {
        double sum = 0;
        int maxAlpha = 0;
        for (int i = 0; i < a.Length; i++)
        {
            sum += Math.Pow(a[i].r - b[i].r, 2) + Math.Pow(a[i].g - b[i].g, 2) + Math.Pow(a[i].b - b[i].b, 2);
            maxAlpha = Math.Max(maxAlpha, Math.Abs(a[i].a - b[i].a));
        }
        return new JObject { ["rgbRmse8Bit"] = Math.Sqrt(sum / (a.Length * 3.0)), ["maxAlphaError8Bit"] = maxAlpha };
    }

    // Decodes like UnpackNormalmapRGorAG (x = r * a, y = g) so DXTnm and RGB layouts compare on the same normal.
    private static JObject NormalError(Color32[] a, Color32[] b)
    {
        int[] histogram = new int[1801];
        double sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double angle = Vector3.Angle(Decode(a[i]), Decode(b[i]));
            sum += angle;
            histogram[Math.Min(1800, (int)(angle * 10))]++;
        }
        int target = (int)Math.Ceiling(a.Length * 0.99), seen = 0, bin = 0;
        for (; bin < histogram.Length; bin++)
        {
            seen += histogram[bin];
            if (seen >= target) break;
        }
        return new JObject { ["meanAngleDeg"] = sum / a.Length, ["p99AngleDeg"] = bin / 10.0 };
    }

    private static Vector3 Decode(Color32 c)
    {
        float x = c.r / 255f * (c.a / 255f) * 2f - 1f;
        float y = c.g / 255f * 2f - 1f;
        return new Vector3(x, y, Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y)));
    }

    private static Color32[] Readback(Texture2D source)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            Color32[] pixels = copy.GetPixels32();
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
