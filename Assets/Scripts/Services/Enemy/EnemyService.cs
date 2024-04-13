using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Controllers;
using EmpireAtWar.Patterns.Strategy;
using LightWeightFramework.Components.Service;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public interface IEnemyService : IService
    {
    }

    public class EnemyService : Service, IInitializable, IEnemyService
    {
        private readonly IEnemyShipSpawner enemyShipSpawner;

        public EnemyService(IEnemyShipSpawner enemyShipSpawner)
        {
            this.enemyShipSpawner = enemyShipSpawner;
        }
        public void Initialize()
        {
            enemyShipSpawner.SetStrategy(UnitSpawnStrategyType.LevelUpFast);
        }
    }
}