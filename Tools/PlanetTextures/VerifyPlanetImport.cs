using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;

public static class VerifyPlanetImport
{
    public static string Main()
    {
        var imports = JObject.Parse(File.ReadAllText("Temp/PlanetImport/imported-textures.json"));
        var textures = new JArray();
        foreach (var path in ((JObject)imports["textures"]).Properties().Concat(((JObject)imports["previews"]).Properties()))
        {
            string asset = (string)path.Value;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(asset);
            bool validFormat = texture.format == TextureFormat.BC7 || texture.format == TextureFormat.RGB24 || texture.format == TextureFormat.RGBA32;
            if (texture.isReadable || !validFormat)
                throw new InvalidOperationException("Texture optimization missing: " + asset);
            textures.Add(new JObject { ["path"] = asset, ["width"] = texture.width, ["height"] = texture.height,
                ["mips"] = texture.mipmapCount, ["format"] = texture.format.ToString(),
                ["editorMemoryBytes"] = Profiler.GetRuntimeMemorySizeLong(texture) });
        }
        var settings = AssetDatabase.LoadAssetAtPath<MapGenerationSettings>("Assets/Settings/Data/Models/Map/MapGenerationSettings.asset");
        var planets = new JArray();
        foreach (PlanetType type in Enum.GetValues(typeof(PlanetType)))
        {
            var prefab = settings.GetPlanetPrefab(type);
            using (var view = new SerializedObject(prefab.View))
            {
                var surface = (MeshRenderer)view.FindProperty("planetRenderers").GetArrayElementAtIndex(0).objectReferenceValue;
                var mesh = surface.GetComponent<MeshFilter>().sharedMesh;
                planets.Add(new JObject { ["planet"] = type.ToString(), ["id"] = (int)type,
                    ["prefab"] = AssetDatabase.GetAssetPath(prefab), ["surfaceMesh"] = AssetDatabase.GetAssetPath(mesh),
                    ["vertices"] = mesh.vertexCount, ["cpuReadable"] = mesh.isReadable,
                    ["cloudShells"] = view.FindProperty("cloudRenderers").arraySize });
            }
        }
        var report = new JObject { ["planets"] = planets, ["textures"] = textures,
            ["textureEditorMemoryBytes"] = textures.Sum(t => (long)t["editorMemoryBytes"]),
            ["verification"] = "Saved prefab/catalog references and imported Windows textures; isolated renders inspected. Editor memory sum is not a packed-player residency measurement." };
        File.WriteAllText("Temp/PlanetImport/verification.json", report.ToString());
        return report["textureEditorMemoryBytes"] + " bytes across " + textures.Count + " textures/previews; " + planets.Count + " planet bindings.";
    }
}
