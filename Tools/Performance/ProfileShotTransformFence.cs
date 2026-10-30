using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;
using Object = UnityEngine.Object;

public static class ProfileShotTransformFence
{
    private const int WARM_SAMPLES = 6;
    private const int BACKGROUND_TRANSFORMS = 8;
    private const int WORK_ITERATIONS = 1000000;
    private const string OUTPUT_FOLDER = "Logs/LateBattleCpu";

    public struct BackgroundTransformWork : IJobParallelForTransform
    {
        public int Iterations;

        public void Execute(int index, TransformAccess transform)
        {
            double sum = 0.0;
            for (int i = 1; i <= Iterations; i++) sum += Math.Sin(i * 0.001 + index);
            transform.localPosition = new Vector3((float)sum, index, 0f);
        }
    }

    public static object Main()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Transform-fence isolation profiling requires Edit Mode.");

        var scene = EditorSceneManager.NewPreviewScene();
        var parameters = new InstantiateParameters { scene = scene };
        var transforms = new TransformAccessArray(BACKGROUND_TRANSFORMS);
        JobHandle work = default;
        var rows = new List<object>();
        try
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vfx/Projectiles/Projectile.prefab");
            GameObject bare = Object.Instantiate(source, parameters);
            RemoveScripts(bare);
            foreach (Renderer renderer in bare.GetComponentsInChildren<Renderer>(true)) Object.DestroyImmediate(renderer);
            foreach (ParticleSystem particles in bare.GetComponentsInChildren<ParticleSystem>(true)) Object.DestroyImmediate(particles);
            for (int i = bare.transform.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(bare.transform.GetChild(i).gameObject);
            foreach (Component component in bare.GetComponents<Component>())
                if (!(component is Transform)) Object.DestroyImmediate(component);

            for (int i = 0; i < BACKGROUND_TRANSFORMS; i++)
                transforms.Add(Object.Instantiate(bare, parameters).transform);

            var templates = new Dictionary<string, GameObject> { { "bare-transform", bare } };
            GameObject particleTemplate = Object.Instantiate(bare, parameters);
            ParticleSystem particleSystem = particleTemplate.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.playOnAwake = false;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            templates.Add("particle-system", particleTemplate);
            GameObject lineTemplate = Object.Instantiate(bare, parameters);
            lineTemplate.AddComponent<LineRenderer>();
            templates.Add("line-renderer", lineTemplate);
            GameObject lightTemplate = Object.Instantiate(bare, parameters);
            lightTemplate.AddComponent<Light>();
            templates.Add("light", lightTemplate);
            foreach (string prefab in new[] { "Projectile", "LaserProjectile", "ProtonTorpedoShot" })
            {
                GameObject template = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/Vfx/Projectiles/" + prefab + ".prefab"), parameters);
                RemoveScripts(template);
                templates.Add(prefab, template);
            }

            foreach (KeyValuePair<string, GameObject> template in templates)
            {
                foreach (bool completeBeforeInstantiate in new[] { false, true })
                {
                    var samples = new List<double[]>();
                    for (int sample = 0; sample <= WARM_SAMPLES; sample++)
                    {
                        GameObject clone = null;
                        try
                        {
                            long totalStart = Stopwatch.GetTimestamp();
                            work = new BackgroundTransformWork { Iterations = WORK_ITERATIONS }.Schedule(transforms);
                            JobHandle.ScheduleBatchedJobs();
                            long start = Stopwatch.GetTimestamp();
                            if (completeBeforeInstantiate) work.Complete();
                            double beforeMs = Milliseconds(start);
                            start = Stopwatch.GetTimestamp();
                            clone = Object.Instantiate(template.Value, parameters);
                            double instantiateMs = Milliseconds(start);
                            start = Stopwatch.GetTimestamp();
                            work.Complete();
                            double afterMs = Milliseconds(start);
                            samples.Add(new[] { beforeMs, instantiateMs, afterMs, Milliseconds(totalStart) });
                        }
                        finally
                        {
                            work.Complete();
                            if (clone != null) Object.DestroyImmediate(clone);
                        }
                    }
                    rows.Add(new
                    {
                        template = template.Key,
                        mode = completeBeforeInstantiate ? "complete-then-instantiate" : "instantiate-with-job-pending",
                        columnNames = new[] { "preCompleteMs", "instantiateMs", "postCompleteMs", "totalMs" },
                        firstSample = samples[0],
                        warmMedians = Enumerable.Range(0, 4).Select(column => Median(samples.Skip(1)
                            .Select(sample => sample[column]).ToArray())).ToArray(),
                        rawSamples = samples
                    });
                }
            }
        }
        finally
        {
            work.Complete();
            transforms.Dispose();
            EditorSceneManager.ClosePreviewScene(scene);
        }

        Directory.CreateDirectory(OUTPUT_FOLDER);
        string path = OUTPUT_FOLDER + "/shot-transform-fence-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json";
        var report = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            processor = SystemInfo.processorType,
            mode = "EditMode preview scene; identical synthetic TransformAccessArray write work; independent clone hierarchy; no battle or user scene changes",
            note = "Compare Instantiate and job Complete separately AND total. Moving a wait is not a total speedup.",
            backgroundTransforms = BACKGROUND_TRANSFORMS,
            workIterationsPerTransform = WORK_ITERATIONS,
            warmSamples = WARM_SAMPLES,
            rows
        };
        File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { path, rows };
    }

    private static void RemoveScripts(GameObject root)
    {
        foreach (MonoBehaviour script in root.GetComponentsInChildren<MonoBehaviour>(true)) Object.DestroyImmediate(script);
    }

    private static double Milliseconds(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

    private static double Median(double[] values)
    {
        Array.Sort(values);
        return (values[values.Length / 2 - 1] + values[values.Length / 2]) * 0.5;
    }
}
