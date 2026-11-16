using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Enemy;
using EmpireAtWar.Services.VContainer;
using Unity.Profiling;
using UnityEngine;
using VContainer;

/// <summary>
/// Shared Team AI scaling probe: Full Large seed 1875278133, human + N UltraHard AIs (alternating enemy/ally teams).
/// Launch(n) from the main menu in Play Mode; then Start(warmup, seconds) writes Logs/TeamAiProfile/ai{n}.json.
/// </summary>
public static class ProfileTeamAi
{
    private const int SEED = 1875278133;
    private static readonly string[] MARKERS =
    {
        "EnemyAI.Scheduler.Tick", "EnemyAI.WorldSnapshot.Rebuild", "EnemyAI.Ship.AssessStation",
        "EnemyAI.Ship.Reconcile", "EnemyAI.Ship.PatrolSample", "EnemyAI.Ship.PatrolSelect", "EnemyAI.Ship.Telemetry", "EnemyAI.Ship.Build", "EnemyAI.Ship.Executor", "EnemyAI.Ship.CaptureProbe",
        "EnemyAI.Context.SelectCaptureObjective", "EnemyAI.Context.FindRaidTarget", "EnemyAI.Context.ForceBuilds",
        "EnemyAI.Production.Evaluate"
    };

    public static object Launch(int aiCount)
    {
        var players = new List<PlayerSlot>
        {
            new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                PlayerController.Human, EnemyAiDifficulty.Medium, 0)
        };
        for (int i = 1; i <= aiCount; i++)
        {
            bool enemy = i % 2 == 1;
            players.Add(new PlayerSlot(new PlayerId(i), new TeamId(enemy ? 1 : 0),
                enemy ? FactionType.Separatist : FactionType.Republic,
                PlayerController.Ai, EnemyAiDifficulty.UltraHard, i));
        }
        var builder = SkirmishPreset.Create(players, PlanetType.Coruscant, MapSize.Large,
            BattleVictoryCondition.DestroyOpponentBase, 20000f, false, SEED);
        Object.FindAnyObjectByType<ProjectScope>().Container.Resolve<IGameCommand>().StartBattle(builder.Build());
        return new { seed = SEED, size = "Large", aiCount };
    }

    public static object Status()
    {
        var scope = Object.FindAnyObjectByType<SkirmishScope>();
        if (scope == null) return new { scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name, state = "NoBattle" };
        return new
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            state = scope.Container.Resolve<EmpireAtWar.Controllers.Game.SkirmishStateModel>().State.ToString(),
            time = Time.time,
            aiScopes = Object.FindObjectsByType<AiPlayerScope>().Length,
            ships = Object.FindObjectsByType<EmpireAtWar.Ship.Ship>().Length
        };
    }

    public static object Start(float warmupSeconds, float seconds)
    {
        Run(warmupSeconds, seconds);
        return new { started = true, warmupSeconds, seconds };
    }

    private static async void Run(float warmupSeconds, float seconds)
    {
        string dir = Path.Combine("Logs", "TeamAiProfile");
        Directory.CreateDirectory(dir);
        int aiCount = Object.FindObjectsByType<AiPlayerScope>().Length;
        string path = Path.Combine(dir, $"ai{aiCount}.json");
        File.WriteAllText(path, "{\"status\":\"running\"}");
        float warmupEnd = Time.time + warmupSeconds;
        while (Time.time < warmupEnd) await Awaitable.NextFrameAsync();

        var scheduler = Object.FindAnyObjectByType<SkirmishScope>().Container.Resolve<EnemyAiScheduler>();
        int starvedStart = scheduler.StarvationRunCount;
        int unitsStart = scheduler.ExecutedUnitCount;
        var names = MARKERS.ToList();
        foreach (string group in new[] { "Ship.Decision", "Squadron.Periodic", "Squadron.Launch" })
            for (int id = 1; id <= aiCount; id++) names.Add($"EnemyAI.{group}.Player{id}");
        var recorders = names.Select(name => ProfilerRecorder.StartNew(ProfilerCategory.Scripts, name, 1)).ToArray();
        var samples = names.Select(_ => new List<double>()).ToArray();
        var frames = new List<double>();
        float end = Time.time + seconds;
        await Awaitable.NextFrameAsync();
        while (Time.time < end)
        {
            await Awaitable.NextFrameAsync();
            frames.Add(Time.unscaledDeltaTime * 1000.0);
            for (int i = 0; i < recorders.Length; i++) samples[i].Add(recorders[i].LastValue / 1e6);
        }
        foreach (var recorder in recorders) recorder.Dispose();

        var rows = new List<string>();
        for (int i = 0; i < names.Count; i++)
            rows.Add($"\"{names[i]}\":{Stats(samples[i])}");
        foreach (string group in new[] { "Ship.Decision", "Squadron.Periodic", "Squadron.Launch" })
        {
            var summed = new List<double>();
            for (int frame = 0; frame < frames.Count; frame++)
            {
                double total = 0;
                for (int i = 0; i < names.Count; i++)
                    if (names[i].StartsWith($"EnemyAI.{group}.")) total += samples[i][frame];
                summed.Add(total);
            }
            rows.Add($"\"EnemyAI.{group}.All\":{Stats(summed)}");
        }
        string json = "{\"status\":\"completed\"," +
            $"\"aiCount\":{aiCount},\"frames\":{frames.Count},\"seconds\":{seconds}," +
            $"\"ships\":{Object.FindObjectsByType<EmpireAtWar.Ship.Ship>().Length}," +
            $"\"starvationRuns\":{scheduler.StarvationRunCount - starvedStart}," +
            $"\"executedUnits\":{scheduler.ExecutedUnitCount - unitsStart}," +
            $"\"budgetMs\":{scheduler.FrameBudgetMs}," +
            $"\"frameMs\":{Stats(frames)}," + string.Join(",", rows) + "}";
        File.WriteAllText(path, json);
    }

    private static string Stats(List<double> values)
    {
        if (values.Count == 0) return "null";
        var sorted = values.OrderBy(v => v).ToList();
        double p95 = sorted[Mathf.Clamp((int)System.Math.Ceiling(0.95 * sorted.Count) - 1, 0, sorted.Count - 1)];
        double p99 = sorted[Mathf.Clamp((int)System.Math.Ceiling(0.99 * sorted.Count) - 1, 0, sorted.Count - 1)];
        return System.FormattableString.Invariant(
            $"{{\"mean\":{values.Average():F4},\"p95\":{p95:F4},\"p99\":{p99:F4},\"max\":{sorted[^1]:F4},\"nonzero\":{values.Count(v => v > 0)}}}");
    }
}
