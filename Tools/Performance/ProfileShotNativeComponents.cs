using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Weapon;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ProfileShotNativeComponents
{
    private const int WARM_SAMPLES = 12;
    private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";
    private const string OUTPUT_FOLDER = "Logs/LateBattleCpu";
    private static readonly string[] VARIANTS =
    {
        "native-all", "native-no-particle-renderers", "native-no-particles",
        "native-no-lines", "native-no-lights", "native-transforms-only", "native-inactive"
    };

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Shot native-component isolation profiling requires Edit Mode.");

        WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
        ShotEffect[] prefabs = weapons.Weapons.Select(definition => weapons.GetProfile(definition.WeaponType).ShotPrefab)
            .Distinct().OrderBy(prefab => AssetDatabase.GetAssetPath(prefab)).ToArray();
        var rows = new List<object>();
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (ShotEffect prefab in prefabs)
            {
                foreach (string variant in VARIANTS)
                {
                    GameObject template = Object.Instantiate(prefab.gameObject,
                        new InstantiateParameters { scene = scene });
                    try
                    {
                        // Strip scripts first while their OnDisable/OnDestroy dependencies are intact.
                        // All mutation is confined to this disposable preview-scene template.
                        foreach (MonoBehaviour script in template.GetComponentsInChildren<MonoBehaviour>(true))
                            Object.DestroyImmediate(script);
                        if (variant == "native-no-particle-renderers" || variant == "native-no-particles")
                            foreach (ParticleSystemRenderer renderer in template.GetComponentsInChildren<ParticleSystemRenderer>(true))
                                Object.DestroyImmediate(renderer);
                        if (variant == "native-no-particles")
                            foreach (ParticleSystem particles in template.GetComponentsInChildren<ParticleSystem>(true))
                                Object.DestroyImmediate(particles);
                        if (variant == "native-no-lines")
                            foreach (LineRenderer line in template.GetComponentsInChildren<LineRenderer>(true))
                                Object.DestroyImmediate(line);
                        if (variant == "native-no-lights")
                            foreach (Light light in template.GetComponentsInChildren<Light>(true))
                                Object.DestroyImmediate(light);
                        if (variant == "native-transforms-only")
                        {
                            foreach (Renderer renderer in template.GetComponentsInChildren<Renderer>(true))
                                Object.DestroyImmediate(renderer);
                            foreach (ParticleSystem particles in template.GetComponentsInChildren<ParticleSystem>(true))
                                Object.DestroyImmediate(particles);
                            foreach (Component component in template.GetComponentsInChildren<Component>(true))
                                if (!(component is Transform)) Object.DestroyImmediate(component);
                        }
                        if (variant == "native-inactive") template.SetActive(false);

                        var samples = new List<double>();
                        for (int sample = 0; sample <= WARM_SAMPLES; sample++)
                        {
                            GameObject clone = null;
                            try
                            {
                                long start = Stopwatch.GetTimestamp();
                                clone = Object.Instantiate(template, new InstantiateParameters { scene = scene });
                                samples.Add((Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency);
                            }
                            finally
                            {
                                if (clone != null) Object.DestroyImmediate(clone);
                            }
                        }
                        double[] sorted = samples.Skip(1).OrderBy(value => value).ToArray();
                        rows.Add(new
                        {
                            prefab = AssetDatabase.GetAssetPath(prefab),
                            name = prefab.name,
                            variant,
                            firstInstantiateMs = samples[0],
                            warmMedianMs = (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) * 0.5,
                            warmMeanMs = sorted.Average(),
                            warmMinimumMs = sorted[0],
                            warmMaximumMs = sorted[sorted.Length - 1],
                            rawInstantiateMs = samples,
                            remainingComponents = template.GetComponentsInChildren<Component>(true)
                                .GroupBy(component => component.GetType().Name)
                                .Select(group => new { type = group.Key, count = group.Count() }).ToArray()
                        });
                    }
                    finally
                    {
                        Object.DestroyImmediate(template);
                    }
                }
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }

        Directory.CreateDirectory(OUTPUT_FOLDER);
        string path = OUTPUT_FOLDER + "/shot-native-components-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json";
        var report = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            processor = SystemInfo.processorType,
            mode = "EditMode isolated preview scene; native-only disposable templates; no battle load; timings include full Instantiate, not only Awake",
            warmSamples = WARM_SAMPLES,
            rows
        };
        File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { path, rows };
    }
}
