using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public static class DesktopCompressionPilot
{
    public static string Main()
    {
        const string OUTPUT = "output/DesktopAssetCompression";
        var inventory = JObject.Parse(File.ReadAllText(OUTPUT + "/Inventory.json"));
        var selection = JArray.Parse(File.ReadAllText("Tools/DesktopAssetCompression/PilotSelection.json"));
        var results = new JArray();
        foreach (JObject item in selection)
        {
            string path = (string)item["path"];
            var baseline = inventory["assets"].Single(r => (string)r["path"] == path);
            if (!(bool)baseline["dependency"]) throw new InvalidOperationException("Pilot asset is not a shipped dependency: " + path);
            var importer = AssetImporter.GetAtPath(path);
            string beforeMetadata = File.ReadAllText(path + ".meta");
            if (EditorJsonUtility.ToJson(importer) != (string)baseline["rollback"])
                throw new InvalidOperationException("Importer changed since inventory: " + path);
            var result = new JObject { ["path"] = path, ["guid"] = baseline["guid"], ["category"] = item["category"], ["before"] = baseline["settings"].DeepClone(), ["rollback"] = baseline["rollback"], ["reason"] = item["reason"] };
            string preview = OUTPUT + "/" + Path.GetFileNameWithoutExtension(path);
            try
            {
            if (importer is TextureImporter t)
            {
                if (t.GetPlatformTextureSettings("Standalone").overridden || t.isReadable)
                    throw new InvalidOperationException("Requires override/CPU-access review: " + path);
                var beforeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var before = Readback(beforeTexture);
                long beforeMemory = Profiler.GetRuntimeMemorySizeLong(beforeTexture);
                File.WriteAllBytes(preview + "-Before.png", before.EncodeToPNG());
                t.textureCompression = (string)item["compression"] == "high" ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Compressed;
                t.crunchedCompression = false;
                EditorUtility.SetDirty(t);
                t.SaveAndReimport();
                var afterTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var after = Readback(afterTexture);
                var a = before.GetPixels32();
                var b = after.GetPixels32();
                if (a.Length != b.Length) throw new InvalidOperationException("Texture dimensions changed: " + path);
                double sum = 0;
                int maxAlpha = 0;
                for (int i = 0; i < a.Length; i++)
                {
                    sum += Math.Pow(a[i].r - b[i].r, 2) + Math.Pow(a[i].g - b[i].g, 2) + Math.Pow(a[i].b - b[i].b, 2);
                    maxAlpha = Math.Max(maxAlpha, Math.Abs(a[i].a - b[i].a));
                }
                result["rgbRmse8Bit"] = Math.Sqrt(sum / (a.Length * 3));
                result["maxAlphaError8Bit"] = maxAlpha;
                result["beforeEditorMemoryBytes"] = beforeMemory;
                result["afterEditorMemoryBytes"] = Profiler.GetRuntimeMemorySizeLong(afterTexture);
                result["afterFormat"] = afterTexture.format.ToString();
                result["afterCompression"] = t.textureCompression.ToString();
                File.WriteAllBytes(preview + "-After.png", after.EncodeToPNG());
                result["retained"] = (long)result["afterEditorMemoryBytes"] < beforeMemory;
                if (!(bool)result["retained"])
                {
                    t.textureCompression = (TextureImporterCompression)Enum.Parse(typeof(TextureImporterCompression), (string)baseline["settings"]["compression"]);
                    EditorUtility.SetDirty(t);
                    t.SaveAndReimport();
                }
                UnityEngine.Object.DestroyImmediate(before);
                UnityEngine.Object.DestroyImmediate(after);
            }
            else if (importer is ModelImporter m)
            {
                var meshes = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
                var vertices = meshes.Select(mesh => mesh.vertices).ToArray();
                var uvs = meshes.Select(mesh => mesh.uv).ToArray();
                m.meshCompression = ModelImporterMeshCompression.Medium;
                EditorUtility.SetDirty(m);
                m.SaveAndReimport();
                var after = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().ToArray();
                bool exact = meshes.Length == after.Length;
                double maxPositionError = 0;
                for (int i = 0; i < after.Length && i < vertices.Length; i++)
                {
                    exact &= vertices[i].SequenceEqual(after[i].vertices) && uvs[i].SequenceEqual(after[i].uv);
                    for (int v = 0; v < Math.Min(vertices[i].Length, after[i].vertexCount); v++)
                        maxPositionError = Math.Max(maxPositionError, Vector3.Distance(vertices[i][v], after[i].vertices[v]));
                }
                result["maxPositionErrorModelUnits"] = maxPositionError;
                result["exactGeometry"] = exact;
                // Existing import evidence preserves exact geometry; record the pilot as an exception.
                m.meshCompression = (ModelImporterMeshCompression)Enum.Parse(typeof(ModelImporterMeshCompression), (string)baseline["settings"]["compression"]);
                EditorUtility.SetDirty(m);
                m.SaveAndReimport();
                File.WriteAllText(path + ".meta", beforeMetadata);
                AssetDatabase.Refresh();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                result["retained"] = "Original mesh settings; source geometry and attachment precision require acceptance.";
            }
            else if (importer is AudioImporter audio)
            {
                if (audio.ContainsSampleSettingsOverride("Standalone"))
                    throw new InvalidOperationException("Requires audio override review: " + path);
                var beforeClip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                beforeClip.LoadAudioData();
                var before = new float[Math.Min(beforeClip.samples, beforeClip.frequency * 8) * beforeClip.channels];
                if (!beforeClip.GetData(before, 0)) throw new InvalidOperationException("Cannot decode baseline: " + path);
                var sampleSettings = audio.defaultSampleSettings;
                sampleSettings.compressionFormat = (string)item["format"] == "adpcm" ? AudioCompressionFormat.ADPCM : AudioCompressionFormat.Vorbis;
                if (sampleSettings.compressionFormat == AudioCompressionFormat.Vorbis) sampleSettings.quality = 0.7f;
                sampleSettings.loadType = AudioClipLoadType.DecompressOnLoad;
                audio.defaultSampleSettings = sampleSettings;
                EditorUtility.SetDirty(audio);
                audio.SaveAndReimport();
                var decoded = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                decoded.LoadAudioData();
                var after = new float[before.Length];
                if (!decoded.GetData(after, 0)) throw new InvalidOperationException("Cannot decode candidate: " + path);
                double signal = 0, error = 0;
                for (int i = 0; i < before.Length; i++)
                {
                    signal += before[i] * before[i];
                    error += Math.Pow(before[i] - after[i], 2);
                }
                result["first8SecondsSnrDb"] = error == 0 ? "identical" : (JToken)(10 * Math.Log10(signal / error));
                result["sampleCount"] = before.Length;
                result["channels"] = decoded.channels;
                result["frequency"] = decoded.frequency;
                sampleSettings.loadType = (string)item["loadType"] == "streaming" ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                audio.defaultSampleSettings = sampleSettings;
                EditorUtility.SetDirty(audio);
                audio.SaveAndReimport();
                bool keep = error == 0 || 10 * Math.Log10(signal / error) >= 20;
                result["retained"] = keep;
                if (!keep)
                {
                    audio.defaultSampleSettings = baseline["settings"].ToObject<AudioImporterSampleSettings>();
                    EditorUtility.SetDirty(audio);
                    audio.SaveAndReimport();
                    File.WriteAllText(path + ".meta", beforeMetadata);
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
                result["after"] = JObject.FromObject(audio.defaultSampleSettings);
                result["acceptance"] = "Listening, startup/loop/crossfade and DSP CPU acceptance pending; do not batch audio yet.";
            }
            else throw new InvalidOperationException("Unsupported pilot importer: " + path);
            }
            catch
            {
                if (importer is TextureImporter texture) texture.textureCompression = (TextureImporterCompression)Enum.Parse(typeof(TextureImporterCompression), (string)baseline["settings"]["compression"]);
                if (importer is ModelImporter model) model.meshCompression = (ModelImporterMeshCompression)Enum.Parse(typeof(ModelImporterMeshCompression), (string)baseline["settings"]["compression"]);
                if (importer is AudioImporter clipImporter) clipImporter.defaultSampleSettings = baseline["settings"].ToObject<AudioImporterSampleSettings>();
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                AssetDatabase.SaveAssets();
                throw;
            }
            results.Add(result);
            AssetDatabase.SaveAssets();
            File.WriteAllText(OUTPUT + "/Pilot.json", results.ToString(Formatting.Indented));
        }
        return "Pilot completed; detailed results: " + OUTPUT + "/Pilot.json";
    }

    private static Texture2D Readback(Texture2D source)
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
            return copy;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
