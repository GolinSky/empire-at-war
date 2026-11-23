using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

// November 23: review every texture still uncompressed on desktop after the November 16 pass.
// Inventory() is read-only: effective Standalone format, type, size, material consumers and blend, prior outcome.
// Run: unity command script run --file Tools/DesktopAssetCompression/DesktopCompressionUncompressed20261123.cs
//      --entry DesktopCompressionUncompressed20261123.Inventory --timeout 900 --json
public static class DesktopCompressionUncompressed20261123
{
    public const string OUTPUT = "output/DesktopAssetCompression/Uncompressed20261123";
    private const string PRIOR = "output/DesktopAssetCompression/Audit20261116";

    private static readonly HashSet<TextureFormat> COMPRESSED = new HashSet<TextureFormat>
    {
        TextureFormat.DXT1, TextureFormat.DXT5, TextureFormat.BC4, TextureFormat.BC5, TextureFormat.BC6H, TextureFormat.BC7,
        TextureFormat.DXT1Crunched, TextureFormat.DXT5Crunched,
    };

    public static string Inventory()
    {
        Directory.CreateDirectory(OUTPUT);
        var consumers = MaterialConsumers();
        var prior = File.Exists(PRIOR + "/compression-classification.json")
            ? JObject.Parse(File.ReadAllText(PRIOR + "/compression-classification.json"))["textures"].ToDictionary(r => (string)r["guid"])
            : new Dictionary<string, JToken>();
        var rows = new JArray();
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || COMPRESSED.Contains(texture.format)) continue;
            string priorReason = null, priorStatus = null;
            if (prior.TryGetValue(guid, out JToken p)) { priorStatus = (string)p["status"]; priorReason = (string)p["reason"]; }
            string result = PRIOR + "/Textures/" + guid + ".json";
            if (File.Exists(result))
            {
                var r = JObject.Parse(File.ReadAllText(result));
                priorStatus = (bool)r["retained"] ? "retained-20261116" : "rejected-20261116";
                priorReason = (string)r["reason"] ?? string.Join("; ", (r["attempts"] as JArray ?? new JArray()).Select(a => a.ToString(Formatting.None)));
            }
            var mats = consumers.TryGetValue(guid, out var list) ? list : new List<JObject>();
            rows.Add(new JObject
            {
                ["path"] = path, ["guid"] = guid, ["type"] = importer.textureType.ToString(), ["format"] = texture.format.ToString(),
                ["width"] = texture.width, ["height"] = texture.height, ["mips"] = texture.mipmapCount, ["readable"] = importer.isReadable,
                ["sRGB"] = importer.sRGBTexture, ["memoryBytes"] = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture),
                ["standaloneOverride"] = importer.GetPlatformTextureSettings("Standalone").overridden,
                ["defaultCompression"] = importer.textureCompression.ToString(),
                ["blockable"] = texture.width % 4 == 0 && texture.height % 4 == 0,
                ["category"] = Category(path, importer, mats),
                ["materials"] = new JArray(mats), ["priorStatus"] = priorStatus, ["priorReason"] = priorReason,
            });
        }
        File.WriteAllText(OUTPUT + "/inventory.json", rows.ToString());
        var summary = rows.GroupBy(r => (string)r["category"]).OrderBy(g => g.Key)
            .Select(g => $"{g.Key}: {g.Count()} textures, {g.Sum(r => (long)r["memoryBytes"]) / 1048576.0:0.0} MB, blockable {g.Count(r => (bool)r["blockable"])}");
        return string.Join("\n", summary);
    }

    // ---------- Screening ----------
    // Each consumer slot says which channels reach the screen; the texture must pass every requirement it has.
    //   rgb    Ship Lit / Ship Wreck base and emission (alpha unused: output alpha is forced to 1, no clip); additive One/One
    //   r      _TeamMaskMap (red only)
    //   normal _BumpMap
    //   premul additive SrcAlpha/One: visible = rgb × a
    //   rgba   UI, gloss maps (smoothness in alpha) and anything unknown
    // Formats: rgb/r only → DXT1 then BC7; normal → DXT5 then BC7; alpha needed → DXT5 then BC7. Else Uncompressed.
    // Gates: RMSE ≤ 2/255 on used channels; alpha max ≤ 8/255 (rgba); premul p99 ≤ 8/255; normal mean ≤ 1.5°, p99 ≤ 6°.
    private static readonly string[] SCREENED = { "additive", "mixed-additive", "color-or-mask", "normal", "ui" };
    private static readonly string[] DATA_TEXTURES = { "Assets/Art/Textures/Vfx/HazardNoise.png", "Assets/Art/Textures/Vfx/HazardWarp.png" };

    public static string Screen(int offset, int count)
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.isUpdating)
            throw new InvalidOperationException("Editor must be idle and outside Play Mode.");
        var rows = Candidates();
        Directory.CreateDirectory(OUTPUT + "/Results");
        Directory.CreateDirectory(OUTPUT + "/Images");
        var slots = SlotRequirements();
        int processed = 0;
        foreach (JToken row in rows.Skip(offset).Take(count))
        {
            string path = (string)row["path"], guid = (string)row["guid"];
            string resultPath = OUTPUT + "/Results/" + guid + ".json";
            if (File.Exists(resultPath)) continue;
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var original = importer.GetPlatformTextureSettings("Standalone");
            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int width = source.width, height = source.height, mips = source.mipmapCount;
            bool readable = importer.isReadable;
            var needs = new HashSet<string>(importer.textureType == TextureImporterType.NormalMap ? new[] { "normal" }
                : (string)row["category"] == "ui" || !slots.ContainsKey(guid) ? new[] { "rgba" } : slots[guid].ToArray());
            var result = new JObject { ["path"] = path, ["guid"] = guid, ["category"] = row["category"], ["needs"] = new JArray(needs.ToArray()),
                ["width"] = width, ["height"] = height, ["beforeFormat"] = source.format.ToString(),
                ["beforeStandalone"] = JObject.FromObject(original), ["beforeMemory"] = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(source),
                ["retained"] = false };
            if (width % 4 != 0 || height % 4 != 0 || width < 4 || height < 4)
            {
                result["reason"] = "Block compression requires dimensions divisible by four; preserve original resolution.";
                File.WriteAllText(resultPath, result.ToString());
                processed++;
                continue;
            }
            var before = Readback(source);
            bool alphaNeeded = needs.Contains("rgba") || needs.Contains("premul");
            var formats = needs.Contains("normal") || alphaNeeded ? new[] { TextureImporterFormat.DXT5, TextureImporterFormat.BC7 }
                : new[] { TextureImporterFormat.DXT1, TextureImporterFormat.BC7 };
            var attempts = new JArray();
            result["attempts"] = attempts;
            bool retained = false;
            Color32[] kept = null;
            try
            {
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
                    var after = Readback(texture);
                    var attempt = new JObject { ["format"] = texture.format.ToString() };
                    bool pass = texture.format == TextureFormat.DXT1 || texture.format == TextureFormat.DXT5 || texture.format == TextureFormat.BC7;
                    foreach (string need in needs) pass &= Gate(need, before, after, attempt);
                    attempt["pass"] = pass;
                    attempt["memory"] = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
                    attempts.Add(attempt);
                    if (pass) { retained = true; kept = after; break; }
                }
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
                var final = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                result["retained"] = retained;
                result["finalFormat"] = final.format.ToString();
                result["afterMemory"] = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(final);
                result["finalStandalone"] = JObject.FromObject(importer.GetPlatformTextureSettings("Standalone"));
                File.WriteAllText(resultPath, result.ToString());
            }
            if (retained) SaveComparison(guid, path, before, kept, width, height);
            processed++;
        }
        return JsonConvert.SerializeObject(new { total = rows.Count, offset, processed });
    }

    public static string Summary()
    {
        var rows = Directory.GetFiles(OUTPUT + "/Results", "*.json").Select(f => JObject.Parse(File.ReadAllText(f))).OrderBy(r => (string)r["path"]).ToArray();
        long before = rows.Sum(r => (long)r["beforeMemory"]), after = rows.Sum(r => (long)(r["afterMemory"] ?? r["beforeMemory"]));
        var lines = rows.Select(r =>
        {
            var last = r["attempts"] is JArray a && a.Count > 0 ? (JObject)a.Last : null;
            string metrics = last == null ? (string)r["reason"] : string.Join(" ", last.Properties().Where(p => p.Name != "format" && p.Name != "pass" && p.Name != "memory").Select(p => $"{p.Name}={p.Value}"));
            return $"{((string)r["path"]).Replace("Assets/Art/Textures/", "")} | {r["category"]} | {string.Join("+", r["needs"])} | {r["width"]}x{r["height"]} | {r["beforeFormat"]} -> {r["finalFormat"]} | {(bool)r["retained"]} | {(long)r["beforeMemory"] / 1048576.0:0.0} -> {(long)(r["afterMemory"] ?? r["beforeMemory"]) / 1048576.0:0.0} MB | {metrics}";
        });
        return $"screened {rows.Length}, retained {rows.Count(r => (bool)r["retained"])}, Editor memory {before / 1048576.0:0.0} -> {after / 1048576.0:0.0} MB\n" + string.Join("\n", lines);
    }

    private static List<JToken> Candidates() =>
        JArray.Parse(File.ReadAllText(OUTPUT + "/inventory.json"))
            .Where(r => SCREENED.Contains((string)r["category"]) && !DATA_TEXTURES.Contains((string)r["path"]))
            .OrderBy(r => (string)r["path"]).ToList();

    private static Dictionary<string, HashSet<string>> SlotRequirements()
    {
        var map = new Dictionary<string, HashSet<string>>();
        foreach (string matGuid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuid));
            if (material == null || material.shader == null) continue;
            string shader = material.shader.name;
            bool ship = shader == "EmpireAtWar/Ship Lit" || shader == "EmpireAtWar/Ship Wreck";
            float src = material.HasProperty("_SrcBlend") ? material.GetFloat("_SrcBlend") : -1f;
            float dst = material.HasProperty("_DstBlend") ? material.GetFloat("_DstBlend") : -1f;
            bool additive = material.renderQueue >= 2500 && dst == (float)UnityEngine.Rendering.BlendMode.One;
            foreach (string property in material.GetTexturePropertyNames())
            {
                Texture texture = material.GetTexture(property);
                if (texture == null || !AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out string guid, out long _)) continue;
                // URP shaders expose the base texture twice: _BaseMap and the legacy _MainTex alias.
                bool baseMap = property == "_BaseMap" || property == "_MainTex";
                string need = property == "_BumpMap" ? "normal"
                    : additive && baseMap ? (src == (float)UnityEngine.Rendering.BlendMode.SrcAlpha ? "premul" : src == (float)UnityEngine.Rendering.BlendMode.One ? "rgb" : "rgba")
                    : ship && (baseMap || property == "_EmissionMap") ? "rgb"
                    : ship && property == "_TeamMaskMap" ? "r"
                    : "rgba";
                if (!map.TryGetValue(guid, out var set)) map[guid] = set = new HashSet<string>();
                set.Add(need);
            }
        }
        return map;
    }

    private static bool Gate(string need, Color32[] a, Color32[] b, JObject attempt)
    {
        switch (need)
        {
            case "normal":
            {
                var n = NormalError(a, b);
                attempt["meanDeg"] = Math.Round((double)n["meanAngleDeg"], 2); attempt["p99Deg"] = n["p99AngleDeg"];
                return (double)n["meanAngleDeg"] <= 1.5 && (double)n["p99AngleDeg"] <= 6;
            }
            case "r":
            {
                double rmse = Rmse(a, b, c => c.r);
                attempt["rRmse"] = Math.Round(rmse, 2);
                return rmse <= 2;
            }
            case "rgb":
            {
                double rmse = RgbRmse(a, b, c => c);
                attempt["rgbRmse"] = Math.Round(rmse, 2);
                return rmse <= 2;
            }
            case "premul":
            {
                double rmse = RgbRmse(a, b, Premultiply);
                int p99 = P99(a, b);
                attempt["visibleRmse"] = Math.Round(rmse, 2); attempt["visibleP99"] = p99;
                return rmse <= 2 && p99 <= 8;
            }
            default:
            {
                double rmse = RgbRmse(a, b, c => c);
                int maxAlpha = 0;
                for (int i = 0; i < a.Length; i++) maxAlpha = Math.Max(maxAlpha, Math.Abs(a[i].a - b[i].a));
                attempt["rgbRmse"] = Math.Round(rmse, 2); attempt["maxAlpha"] = maxAlpha;
                return rmse <= 2 && maxAlpha <= 8;
            }
        }
    }

    private static Color32 Premultiply(Color32 c) => new Color32((byte)(c.r * c.a / 255), (byte)(c.g * c.a / 255), (byte)(c.b * c.a / 255), 255);

    private static double Rmse(Color32[] a, Color32[] b, Func<Color32, byte> channel)
    {
        double sum = 0;
        for (int i = 0; i < a.Length; i++) sum += Math.Pow(channel(a[i]) - channel(b[i]), 2);
        return Math.Sqrt(sum / a.Length);
    }

    private static double RgbRmse(Color32[] a, Color32[] b, Func<Color32, Color32> map)
    {
        double sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            Color32 x = map(a[i]), y = map(b[i]);
            sum += Math.Pow(x.r - y.r, 2) + Math.Pow(x.g - y.g, 2) + Math.Pow(x.b - y.b, 2);
        }
        return Math.Sqrt(sum / (a.Length * 3.0));
    }

    private static int P99(Color32[] a, Color32[] b)
    {
        var histogram = new int[256];
        for (int i = 0; i < a.Length; i++)
        {
            Color32 x = Premultiply(a[i]), y = Premultiply(b[i]);
            histogram[Math.Max(Math.Abs(x.r - y.r), Math.Max(Math.Abs(x.g - y.g), Math.Abs(x.b - y.b)))]++;
        }
        int target = (int)Math.Ceiling(a.Length * 0.99), seen = 0;
        for (int bin = 0; bin < 256; bin++) { seen += histogram[bin]; if (seen >= target) return bin; }
        return 255;
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
        for (; bin < histogram.Length; bin++) { seen += histogram[bin]; if (seen >= target) break; }
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

    // Before | after | difference × 8, side by side, at most 512 px per panel, for visual review.
    private static void SaveComparison(string guid, string path, Color32[] before, Color32[] after, int width, int height)
    {
        int step = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(width, height) / 512f));
        int w = width / step, h = height / step;
        var image = new Texture2D(w * 3, h, TextureFormat.RGBA32, false);
        try
        {
            var pixels = new Color32[w * 3 * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = y * step * width + x * step;
                    Color32 a = before[i], b = after[i];
                    pixels[y * w * 3 + x] = new Color32(a.r, a.g, a.b, 255);
                    pixels[y * w * 3 + w + x] = new Color32(b.r, b.g, b.b, 255);
                    pixels[y * w * 3 + 2 * w + x] = new Color32(Diff(a.r, b.r), Diff(a.g, b.g), Diff(a.b, b.b), 255);
                }
            image.SetPixels32(pixels);
            image.Apply();
            File.WriteAllBytes($"{OUTPUT}/Images/{Path.GetFileNameWithoutExtension(path)}_{guid.Substring(0, 6)}.png", image.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(image);
        }
    }

    private static byte Diff(byte p, byte q) => (byte)Mathf.Min(255, Mathf.Abs(p - q) * 8);

    private static string Category(string path, TextureImporter importer, List<JObject> mats)
    {
        if (path.Contains("/Editor/") || path.Contains("/Tests/") || path.Contains("/Sandbox/") || path.StartsWith("Assets/ThirdParty") || path.StartsWith("Assets/Plugins"))
            return "excluded-editor-vendor";
        if (importer.isReadable) return "excluded-readable";
        if (path.Contains("/Fonts/") || path.Contains("SDF")) return "excluded-font";
        if (importer.textureType == TextureImporterType.Sprite || path.Contains("/Ui/") || path.Contains("/UI/")) return "ui";
        if (mats.Count == 0) return "no-material";
        if (importer.textureType == TextureImporterType.NormalMap) return "normal";
        if (mats.All(m => (bool)m["additive"])) return "additive";
        if (mats.Any(m => (bool)m["additive"])) return "mixed-additive";
        return "color-or-mask";
    }

    private static Dictionary<string, List<JObject>> MaterialConsumers()
    {
        var map = new Dictionary<string, List<JObject>>();
        var guidPattern = new Regex("guid: ([0-9a-f]{32})");
        foreach (string matGuid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string matPath = AssetDatabase.GUIDToAssetPath(matGuid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null) continue;
            float src = material.HasProperty("_SrcBlend") ? material.GetFloat("_SrcBlend") : -1f;
            float dst = material.HasProperty("_DstBlend") ? material.GetFloat("_DstBlend") : -1f;
            bool transparent = material.renderQueue >= 2500;
            var info = new JObject
            {
                ["material"] = matPath, ["shader"] = material.shader != null ? material.shader.name : "<missing>",
                ["src"] = src, ["dst"] = dst, ["queue"] = material.renderQueue,
                ["additive"] = transparent && dst == (float)UnityEngine.Rendering.BlendMode.One,
            };
            foreach (Match m in guidPattern.Matches(File.ReadAllText(matPath)))
            {
                string g = m.Groups[1].Value;
                if (!map.TryGetValue(g, out var list)) map[g] = list = new List<JObject>();
                if (!list.Any(x => (string)x["material"] == matPath)) list.Add(info);
            }
        }
        return map;
    }
}
