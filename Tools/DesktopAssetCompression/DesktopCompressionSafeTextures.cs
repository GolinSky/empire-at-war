using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// November 16: desktop-only, quality-screened compression. Never changes Read/Write or resolution.
public static class DesktopCompressionSafeTextures
{
    private const string OUTPUT = "output/DesktopAssetCompression/Audit20261116";

    public static string Main(int offset, int count)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Editor must be idle and outside Play Mode.");
        var candidates = JObject.Parse(File.ReadAllText(OUTPUT + "/compression-classification.json"))["textures"]
            .Where(r => (string)r["status"] == "candidate-not-yet-screened")
            .OrderBy(r => ((string)r["path"]).Contains("skyboxv1_stars_only") || ((string)r["path"]).Contains("Providence_Hull_Specular") ? 0 : 1)
            .ThenBy(r => (string)r["path"]).ToArray();
        Directory.CreateDirectory(OUTPUT + "/Textures");
        int processed = 0;
        foreach (var record in candidates.Skip(offset).Take(count))
        {
            string path = (string)record["path"];
            string resultPath = OUTPUT + "/Textures/" + AssetDatabase.AssetPathToGUID(path) + ".json";
            if (File.Exists(resultPath)) continue;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var original = importer.GetPlatformTextureSettings("Standalone");
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int width = source.width, height = source.height, mips = source.mipmapCount;
            bool readable = importer.isReadable;
            bool normal = importer.textureType == TextureImporterType.NormalMap;
            var result = new JObject { ["path"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["width"] = width, ["height"] = height, ["mips"] = mips,
                ["beforeFormat"] = source.format.ToString(), ["readable"] = readable,
                ["beforeStandalone"] = JObject.FromObject(original), ["retained"] = false };
            if (width % 4 != 0 || height % 4 != 0 || width < 4 || height < 4)
            {
                result["reason"] = "Block compression requires dimensions divisible by four; preserve original resolution.";
                File.WriteAllText(resultPath, result.ToString());
                continue;
            }
            if (source.format != TextureFormat.RGBA32 && source.format != TextureFormat.RGB24)
                throw new InvalidOperationException("Baseline is no longer uncompressed: " + path);
            var before = Readback(source);
            bool retained = false;
            try
            {
                var attempts = new JArray();
                result["attempts"] = attempts;
                bool artwork = path.Contains("/Ui/") || path.Contains("/UI/") || importer.textureType == TextureImporterType.Sprite;
                var formats = normal ? new[] { TextureImporterFormat.DXT5 }
                    : artwork ? new[] { TextureImporterFormat.BC7 }
                    : new[] { source.format == TextureFormat.RGB24 ? TextureImporterFormat.DXT1 : TextureImporterFormat.DXT5, TextureImporterFormat.BC7 };
                foreach (var format in formats)
                {
                    var candidate = original.overridden ? importer.GetPlatformTextureSettings("Standalone") : importer.GetDefaultPlatformTextureSettings();
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
                File.WriteAllText(resultPath, result.ToString());
            }
            processed++;
        }
        return JsonConvert.SerializeObject(new { total = candidates.Length, offset, processed });
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
