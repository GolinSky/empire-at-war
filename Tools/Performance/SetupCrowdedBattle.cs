using EmpireAtWar.Commands.Game;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.VContainer;
using UnityEngine;
using VContainer;
using System.Linq;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.Units;

public static class SetupCrowdedBattle
{
    public static object Launch()
    {
        var players = new[]
        {
            new PlayerSlot(new PlayerId(0), new TeamId(0), FactionType.Republic,
                PlayerController.Human, EnemyAiDifficulty.Medium, 0),
            new PlayerSlot(new PlayerId(1), new TeamId(1), FactionType.Separatist,
                PlayerController.Ai, EnemyAiDifficulty.UltraHard, 1)
        };
        var builder = SkirmishPreset.Create(players, PlanetType.Coruscant, MapSize.Small,
            BattleVictoryCondition.DestroyOpponentBase, 1000f, true, 1875278133);
        Object.FindAnyObjectByType<ProjectScope>().Container.Resolve<IGameCommand>().StartBattle(builder.Build());
        return new { seed = 1875278133, mode = "Full", size = "Small", enemy = "UltraHard" };
    }

    public static object Status()
    {
        var scope = Object.FindAnyObjectByType<SkirmishScope>();
        return new
        {
            scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
            state = scope.Container.Resolve<EmpireAtWar.Controllers.Game.SkirmishStateModel>().State.ToString(),
            time = Time.time,
            ships = Object.FindObjectsByType<EmpireAtWar.Ship.Ship>().Length,
            squadrons = Object.FindObjectsByType<EmpireAtWar.Entities.Squadrons.Squadron>().Count(s => s.gameObject.activeInHierarchy),
            weapons = Object.FindObjectsByType<EmpireAtWar.Components.Weapon.WeaponComponent>().Length,
            local = Object.FindAnyObjectByType<LocalPlayerScope>() != null,
            enemy = Object.FindAnyObjectByType<AiPlayerScope>() != null
        };
    }

    public static object Engage()
    {
        var scope = Object.FindAnyObjectByType<SkirmishScope>().Container;
        var entities = scope.Resolve<IEntityLocator>();
        var local = new PlayerId(0);
        var enemyBase = entities.Entities.First(e => e.Owner == new PlayerId(1) && e.IsPlayerBase());
        var ships = entities.Entities.Where(e => e.Owner == local &&
            e.TryGetFacade(out IUnitTypeFacade type) && type.UnitTypeId.IsShip).ToList();
        var destination = enemyBase.GetFacade<IEntityTransformFacade>().Transform.position;
        scope.Resolve<EmpireAtWar.Services.UnitOrders.IUnitOrderService>().IssueAttackMove(ships, destination);
        return new
        {
            ordered = ships.Count, destination,
            localLevel = Object.FindAnyObjectByType<LocalPlayerScope>().Container.Resolve<PlayerFactionModel>().CurrentLevel,
            enemyLevel = Object.FindAnyObjectByType<AiPlayerScope>().Container.Resolve<EmpireAtWar.Entities.EnemyFaction.Models.EnemyFactionModel>().CurrentLevel,
            status = Status()
        };
    }

    public static async System.Threading.Tasks.Task<object> Prepare()
    {
        var local = Object.FindAnyObjectByType<LocalPlayerScope>().Container;
        var faction = local.Resolve<EmpireAtWar.Services.Factions.IFactionService>();
        var cheats = local.Resolve<EmpireAtWar.Services.Cheats.ICheatService>();
        var world = local.Resolve<EmpireAtWar.Services.Cheats.CheatWorldService>();
        var enemy = new PlayerId(1);
        for (int level = 1; level < 5; level++)
        {
            faction.ForceUpgradeStation();
            world.ForceUpgradeEnemyStation(enemy);
            await Awaitable.NextFrameAsync();
        }
        cheats.AddMoney(1000000f);
        world.AddEnemyMoney(enemy, 1000000f);
        var roster = local.Resolve<FactionRoster>();
        int population = 0;
        var spawned = new System.Collections.Generic.List<string>();
        for (int pass = 0; pass < 8 && population < 120; pass++)
        {
            foreach (var ship in roster.Ships.OrderBy(pair => pair.Value.UnitCapacity))
            {
                if (ship.Value.AvailableLevel > 5 || ship.Value.IsHero && pass > 0 ||
                    population + ship.Value.UnitCapacity > 120) continue;
                if (!cheats.ForceSpawnShipAtDefaultZone(new EmpireAtWar.Controllers.Factions.ShipUnitRequest(ship.Value, ship.Key)))
                    continue;
                population += ship.Value.UnitCapacity;
                spawned.Add(ship.Key.ToString());
                await Awaitable.NextFrameAsync();
            }
        }
        return new { population, spawned, status = Status() };
    }

