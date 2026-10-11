using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;
using UnityEngine.Profiling;

public static class DesktopCompressionInventory
{
    public static string Main()
    {
        const string OUTPUT = "output/DesktopAssetCompression";
        Directory.CreateDirectory(OUTPUT);
        var roots = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToList();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        foreach (var group in settings.groups.Where(g => g != null))
            foreach (var entry in group.entries)
                roots.Add(entry.AssetPath);
        roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/Resources/") && !AssetDatabase.IsValidFolder(p)));
        var shipped = new HashSet<string>(AssetDatabase.GetDependencies(roots.Distinct().ToArray(), true));
        var materialUses = new Dictionary<string, List<string>>();
        foreach (var path in shipped.Where(p => p.EndsWith(".mat")))
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            foreach (var property in material.GetTexturePropertyNames())
            {
                var texture = material.GetTexture(property);
                if (texture == null) continue;
                var texturePath = AssetDatabase.GetAssetPath(texture);
                if (!materialUses.ContainsKey(texturePath)) materialUses[texturePath] = new List<string>();
                materialUses[texturePath].Add(path + ":" + material.shader.name + ":" + property);
            }
        }
        var records = new JArray();
        foreach (var path in AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith("Assets/Art/Textures/") || p.StartsWith("Assets/Art/Models/") || p.StartsWith("Assets/Audio/")))
        {
            var importer = AssetImporter.GetAtPath(path);
            if (!(importer is TextureImporter) && !(importer is ModelImporter) && !(importer is AudioImporter)) continue;
            var record = new JObject { ["path"] = path, ["guid"] = AssetDatabase.AssetPathToGUID(path), ["dependency"] = shipped.Contains(path), ["importer"] = importer.GetType().Name, ["rollback"] = EditorJsonUtility.ToJson(importer) };
            if (importer is TextureImporter t)
            {
                var platform = t.GetPlatformTextureSettings("Standalone");
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                record["settings"] = JObject.FromObject(new { compression = t.textureCompression.ToString(), t.crunchedCompression, t.maxTextureSize, t.mipmapEnabled, t.sRGBTexture, t.isReadable, type = t.textureType.ToString(), platform.overridden, format = platform.format.ToString(), platformMaxSize = platform.maxTextureSize, platform.compressionQuality });
                record["windowsFormat"] = t.GetAutomaticFormat("Standalone").ToString();
                record["uses"] = JArray.FromObject(materialUses.ContainsKey(path) ? materialUses[path] : new List<string>());
                record["imported"] = JObject.FromObject(new { texture.width, texture.height, texture.mipmapCount, format = texture.format.ToString(), editorMemoryBytes = Profiler.GetRuntimeMemorySizeLong(texture) });
                record["category"] = path.StartsWith("Assets/Art/Textures/Models/") ? "model-review" : "ui-vfx-data-review";
            }
            if (importer is ModelImporter m)
            {
                record["settings"] = JObject.FromObject(new { compression = m.meshCompression.ToString(), m.isReadable, animation = m.animationType.ToString() });
                record["meshes"] = JArray.FromObject(AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().Select(mesh => new { mesh.name, mesh.vertexCount, mesh.subMeshCount, mesh.blendShapeCount, editorMemoryBytes = Profiler.GetRuntimeMemorySizeLong(mesh) }));
                record["category"] = "precision-rig-collider-review";
            }
            if (importer is AudioImporter a)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                record["settings"] = JObject.FromObject(a.defaultSampleSettings);
                record["standaloneOverride"] = a.ContainsSampleSettingsOverride("Standalone");
                record["standalone"] = JObject.FromObject(a.GetOverrideSampleSettings("Standalone"));
                record["flags"] = JObject.FromObject(new { a.forceToMono, a.loadInBackground, a.ambisonic });
                record["clip"] = JObject.FromObject(new { clip.length, clip.channels, clip.frequency, clip.samples, loadType = clip.loadType.ToString(), editorMemoryBytes = Profiler.GetRuntimeMemorySizeLong(clip) });
                record["category"] = path.Contains("/Music/Ship/") ? "engine-loop" : path.Contains("/Music/") ? "music-review" : path.Contains("/Voices/") ? "voice-review" : "sfx-review";
            }
            records.Add(record);
        }
        var report = new JObject { ["unity"] = Application.unityVersion, ["target"] = EditorUserBuildSettings.activeBuildTarget.ToString(), ["graphicsApi"] = SystemInfo.graphicsDeviceType.ToString(), ["gpu"] = SystemInfo.graphicsDeviceName, ["roots"] = JArray.FromObject(roots.Distinct()), ["atlasPaths"] = JArray.FromObject(shipped.Where(p => p.EndsWith(".spriteatlas") || p.EndsWith(".spriteatlasv2"))), ["assets"] = records };
        File.WriteAllText(OUTPUT + "/Inventory.json", report.ToString(Formatting.Indented));
        return JsonConvert.SerializeObject(new { total = records.Count, dependencies = records.Count(r => (bool)r["dependency"]), byImporter = records.GroupBy(r => (string)r["importer"]).ToDictionary(g => g.Key, g => g.Count()), report = OUTPUT + "/Inventory.json" });
    }
}
