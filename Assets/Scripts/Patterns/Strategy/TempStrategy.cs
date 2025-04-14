using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Models.Factions;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.Patterns.Strategy
{
    public class TempStrategy: BaseUnitSpawnStrategy
    {
        private const float DECISION_DELAY = 30f;
        private readonly ITimer _decisionTimer;
        private readonly ITimer _levelUpgradeTimer;
        private bool _isMaxLevelReached;
        public TempStrategy(EnemyFactionModel factionModel, IEnemyPurchaseProcessor enemyPurchaseProcessor, IUnitRequestFactory unitRequestFactory) 
            : base(factionModel, enemyPurchaseProcessor, unitRequestFactory)
        {
            _decisionTimer = TimerFactory.ConstructTimer(DECISION_DELAY);
            _levelUpgradeTimer = TimerFactory.ConstructTimer(FactionModel.GetCurrentLevelFactionData().BuildTime);
        }

        public override void Start()
        {
           // BuildDefends();
        }
        
        public override void Stop() {}

        private void TryLevelUp()
        {
            if(_isMaxLevelReached) return;
            
            if (_levelUpgradeTimer.IsComplete)
            {
                FactionData currentLevelData = FactionModel.GetCurrentLevelFactionData();
                if (currentLevelData == null)
                {
                    _isMaxLevelReached = true;
                    return;
                }
                BuildUnit(FactionModel.CurrentLevel, FactionModel.GetCurrentLevelFactionData());
                Debug.Log($"Upgrading level: {FactionModel.CurrentLevel}");
                _levelUpgradeTimer.StartTimer();
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
            if (_decisionTimer.IsComplete)
            {
                TryLevelUp();
                BuildShip();
                BuildMining();
                BuildDefends();
                _decisionTimer.StartTimer();
            }
        }
    }
}