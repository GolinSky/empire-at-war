using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// November 23: Resurgent import textures (hull + turrets), same desktop policy and gates as DesktopCompressionSafeTextures.
// Baseline = Uncompressed at the current 2048 import ceiling (4K sources stay capped; resolution never changed).
// Normal maps → DXT5; colour → DXT1 (opaque) / DXT5 (alpha), then BC7; keep the first passing format, else Uncompressed.
// Gates: colour RGB RMSE ≤ 2/255 and max alpha ≤ 8/255; normals mean ≤ 1.5°, p99 ≤ 6°.
// Run: unity command script run --file Tools/DesktopAssetCompression/DesktopCompressionResurgent.cs
//      --entry DesktopCompressionResurgent.Main --args '[0,12]' --timeout 900 --json
public static class DesktopCompressionResurgent
{
    private const string OUTPUT = "output/DesktopAssetCompression/Resurgent20261123";
    private const string ROOT = "Assets/Art/Textures/Models/EmpireShips";

    public static string Main(int offset, int count)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Editor must be idle and outside Play Mode.");
        string[] paths = AssetDatabase.FindAssets("t:Texture2D", new[] { ROOT }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.StartsWith(ROOT + "/Resurgent", StringComparison.Ordinal)).Distinct().OrderBy(p => p).ToArray();
        Directory.CreateDirectory(OUTPUT);
        int processed = 0;
        foreach (string path in paths.Skip(offset).Take(count))
        {
            string resultPath = OUTPUT + "/" + AssetDatabase.AssetPathToGUID(path) + ".json";
            if (File.Exists(resultPath)) continue;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool normal = importer.textureType == TextureImporterType.NormalMap;
            bool readable = importer.isReadable;
            var result = new JObject { ["path"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path), ["normal"] = normal,
                ["beforeDefault"] = JObject.FromObject(importer.GetDefaultPlatformTextureSettings()),
                ["beforeStandalone"] = JObject.FromObject(importer.GetPlatformTextureSettings("Standalone")), ["retained"] = false };

            // Uncompressed reference at the existing import ceiling.
            var baseline = importer.GetDefaultPlatformTextureSettings();
            baseline.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(baseline);
            importer.ClearPlatformTextureSettings("Standalone");
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int width = source.width, height = source.height, mips = source.mipmapCount;
            result["width"] = width; result["height"] = height; result["mips"] = mips; result["baselineFormat"] = source.format.ToString();
            if (width % 4 != 0 || height % 4 != 0 || width < 4 || height < 4)
            {
                result["reason"] = "Block compression requires dimensions divisible by four; left uncompressed.";
                Finish(importer, path, result, resultPath);
                processed++;
                continue;
            }
            var before = Readback(source);
            bool retained = false;
            var attempts = new JArray();
            result["attempts"] = attempts;
            // Normal maps: DXT5 first; BC7 retry (2026-11-12 precedent) when the hull normals miss the p99 gate.
            var formats = normal ? new[] { TextureImporterFormat.DXT5, TextureImporterFormat.BC7 }
                : new[] { source.format == TextureFormat.RGB24 ? TextureImporterFormat.DXT1 : TextureImporterFormat.DXT5, TextureImporterFormat.BC7 };
            foreach (var format in formats)
            {
                var candidate = importer.GetDefaultPlatformTextureSettings();
                candidate.name = "Standalone";
                candidate.overridden = true;
                candidate.format = format;
                candidate.textureCompression = format == TextureImporterFormat.BC7 ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                candidate.compressionQuality = format == TextureImporterFormat.BC7 ? 100 : 50;
                candidate.crunchedCompression = false;
                importer.SetPlatformTextureSettings(candidate);
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture.width != width || texture.height != height || texture.mipmapCount != mips || importer.isReadable != readable)
                    throw new InvalidOperationException("Dimensions/mips/readability changed: " + path);
                var attempt = normal ? NormalError(before, Readback(texture)) : ColorError(before, Readback(texture));
                attempt["format"] = texture.format.ToString();
                bool compressed = texture.format == TextureFormat.DXT1 || texture.format == TextureFormat.DXT5 || texture.format == TextureFormat.BC7;
                retained = compressed && (normal ? (double)attempt["meanAngleDeg"] <= 1.5 && (double)attempt["p99AngleDeg"] <= 6
                    : (double)attempt["rgbRmse8Bit"] <= 2 && (int)attempt["maxAlphaError8Bit"] <= 8);
                attempt["pass"] = retained;
                attempts.Add(attempt);
                if (retained) break;
            }
            if (!retained)
            {
                importer.ClearPlatformTextureSettings("Standalone");
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
            result["retained"] = retained;
            Finish(importer, path, result, resultPath);
            processed++;
        }
        return JsonConvert.SerializeObject(new { total = paths.Length, offset, processed });
    }

    public static string Summary()
    {
        var rows = Directory.GetFiles(OUTPUT, "*.json").Select(f => JObject.Parse(File.ReadAllText(f))).OrderBy(r => (string)r["path"]).ToArray();
        return string.Join("\n", rows.Select(r =>
        {
            var last = r["attempts"] is JArray a && a.Count > 0 ? (JObject)a.Last : null;
            string err = last == null ? "" : (bool)r["normal"] ? $"mean {(double)last["meanAngleDeg"]:0.00}° p99 {(double)last["p99AngleDeg"]:0.0}°"
                : $"rmse {(double)last["rgbRmse8Bit"]:0.00} alpha {(int)last["maxAlphaError8Bit"]}";
            return $"{Path.GetFileName((string)r["path"])} | {r["width"]}x{r["height"]} | {r["finalFormat"]} | retained={r["retained"]} | {err} | {r["reason"]}";
        }));
    }

    private static void Finish(TextureImporter importer, string path, JObject result, string resultPath)
    {
        AssetDatabase.SaveAssets();
        result["finalStandalone"] = JObject.FromObject(importer.GetPlatformTextureSettings("Standalone"));
        result["finalFormat"] = AssetDatabase.LoadAssetAtPath<Texture2D>(path).format.ToString();
        result["memoryBytes"] = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(AssetDatabase.LoadAssetAtPath<Texture2D>(path));
        File.WriteAllText(resultPath, result.ToString());
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
        return new JObject { ["meanAngleDeg"] = sum / a.Length, ["p99AngleDeg"] = Math.Min(180, (bin + 1) / 10.0) };
    }

    private static Vector3 Decode(Color32 c)
    {
        float x = c.r / 255f * (c.a / 255f) * 2f - 1f;
        float y = c.g / 255f * 2f - 1f;
        return new Vector3(x, y, Mathf.Sqrt(Mathf.Max(0f, 1f - x * x - y * y)));
    }

    private static Color32[] Readback(Texture2D source)
    {
        var previous = RenderTexture.active;
        var target = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, true);
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
