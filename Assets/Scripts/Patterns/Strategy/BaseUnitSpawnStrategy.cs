using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;

namespace EmpireAtWar.Patterns.Strategy
{
    public abstract class BaseUnitSpawnStrategy : IUnitSpawnStrategy
    {
        protected IUnitRequestFactory UnitRequestFactory { get; }
        protected EnemyFactionModel FactionModel { get; }
        protected IEnemyPurchaseMediator EnemyPurchaseMediator { get; }

        protected BaseUnitSpawnStrategy(EnemyFactionModel factionModel, IEnemyPurchaseMediator enemyPurchaseMediator,
            IUnitRequestFactory unitRequestFactory)
        {
            FactionModel = factionModel;
            EnemyPurchaseMediator = enemyPurchaseMediator;
            UnitRequestFactory = unitRequestFactory;
        }

        public abstract void Start();
        public abstract void Update();
        public abstract void Stop();

        protected virtual void BuildUnit(UnitRequest unitRequest)
        {
            EnemyPurchaseMediator.Handle(unitRequest);
        }

        protected virtual void BuildUnit(KeyValuePair<ShipType, FactionData> keyValuePair)
        {
            BuildUnit(UnitRequestFactory.ConstructUnitRequest(keyValuePair.Value, keyValuePair.Key));
        }
        
        protected virtual void BuildUnit(KeyValuePair<MiningFacilityType, FactionData> keyValuePair)
        {
            BuildUnit(UnitRequestFactory.ConstructUnitRequest(keyValuePair.Value, keyValuePair.Key));
        }
        
        protected virtual void BuildUnit(KeyValuePair<DefendPlatformType, FactionData> keyValuePair)
        {
            BuildUnit(UnitRequestFactory.ConstructUnitRequest(keyValuePair.Value, keyValuePair.Key));
        }
        
        protected virtual void BuildUnit(int level, FactionData factionData)
        {
            BuildUnit(UnitRequestFactory.ConstructUnitRequest(factionData, level));
        }
    }

    public class LevelUpStrategy : BaseUnitSpawnStrategy
    {
        public LevelUpStrategy(EnemyFactionModel factionModel, IEnemyPurchaseMediator enemyPurchaseMediator,
            IUnitRequestFactory unitRequestFactory)
            : base(factionModel, enemyPurchaseMediator, unitRequestFactory)
        {
        }

        public override void Start()
        {
            BuildUnit(FactionModel.ShipFactionData.FirstOrDefault());
        }

        public override void Update()
        {
        }

        public override void Stop()
        {
        }
    }
}