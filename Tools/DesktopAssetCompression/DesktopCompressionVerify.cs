using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class DesktopCompressionVerify
{
    public static string Main()
    {
        const string OUTPUT = "output/DesktopAssetCompression";
        var inventory = JObject.Parse(File.ReadAllText(OUTPUT + "/Inventory.json"));
        var baseline = inventory["assets"].ToDictionary(r => (string)r["path"]);
        var batch = new JArray(Directory.GetFiles(OUTPUT, "Batch*.json").SelectMany(p => JArray.Parse(File.ReadAllText(p))));
        var pilot = JArray.Parse(File.ReadAllText(OUTPUT + "/Pilot.json"));
        var paths = batch.Select(r => (string)r["path"]).Concat(pilot.Select(r => (string)r["path"])).Distinct().ToArray();
        var records = new JArray();
        foreach (string path in paths)
        {
            var old = baseline[path];
            if (AssetDatabase.AssetPathToGUID(path) != (string)old["guid"]) throw new InvalidOperationException("GUID changed: " + path);
            var importer = AssetImporter.GetAtPath(path);
            var result = new JObject { ["path"] = path, ["guid"] = old["guid"], ["before"] = old["settings"].DeepClone() };
            if (importer is TextureImporter t)
            {
                bool restored = path.EndsWith("UnitSelectionRing.png") || batch.Any(r => (string)r["path"] == path && !(bool)r["retained"]);
                var expected = restored ? TextureImporterCompression.Uncompressed : path.EndsWith("AcclamatorAssaultIcon.png") ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                if (t.textureCompression != expected || t.crunchedCompression != (bool)old["settings"]["crunchedCompression"] || t.maxTextureSize != (int)old["settings"]["maxTextureSize"] || t.mipmapEnabled != (bool)old["settings"]["mipmapEnabled"] || t.sRGBTexture != (bool)old["settings"]["sRGBTexture"] || t.isReadable != (bool)old["settings"]["isReadable"] || t.textureType.ToString() != (string)old["settings"]["type"] || t.GetPlatformTextureSettings("Standalone").overridden)
                    throw new InvalidOperationException("Unexpected persisted texture settings: " + path);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture.width != (int)old["imported"]["width"] || texture.height != (int)old["imported"]["height"] || texture.mipmapCount != (int)old["imported"]["mipmapCount"])
                    throw new InvalidOperationException("Dimensions/mip count changed: " + path);
                result["after"] = JObject.FromObject(new { compression = t.textureCompression.ToString(), format = texture.format.ToString(), texture.width, texture.height, texture.mipmapCount });
            }
            else if (importer is ModelImporter m)
            {
                if (m.meshCompression.ToString() != (string)old["settings"]["compression"] || m.isReadable != (bool)old["settings"]["isReadable"] || m.animationType.ToString() != (string)old["settings"]["animation"])
                    throw new InvalidOperationException("Mesh exception not restored: " + path);
                result["after"] = old["settings"].DeepClone();
            }
            else if (importer is AudioImporter audio)
            {
                var s = audio.defaultSampleSettings;
                var previous = old["settings"].ToObject<AudioImporterSampleSettings>();
                bool music = path.Contains("/Music/");
                bool weapon = path.Contains("/Weapons/");
                if (s.loadType != (music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad) || s.compressionFormat != (weapon ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis) || s.quality != (music ? 0.7f : previous.quality) || s.preloadAudioData != previous.preloadAudioData || s.sampleRateSetting != previous.sampleRateSetting || s.sampleRateOverride != previous.sampleRateOverride || audio.ContainsSampleSettingsOverride("Standalone") || audio.forceToMono != (bool)old["flags"]["forceToMono"] || audio.loadInBackground != (bool)old["flags"]["loadInBackground"])
                    throw new InvalidOperationException("Unexpected persisted audio settings: " + path);
                result["after"] = JObject.FromObject(s);
                result["standalone"] = JObject.FromObject(audio.GetOverrideSampleSettings("Standalone"));
            }
            records.Add(result);
        }
        foreach (JObject record in pilot)
        {
            record.Remove("rollback");
            string path = (string)record["path"];
            record["finalDisposition"] = path.EndsWith("UnitSelectionRing.png") || path.EndsWith("AcclamatorAssault.fbx") || path.Contains("/Commands/") ? "Restored original settings; exception retained." : "Retained importer change; runtime/Mac acceptance pending.";
        }
        var renders = new JArray();
        foreach (string beforePath in Directory.GetFiles(OUTPUT + "/Before", "*.png"))
        {
            var beforeImage = new Texture2D(2, 2);
            var afterImage = new Texture2D(2, 2);
            beforeImage.LoadImage(File.ReadAllBytes(beforePath));
            afterImage.LoadImage(File.ReadAllBytes(OUTPUT + "/After/" + Path.GetFileName(beforePath)));
            var a = beforeImage.GetPixels32();
            var b = afterImage.GetPixels32();
            double sum = 0;
            for (int i = 0; i < a.Length; i++) sum += Math.Pow(a[i].r - b[i].r, 2) + Math.Pow(a[i].g - b[i].g, 2) + Math.Pow(a[i].b - b[i].b, 2);
            renders.Add(new JObject { ["image"] = Path.GetFileName(beforePath), ["rgbRmse8Bit"] = Math.Sqrt(sum / (a.Length * 3)) });
            UnityEngine.Object.DestroyImmediate(beforeImage);
            UnityEngine.Object.DestroyImmediate(afterImage);
        }
        var manifest = new JObject { ["unity"] = inventory["unity"], ["target"] = inventory["target"], ["graphicsApi"] = inventory["graphicsApi"], ["gpu"] = inventory["gpu"], ["inventoryCount"] = baseline.Count, ["dependencyCount"] = baseline.Values.Count(r => (bool)r["dependency"]), ["atlasPaths"] = inventory["atlasPaths"], ["pilot"] = pilot, ["batches"] = batch, ["persistedReadback"] = records, ["renderComparisons"] = renders };
        Directory.CreateDirectory("Tools/DesktopAssetCompression/Evidence");
        File.WriteAllText("Tools/DesktopAssetCompression/Evidence/Manifest.json", manifest.ToString(Formatting.Indented));
        return JsonConvert.SerializeObject(new { verified = records.Count, batchRetained = batch.Count(r => (bool)r["retained"]), batchRestored = batch.Count(r => !(bool)r["retained"]), manifest = "Tools/DesktopAssetCompression/Evidence/Manifest.json" });
    }
}