    public static async System.Threading.Tasks.Task<object> StressFleets()
    {
        var battle = Object.FindAnyObjectByType<SkirmishScope>().Container;
        var ai = Object.FindAnyObjectByType<AiPlayerScope>().Container;
        var owner = new PlayerId(1);
        var roster = ai.Resolve<FactionRoster>();
        var controller = ai.Resolve<EmpireAtWar.Entities.EnemyFaction.Controllers.EnemyFactionController>();
        var factory = ai.Resolve<EmpireAtWar.Ship.ShipFactory>();
        var spawnPoints = ai.Resolve<EmpireAtWar.Services.ShipSpawning.IShipSpawnPoints>();
        int shipsAdded = 0;
        for (int pass = 0; pass < 8 && Object.FindObjectsByType<EmpireAtWar.Ship.Ship>().Length < 80; pass++)
        {
            foreach (var pair in roster.Ships)
            {
                if (pair.Value.IsHero && pass > 0) continue;
                if (!spawnPoints.TryGetDefaultZoneSpawnPosition(owner, pair.Key, out Vector3 position)) continue;
                var ship = factory.Create(owner, pair.Key, position);
                controller.AdoptShip(ship, new EmpireAtWar.Controllers.Factions.ShipUnitRequest(pair.Value, pair.Key));
                shipsAdded++;
                await Awaitable.NextFrameAsync();
                if (Object.FindObjectsByType<EmpireAtWar.Ship.Ship>().Length >= 80) break;
            }
        }
        var spawner = ai.Resolve<EmpireAtWar.Services.Squadrons.ISquadronSpawner>();
        var entities = battle.Resolve<IEntityLocator>();
        Vector3 home = entities.Entities.First(e => e.Owner == owner && e.IsPlayerBase())
            .GetFacade<IEntityTransformFacade>().Transform.position;
        int squadronsAdded = 0;
        var types = roster.Squadrons.ToArray();
        while (Object.FindObjectsByType<EmpireAtWar.Entities.Squadrons.Squadron>().Length < 320 && squadronsAdded < 320)
        {
            var pair = types[squadronsAdded % types.Length];
            Vector2 offset = Random.insideUnitCircle * 500f;
            var squadron = spawner.Spawn(owner, pair.Key, home + new Vector3(offset.x, 0, offset.y), Quaternion.identity);
            controller.AdoptSquadron(squadron, new EmpireAtWar.Controllers.Factions.SquadronUnitRequest(pair.Value, pair.Key));
            squadronsAdded++;
            await Awaitable.NextFrameAsync();
        }
        battle.Resolve<EmpireAtWar.Services.Camera.ICameraService>().MoveTo(home);
        return new { shipsAdded, squadronsAdded, status = Status() };
    }

    public static object ArmPrewarm()
    {
        var prewarmer = Object.FindAnyObjectByType<SkirmishScope>().Container
            .Resolve<EmpireAtWar.ViewComponents.Weapon.ShotPoolPrewarmer>();
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var caches = (System.Collections.IDictionary)prewarmer.GetType().GetField("_caches", flags).GetValue(prewarmer);
        var enqueue = prewarmer.GetType().GetMethod("Enqueue", flags);
        var rows = new System.Collections.Generic.List<object>();
        foreach (System.Collections.DictionaryEntry entry in caches)
        {
            var all = entry.Value.GetType().GetField("_all", flags).GetValue(entry.Value);
            int count = (int)all.GetType().GetProperty("Count").GetValue(all);
            enqueue.Invoke(prewarmer, new object[] { entry.Value, count + 12 });
            rows.Add(new { prefab = ((Object)entry.Key).name, count, target = count + 12 });
        }
        return rows;
    }

    public static object ArmPrewarmDelayed()
    {
        ArmAfterDelay();
        return new { status = "scheduled", delaySeconds = 3 };
    }

    private static async void ArmAfterDelay()
    {
        await Awaitable.WaitForSecondsAsync(3f);
        UnityEngine.Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(ArmPrewarm()));
    }
}
