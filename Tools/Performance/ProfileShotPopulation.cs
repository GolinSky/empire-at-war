using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using EmpireAtWar.ViewComponents.Weapon;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using Object = UnityEngine.Object;

public static class ProfileShotPopulation
{
    private const int SAMPLES = 12;
    private const long MEMORY_LIMIT_BYTES = 384L * 1024 * 1024;
    private const string OUTPUT_FOLDER = "Logs/LateBattleCpu";
    private static readonly int[] POPULATIONS = { 0, 128, 512, 1024 };

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Shot population isolation profiling requires Edit Mode.");

        var rows = new List<object>();
        foreach (string prefabName in new[] { "Projectile", "LaserProjectile", "ProtonTorpedoShot" })
        {
            foreach (bool active in new[] { true, false })
            {
                var scene = EditorSceneManager.NewPreviewScene();
                var retained = new List<GameObject>();
                long memoryStart = Profiler.GetTotalAllocatedMemoryLong();
                bool memoryLimitReached = false;
                try
                {
                    var parameters = new InstantiateParameters { scene = scene };
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/Prefabs/Vfx/Projectiles/" + prefabName + ".prefab");
                    GameObject template = Object.Instantiate(prefab, parameters);
                    // Produce the same stopped visuals as the live pool, then retain native state only.
                    template.GetComponent<ShotEffect>().ResetForPool();
                    foreach (MonoBehaviour script in template.GetComponentsInChildren<MonoBehaviour>(true))
                        Object.DestroyImmediate(script);
                    template.SetActive(active);

                    foreach (int requestedPopulation in POPULATIONS)
                    {
                        long populateStart = Stopwatch.GetTimestamp();
                        while (retained.Count < requestedPopulation)
                        {
                            if ((retained.Count % 8) == 0 &&
                                Profiler.GetTotalAllocatedMemoryLong() - memoryStart >= MEMORY_LIMIT_BYTES)
                            {
                                memoryLimitReached = true;
                                break;
                            }
                            retained.Add(Object.Instantiate(template, parameters));
                        }
                        double populateMs = Milliseconds(populateStart);
                        var samples = new List<double>();
                        for (int sample = 0; sample < SAMPLES; sample++)
                        {
                            GameObject clone = null;
                            try
                            {
                                long start = Stopwatch.GetTimestamp();
                                clone = Object.Instantiate(template, parameters);
                                samples.Add(Milliseconds(start));
                            }
                            finally
                            {
                                if (clone != null) Object.DestroyImmediate(clone);
                            }
                        }
                        double[] sorted = samples.OrderBy(value => value).ToArray();
                        rows.Add(new
                        {
                            prefab = prefabName,
                            activeIdle = active,
                            requestedPopulation,
                            retainedPopulation = retained.Count,
                            retainedParticleSystems = retained.Count * template.GetComponentsInChildren<ParticleSystem>(true).Length,
                            populateMs,
                            instantiateMedianMs = (sorted[SAMPLES / 2 - 1] + sorted[SAMPLES / 2]) * 0.5,
                            instantiateMeanMs = sorted.Average(),
                            instantiateMinimumMs = sorted[0],
                            instantiateMaximumMs = sorted[SAMPLES - 1],
                            rawInstantiateMs = samples,
                            allocatedMemoryDeltaBytes = Profiler.GetTotalAllocatedMemoryLong() - memoryStart,
                            memoryLimitReached
                        });
                        if (memoryLimitReached) break;
                    }
                }
                finally
                {
                    EditorSceneManager.ClosePreviewScene(scene);
                }
            }
        }

        Directory.CreateDirectory(OUTPUT_FOLDER);
        string path = OUTPUT_FOLDER + "/shot-population-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json";
        var report = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            processor = SystemInfo.processorType,
            mode = "EditMode preview scenes; stopped native-only shot clones; active/inactive populations; synchronous single-frame diagnostic without battle or particle simulation",
            memoryLimitBytes = MEMORY_LIMIT_BYTES,
            samplesPerPopulation = SAMPLES,
            rows
        };
        File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { path, rows };
    }

    private static double Milliseconds(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
}
