using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Patterns.Strategy
{
    public class TempStrategy: BaseUnitSpawnStrategy
    {
        private const float DecisionDelay = 30f;
        private readonly ITimer decisionTimer;
        private readonly ITimer levelUpgradeTimer;
        private bool isMaxLevelReached;
        public TempStrategy(EnemyFactionModel factionModel, IEnemyPurchaseMediator enemyPurchaseMediator, IUnitRequestFactory unitRequestFactory) 
            : base(factionModel, enemyPurchaseMediator, unitRequestFactory)
        {
            decisionTimer = TimerFactory.ConstructTimer(DecisionDelay);
            levelUpgradeTimer = TimerFactory.ConstructTimer(FactionModel.GetCurrentLevelFactionData().BuildTime);
        }

        public override void Start()
        {
           // BuildDefends();
        }
        
        public override void Stop() {}

        private void TryLevelUp()
        {
            if(isMaxLevelReached) return;
            
            if (levelUpgradeTimer.IsComplete)
            {
                FactionData currentLevelData = FactionModel.GetCurrentLevelFactionData();
                if (currentLevelData == null)
                {
                    isMaxLevelReached = true;
                    return;
                }
                BuildUnit(FactionModel.CurrentLevel, FactionModel.GetCurrentLevelFactionData());
                Debug.Log($"Upgrading level: {FactionModel.CurrentLevel}");
                levelUpgradeTimer.StartTimer();
            }
        }

        private void BuildShip()
        {
            foreach (var keyValuePair in FactionModel.ShipFactionData)
            {
                if (FactionModel.CurrentLevel >= keyValuePair.Value.AvailableLevel)
                {
                    BuildUnit(keyValuePair);
                    Debug.Log($"Start building ship: {keyValuePair.Key}");
                }
            }
        }

        private void BuildMining()
        {
            foreach (var keyValuePair in FactionModel.MiningFactions)
            {
                if (FactionModel.CurrentLevel >= keyValuePair.Value.AvailableLevel)
                {
                    BuildUnit(keyValuePair);
                    Debug.Log($"Start building mining: {keyValuePair.Key}");
                }
            }
        }

        private void BuildDefends()
        {
            foreach (var keyValuePair in FactionModel.DefendPlatforms)
            {
                if (FactionModel.CurrentLevel >= keyValuePair.Value.AvailableLevel)
                {
                    BuildUnit(keyValuePair);
                    Debug.Log($"Start building defense: {keyValuePair.Key}");
                }
            }
        }

        public override void Update()
        {
            //return;
            if (decisionTimer.IsComplete)
            {
                TryLevelUp();
                BuildShip();
                BuildMining();
                BuildDefends();
                decisionTimer.StartTimer();
            }
        }
    }
}