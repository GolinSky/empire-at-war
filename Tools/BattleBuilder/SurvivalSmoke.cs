using System.Linq;
using EmpireAtWar.Commands.Game;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.VContainer;
using UnityEngine;
using VContainer;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using System;
using System.IO;
using System.Reflection;
using Object = UnityEngine.Object;

public static class SurvivalSmoke
{
    public static string Start()
    {
        var project = Object.FindFirstObjectByType<ProjectScope>();
        project.Container.Resolve<IGameCommand>().StartBattle(SurvivalPreset.Create(new SurvivalSettings(
            FactionType.Republic, FactionType.Republic, FactionType.Separatist, MapSize.Small,
            EmpireAtWar.Entities.EnemyFaction.Models.EnemyAiDifficulty.Medium, true)).Build());
        return "Survival launch requested.";
    }
    public static object Status()
    {
        var battle = Object.FindFirstObjectByType<SkirmishScope>();
        var model = battle.Container.Resolve<SurvivalModel>();
        return new { State = battle.Container.Resolve<SkirmishStateModel>().State.ToString(),
            Phase = model.Phase.ToString(), model.WaveIndex, model.PreparationTimeLeft,
            Units = battle.Container.Resolve<IEntityLocator>().Entities.GroupBy(entity => entity.Owner.Index)
                .Select(group => new { Player = group.Key, Count = group.Count(entity => !entity.HealthModel.IsDestroyed) }).ToArray() };
    }
    public static string StartWave()
    {
        Object.FindFirstObjectByType<SkirmishScope>().Container.Resolve<SurvivalModel>().StartWaveNow();
        return "Started wave.";
    }
    public static string ClearEnemies()
    {
        var scope = Object.FindAnyObjectByType<SkirmishScope>();
        int owner = scope.Battle.Players.Single(player => player.Role == PlayerRole.WaveAttacker).Slot.Id.Index;
        var enemies = scope.Container.Resolve<IEntityLocator>().Entities.Where(entity => entity.Owner.Index == owner).ToArray();
        foreach (var entity in enemies)
        {
            if (entity.TryGetFacade<IEntityDestroyFacade>(out var destroy)) destroy.Destroy();
            else
            {
                var health = (EmpireAtWar.Components.Squadrons.Health.SquadronHealthComponent)entity.HealthModel;
                foreach (var member in health.HardPointModels)
                {
                    health.ApplyDamage(1000000000, EmpireAtWar.Components.AttackComponent.DamageType.PointDefense, member.Id);
                    health.ApplyDamage(1000000000, EmpireAtWar.Components.AttackComponent.DamageType.PointDefense, member.Id);
                }
            }
        }
        return $"Destroyed {enemies.Length} wave entities.";
    }
    public static string DestroyStation()
    {
        var scope = Object.FindAnyObjectByType<SkirmishScope>();
        scope.Container.Resolve<EmpireAtWar.Services.Stations.IStationRegistry>().TryGetLivingStation(new EmpireAtWar.Models.Players.PlayerId(0), out var station);
        ((Component)station.HealthModel).GetComponentInParent<EntityLifetimeScope>().Container.Resolve<EmpireAtWar.Models.Health.HealthModel>().Kill();
        return "Destroyed defending station.";
    }
    public static string VerifyAllWaves() { _ = VerifyAsync(); return "Five-wave runtime check started."; }
    private static async Awaitable VerifyAsync()
    {
        const string OUTPUT = "output/BattleBuilderPhase1/survival-runtime.txt";
        try
        {
            var scope = UnityEngine.Object.FindAnyObjectByType<SkirmishScope>();
            var model = scope.Container.Resolve<SurvivalModel>();
            var director = scope.Container.Resolve<EmpireAtWar.Services.Battle.SurvivalWaveDirector>();
            var pending = (System.Collections.ICollection)director.GetType().GetField("_pending", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
            for (int wave = 1; wave <= 5; wave++)
            {
                model.StartWaveNow();
                float deadline = Time.realtimeSinceStartup + 30;
                await Awaitable.NextFrameAsync();
                while (pending.Count > 0 && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
                if (pending.Count > 0) throw new Exception($"Wave {wave} could not finish spawning.");
                while (model.Phase == SurvivalPhase.WaveActive && Time.realtimeSinceStartup < deadline)
                { ClearEnemies(); await Awaitable.NextFrameAsync(); }
                if (model.Phase != (wave == 5 ? SurvivalPhase.Won : SurvivalPhase.Preparation))
                    throw new Exception($"Wave {wave}: unexpected phase {model.Phase}.");
            }
            await Awaitable.NextFrameAsync();
            File.WriteAllText(OUTPUT, "PASS: Five real authored waves spawned and cleared; Survival won with defending station alive.");
        }
        catch (Exception error) { File.WriteAllText(OUTPUT, error.ToString()); Debug.LogException(error); }
    }
}
