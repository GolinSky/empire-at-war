using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.ViewComponents.Weapon;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ProbeLiveShots
{
    private const int SAMPLES_PER_PREFAB = 12;
    private const string OUTPUT_FOLDER = "Logs/LateBattleCpu";

    public static object Start()
    {
        RunInBackground();
        return new { status = "running", samples = SAMPLES_PER_PREFAB * 7 };
    }

    private static async void RunInBackground()
    {
        object result = await Main();
        UnityEngine.Debug.Log(JsonConvert.SerializeObject(result));
    }

    public static async Task<object> Main()
    {
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("Live shot profiling requires an already running battle.");

        WeaponsData weapons = AssetDatabase.LoadAssetAtPath<WeaponsData>(
            "Assets/Settings/Data/Models/Weapon/WeaponsData.asset");
        ShotEffect[] prefabs = weapons.Weapons.Select(definition => weapons.GetProfile(definition.WeaponType).ShotPrefab)
            .Distinct().OrderBy(prefab => AssetDatabase.GetAssetPath(prefab)).ToArray();
        var instantiateMarkers = prefabs.Select(prefab => new ProfilerMarker(
            "Battle.Projectile.Probe.Instantiate." + prefab.name)).ToArray();
        var resetMarkers = prefabs.Select(prefab => new ProfilerMarker(
            "Battle.Projectile.Probe.Reset." + prefab.name)).ToArray();
        var rows = new List<object>();
        string sceneName = SceneManager.GetActiveScene().name;
        int firstFrame = Time.frameCount;
        for (int sample = 0; sample < SAMPLES_PER_PREFAB; sample++)
        {
            for (int index = 0; index < prefabs.Length; index++)
            {
                await Awaitable.NextFrameAsync();
                if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().name != sceneName)
                    throw new InvalidOperationException("Live shot profiling stopped because the running scene changed.");
                ShotEffect clone = null;
                double instantiateMs;
                double resetMs;
                try
                {
                    long start = Stopwatch.GetTimestamp();
                    using (instantiateMarkers[index].Auto()) clone = Object.Instantiate(prefabs[index]);
                    instantiateMs = Milliseconds(start);
                    start = Stopwatch.GetTimestamp();
                    using (resetMarkers[index].Auto()) clone.ResetForPool();
                    resetMs = Milliseconds(start);
                }
                finally
                {
                    if (clone != null) Object.Destroy(clone.gameObject);
                }
                rows.Add(new
                {
                    frame = Time.frameCount,
                    gameTime = Time.time,
                    sample,
                    prefab = AssetDatabase.GetAssetPath(prefabs[index]),
                    name = prefabs[index].name,
                    instantiateMs,
                    resetMs
                });
            }
        }
        await Awaitable.NextFrameAsync();
        Directory.CreateDirectory(OUTPUT_FOLDER);
        string path = OUTPUT_FOLDER + "/live-shot-probe-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ") + ".json";
        var report = new
        {
            capturedUtc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            processor = SystemInfo.processorType,
            operatingSystem = SystemInfo.operatingSystem,
            scene = sceneName,
            firstFrame,
            lastFrame = Time.frameCount,
            mode = "Live battle; actual prefab including scripts; one unfired clone per frame after NextFrameAsync; ResetForPool then deferred Destroy",
            note = "NextFrameAsync resumes at Unity's awaitable continuation point, not the service's exact ordered Tick slot. Probe markers distinguish these samples from genuine cache prewarm and on-demand samples.",
            samplesPerPrefab = SAMPLES_PER_PREFAB,
            rows
        };
        File.WriteAllText(path, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { path, firstFrame, lastFrame = Time.frameCount, samples = rows.Count };
    }

    private static double Milliseconds(long start) =>
        (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
}
