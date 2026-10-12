using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class InspectTectorReplacement
{
    private const string OUTPUT = "F:/CodexArtifacts/tector-preview/UnityReplacement/";
    private static string PathOf(Transform transform, Transform root)
    {
        return transform == root ? "" : PathOf(transform.parent, root) + "/" + transform.name;
    }

    public static string Main() => Capture("UnityBaseline.json");
    public static string Verify() => Capture("UnityAfter.json");
    public static string State() => new JObject { ["playing"] = EditorApplication.isPlaying, ["transitioning"] = EditorApplication.isPlayingOrWillChangePlaymode, ["compiling"] = EditorApplication.isCompiling }.ToString();
    private static string Capture(string filename)
    {
        Directory.CreateDirectory(OUTPUT);
        var report = new JObject();
        foreach (string path in new[] { "Assets/Prefabs/Models/Ships/Tector.prefab", "Assets/Prefabs/Models/Ships/TectorShipView.prefab", "Assets/Prefabs/Models/Wrecks/TectorWreckView.prefab", "Assets/Prefabs/Ui/Reinforcement/TectorReinforcementView.prefab" })
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var transforms = root.GetComponentsInChildren<Transform>(true);
            var components = root.GetComponentsInChildren<MonoBehaviour>(true);
            var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
            var bounds = renderers.Where(r => r.enabled).First().bounds;
            foreach (var renderer in renderers.Where(r => r.enabled).Skip(1)) bounds.Encapsulate(renderer.bounds);
            report[path] = new JObject
            {
                ["guid"] = AssetDatabase.AssetPathToGUID(path),
                ["boundsCenter"] = JArray.FromObject(new[] { bounds.center.x, bounds.center.y, bounds.center.z }),
                ["boundsSize"] = JArray.FromObject(new[] { bounds.size.x, bounds.size.y, bounds.size.z }),
                ["transforms"] = new JArray(transforms.Select(t => new JObject
                {
                    ["path"] = PathOf(t, root.transform), ["position"] = JArray.FromObject(new[] { t.position.x, t.position.y, t.position.z }),
                    ["localPosition"] = JArray.FromObject(new[] { t.localPosition.x, t.localPosition.y, t.localPosition.z }),
                    ["localScale"] = JArray.FromObject(new[] { t.localScale.x, t.localScale.y, t.localScale.z })
                })),
                ["components"] = new JArray(components.Select(c => new JObject { ["type"] = c.GetType().Name, ["path"] = PathOf(c.transform, root.transform), ["json"] = EditorJsonUtility.ToJson(c) })),
                ["renderers"] = new JArray(renderers.Select(r => new JObject
                {
                    ["path"] = PathOf(r.transform, root.transform), ["enabled"] = r.enabled,
                    ["materials"] = JArray.FromObject(r.sharedMaterials.Select(AssetDatabase.GetAssetPath)),
                    ["mesh"] = AssetDatabase.GetAssetPath(r.GetComponents<MeshFilter>().Single().sharedMesh)
                }))
            };
        }
        File.WriteAllText(OUTPUT + filename, report.ToString());
        return new JObject(report.Properties().Select(p => new JProperty(p.Name, new JObject
        {
            ["guid"] = p.Value["guid"], ["boundsSize"] = p.Value["boundsSize"],
            ["componentCount"] = p.Value["components"].Count(),
            ["rendererCount"] = p.Value["renderers"].Count()
        }))).ToString();
    }
}
