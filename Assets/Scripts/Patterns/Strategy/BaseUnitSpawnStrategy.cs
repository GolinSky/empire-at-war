using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;

namespace EmpireAtWar.Patterns.Strategy
{
    public abstract class BaseUnitSpawnStrategy : IUnitSpawnStrategy
    {
        protected IUnitRequestFactory UnitRequestFactory { get; }
        protected EnemyFactionModel FactionModel { get; }
        protected  IPurchaseMediator EnemyPurchaseMediator { get; }
        
        protected BaseUnitSpawnStrategy(EnemyFactionModel factionModel, IPurchaseMediator enemyPurchaseMediator, IUnitRequestFactory unitRequestFactory)
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
        
        protected virtual void BuildShipUnit(KeyValuePair<ShipType,FactionData> keyValuePair)
        {
            BuildUnit(UnitRequestFactory.ConstructUnitRequest(keyValuePair.Value, keyValuePair.Key));
        }

    }

    public class LevelUpStrategy:BaseUnitSpawnStrategy
    {
        public LevelUpStrategy(EnemyFactionModel factionModel, IPurchaseMediator enemyPurchaseMediator, IUnitRequestFactory unitRequestFactory) 
            : base(factionModel, enemyPurchaseMediator, unitRequestFactory)
        {
        }

        public override void Start()
        {
            BuildShipUnit(FactionModel.ShipFactionData.FirstOrDefault());
        }

        public override void Update()
        {
        }

        public override void Stop()
        {
        }
    }
}