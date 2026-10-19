using System.Linq;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.VContainer;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public static class SimulationSmoke
{
    public static SimulationSetup Setup(bool replace = false)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<FactionCatalog>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:FactionCatalog").Single()));
        var a = replace ? FactionType.Empire : FactionType.Republic;
        var b = FactionType.Separatist;
        var shipA = catalog.Get(a).Ships.OrderBy(pair => pair.Value.Price).First().Key;
        var shipB = catalog.Get(b).Ships.OrderBy(pair => pair.Value.Price).First().Key;
        return new SimulationSetup(a, b, new[] { new SimulationFleetEntry(UnitTypeId.Ship(shipA), 2),
                new SimulationFleetEntry(UnitTypeId.Squadron(catalog.Get(a).Squadrons.Keys.First()), 1) },
            new[] { new SimulationFleetEntry(UnitTypeId.Ship(shipB), 2),
                new SimulationFleetEntry(UnitTypeId.Squadron(catalog.Get(b).Squadrons.Keys.First()), 1) }, 500, 30, false);
    }
    public static string Start()
    {
        Object.FindAnyObjectByType<ProjectScope>().Container.Resolve<IGameCommand>().StartBattle(SimulationPreset.Create(Setup()).Build());
        return "Simulation launch requested.";
    }
    public static object Status()
    {
        var scope = Object.FindAnyObjectByType<SkirmishScope>();
        var model = scope.Container.Resolve<SimulationModel>();
        return new { Scene = SceneManager.GetActiveScene().GetHashCode(), State = scope.Container.Resolve<SkirmishStateModel>().State.ToString(),
            model.Elapsed, model.IsSpawning, model.CountA, model.CountB,
            Result = model.Result == null ? "none" : model.Result.Outcome.ToString(),
            Entities = scope.Container.Resolve<IEntityLocator>().Entities.Select(entity => new { entity.Id, Player = entity.Owner.Index }).ToArray() };
    }
    public static string Restart()
    {
        var scope = Object.FindAnyObjectByType<SkirmishScope>();
        _ = scope.Container.Resolve<SimulationDirector>().RestartAsync(scope.Container.Resolve<SimulationModel>().Setup);
        return "Restart requested.";
    }
    public static string Replace()
    {
        _ = Object.FindAnyObjectByType<SkirmishScope>().Container.Resolve<SimulationDirector>().RestartAsync(Setup(true));
        return "Replacement requested.";
    }
    public static string Pause()
    {
        Object.FindAnyObjectByType<SkirmishScope>().Container.Resolve<SkirmishOrchestrator>().TogglePause();
        return "Toggled pause.";
    }
    public static string Speed()
    {
        Object.FindAnyObjectByType<SkirmishScope>().Container.Resolve<SkirmishOrchestrator>().ToggleSpeedUp();
        return "Toggled speed.";
    }

    public static string Verify() { _ = VerifyAsync(); return "Simulation lifecycle verification started."; }

    private static async Awaitable VerifyAsync()
    {
        const string OUTPUT = "output/BattleBuilderPhase1/simulation-runtime.txt";
        try
        {
            var scope = Object.FindAnyObjectByType<SkirmishScope>();
            var model = scope.Container.Resolve<SimulationModel>();
            var director = scope.Container.Resolve<SimulationDirector>();
            var state = scope.Container.Resolve<SkirmishStateModel>();
            var flow = scope.Container.Resolve<SkirmishOrchestrator>();
            var entities = scope.Container.Resolve<IEntityLocator>();
            int scene = SceneManager.GetActiveScene().GetHashCode();
            await director.RestartAsync(Setup());
            while (model.IsSpawning) await Awaitable.NextFrameAsync();
            if (model.CountA != 3 || model.CountB != 3) throw new System.Exception("Expected ships and squadrons on both sides.");
            flow.TogglePause();
            float elapsed = model.Elapsed;
            for (int i = 0; i < 20; i++) await Awaitable.NextFrameAsync();
            if (model.Elapsed != elapsed) throw new System.Exception("Paused timer advanced.");
            flow.TogglePause(); flow.ToggleSpeedUp();
            if (state.Speed != GameSpeed.Fast || Time.timeScale != 4) throw new System.Exception("Fast speed was not applied.");
            while (model.Elapsed < 10) await Awaitable.NextFrameAsync();
            var oldIds = entities.Entities.Select(entity => entity.Id).ToArray();
            await director.RestartAsync(Setup(true));
            while (model.IsSpawning) await Awaitable.NextFrameAsync();
            if (SceneManager.GetActiveScene().GetHashCode() != scene || entities.Entities.Any(entity => oldIds.Contains(entity.Id)))
                throw new System.Exception("Replacement reloaded the scene or retained old entities.");
            scope.Container.Resolve<ISimulationHudPresenter>().New();
            if (state.State != BattleState.Paused) throw new System.Exception("New setup did not pause simulation.");
            scope.Container.Resolve<EmpireAtWar.Entities.MainMenu.Simulation.ISimulationSetupPresenter>().Close();
            if (state.State != BattleState.Running) throw new System.Exception("Cancel setup did not resume simulation.");
            flow.ToggleSpeedUp();
            while (model.Result == null) await Awaitable.NextFrameAsync();
            await Awaitable.NextFrameAsync();
            if (state.State != BattleState.Ended) throw new System.Exception("Result did not end simulation.");
            await director.RestartAsync(model.Setup);
            while (model.IsSpawning) await Awaitable.NextFrameAsync();
            if (state.State != BattleState.Running || model.Result != null) throw new System.Exception("Ended restart failed.");
            System.IO.File.WriteAllText(OUTPUT, $"PASS: ships and squadrons, pause, 4x speed, combat replacement, New/cancel, result and Ended restart; same scene {scene}, fresh entity IDs.");
        }
        catch (System.Exception error) { System.IO.File.WriteAllText(OUTPUT, error.ToString()); Debug.LogException(error); }
    }
}
