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

public static class ProfileShotPrefabs
{
    private const int WARM_SAMPLES = 12;
    private const string WEAPONS_PATH = "Assets/Settings/Data/Models/Weapon/WeaponsData.asset";
    private const string OUTPUT_FOLDER = "Logs/LateBattleCpu";

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Shot prefab isolation profiling requires Edit Mode.");

        WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(WEAPONS_PATH);
        var profiles = weapons.Weapons.Select(definition => weapons.GetProfile(definition.WeaponType)).ToList();
        ShotEffect[] prefabs = profiles.Select(profile => profile.ShotPrefab).Distinct()
            .OrderBy(prefab => AssetDatabase.GetAssetPath(prefab)).ToArray();
        var rows = new List<object>();
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (ShotEffect prefab in prefabs)
            {
                var instantiate = new List<double>();
                var reset = new List<double>();
                for (int sample = 0; sample <= WARM_SAMPLES; sample++)
                {
                    GameObject clone = null;
                    try
                    {
                        long start = Stopwatch.GetTimestamp();
                        clone = Object.Instantiate(prefab.gameObject, new InstantiateParameters { scene = scene });
                        instantiate.Add(Milliseconds(start));
                        // Editor diagnostic lookup is outside either timed operation.
                        ShotEffect effect = clone.GetComponent<ShotEffect>();
                        start = Stopwatch.GetTimestamp();
                        effect.ResetForPool();
                        reset.Add(Milliseconds(start));
                    }
                    finally
                    {
                        if (clone != null) Object.DestroyImmediate(clone);
                    }
                }
                ParticleSystem[] particles = prefab.GetComponentsInChildren<ParticleSystem>(true);
                rows.Add(new
                {
                    prefab = AssetDatabase.GetAssetPath(prefab),
                    name = prefab.name,
                    weaponTypes = profiles.Where(profile => profile.ShotPrefab == prefab)
                        .Select(profile => profile.WeaponType.ToString()).Distinct().ToArray(),
                    gameObjects = prefab.GetComponentsInChildren<Transform>(true).Length,
                    particleSystems = particles.Select(particle => new
                    {
                        name = particle.name,
                        maximumParticles = particle.main.maxParticles,
                        playOnAwake = particle.main.playOnAwake,
                        prewarm = particle.main.prewarm,
                        cullingMode = particle.main.cullingMode.ToString()
                    }).ToArray(),
                    lights = prefab.GetComponentsInChildren<Light>(true).Length,
                    renderers = prefab.GetComponentsInChildren<Renderer>(true)
                        .Select(renderer => renderer.GetType().Name).ToArray(),
                    scripts = prefab.GetComponentsInChildren<MonoBehaviour>(true)
                        .Select(script => script.GetType().FullName).ToArray(),
                    firstInstantiateMs = instantiate[0],
                    firstResetMs = reset[0],
                    warmInstantiateMs = Summary(instantiate.Skip(1).ToArray()),
                    warmResetMs = Summary(reset.Skip(1).ToArray()),
                    rawInstantiateMs = instantiate,
                    rawResetMs = reset
                });
            }
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }

        Directory.CreateDirectory(OUTPUT_FOLDER);
        string path = OUTPUT_FOLDER + "/shot-prefabs-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json";
        var report = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            processor = SystemInfo.processorType,
            operatingSystem = SystemInfo.operatingSystem,
            mode = "EditMode isolated preview scene; one clone at a time; no battle load; first call is not an asset-cache reset",
            warmSamples = WARM_SAMPLES,
            rows
        };
        File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { path, report };
    }

    private static double Milliseconds(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

    private static object Summary(double[] values)
    {
        Array.Sort(values);
        return new
        {
            mean = values.Average(),
            median = (values[values.Length / 2 - 1] + values[values.Length / 2]) * 0.5,
            minimum = values[0],
            maximum = values[values.Length - 1]
        };
    }
}
