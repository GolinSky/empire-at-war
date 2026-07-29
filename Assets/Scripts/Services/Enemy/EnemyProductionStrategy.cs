using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyProductionStrategy
    {
        private const float MINIMUM_PRODUCTION_INTERVAL = 1f;

        private readonly EnemyFactionModel _factionModel;
        private readonly IEnemyPurchaseProcessor _purchaseProcessor;
        private readonly IUnitRequestFactory _requestFactory;
        private readonly IEconomyModelObserver _economyModel;
        private readonly IEnemyAiStateProvider _stateProvider;
        private readonly IGameModelObserver _gameModel;
        private readonly EnemyProductionDecisionModel _decisionModel;
        private readonly EnemyUnitLimitModel _unitLimitModel;

        private float _decisionTimer;

        public EnemyProductionStrategy(
            EnemyFactionModel factionModel,
            IEnemyPurchaseProcessor purchaseProcessor,
            IUnitRequestFactory requestFactory,
            IEconomyModelObserver economyModel,
            IEnemyAiStateProvider stateProvider,
            IGameModelObserver gameModel,
            EnemyProductionDecisionModel decisionModel,
            EnemyUnitLimitModel unitLimitModel)
        {
            _factionModel = factionModel ?? throw new ArgumentNullException(nameof(factionModel));
            _purchaseProcessor = purchaseProcessor ?? throw new ArgumentNullException(nameof(purchaseProcessor));
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            _economyModel = economyModel ?? throw new ArgumentNullException(nameof(economyModel));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _gameModel = gameModel ?? throw new ArgumentNullException(nameof(gameModel));
            _decisionModel = decisionModel ?? throw new ArgumentNullException(nameof(decisionModel));
            _unitLimitModel = unitLimitModel ?? throw new ArgumentNullException(nameof(unitLimitModel));
        }

        public void Start()
        {
            _decisionTimer = 0f;
        }

        public void Tick(float deltaTime)
        {
            _decisionTimer -= deltaTime;
            if (_decisionTimer > 0f)
            {
                return;
            }

            EnemyAiDifficultyProfile profile = EnemyAiDifficultyProfile.Get(
                _gameModel.EnemyDifficulty);
            _decisionTimer = Mathf.Max(
                MINIMUM_PRODUCTION_INTERVAL,
                profile.DecisionInterval * 2f);
            EvaluateProduction(profile);
        }

        private void EvaluateProduction(EnemyAiDifficultyProfile profile)
        {
            bool canBuildShip = TrySelectShip(out KeyValuePair<ShipType, FactionData> ship);
            int miningFacilityCount = CountReservedMiningFacilities();
            bool hasMiningOption = TrySelectMiningFacility(
                out KeyValuePair<MiningFacilityType, FactionData> mining);
            bool canBuildMining = hasMiningOption &&
                mining.Value.Price <= _economyModel.Money;
            bool canBuildDefense = TrySelectCheapestAffordable(
                _factionModel.DefendPlatforms,
                out KeyValuePair<DefendPlatformType, FactionData> defense);
            FactionData levelData = _factionModel.GetCurrentLevelFactionData();
            bool canLevelUp = levelData != null && levelData.Price <= _economyModel.Money;

            EnemyProductionCategory category = _decisionModel.Evaluate(
                new EnemyProductionSnapshot(
                    _stateProvider.CurrentState,
                    _gameModel.EnemyDifficulty,
                    miningFacilityCount,
                    hasMiningOption,
                    canBuildShip,
                    canBuildMining,
                    canBuildDefense,
                    canLevelUp));

            UnitRequest request = category switch
            {
                EnemyProductionCategory.Ship =>
                    _requestFactory.ConstructUnitRequest(ship.Value, ship.Key),
                EnemyProductionCategory.Mining =>
                    _requestFactory.ConstructUnitRequest(mining.Value, mining.Key),
                EnemyProductionCategory.Defense =>
                    _requestFactory.ConstructUnitRequest(defense.Value, defense.Key),
                EnemyProductionCategory.Level =>
                    _requestFactory.ConstructUnitRequest(levelData, _factionModel.CurrentLevel),
                EnemyProductionCategory.None => null,
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };

            if (request == null)
            {
                if (miningFacilityCount < profile.MinimumMiningFacilities &&
                    hasMiningOption)
                {
                    Debug.Log(
                        $"[EnemyAI:Production] SavingForMining=true, " +
                        $"Mining={miningFacilityCount}/{profile.MinimumMiningFacilities}, " +
                        $"Cost={mining.Value.Price}, Money={_economyModel.Money}");
                }

                return;
            }

            Debug.Log(
                $"[EnemyAI:Production] State={_stateProvider.CurrentState}, " +
                $"Category={category}, Mining={miningFacilityCount}/" +
                $"{profile.MinimumMiningFacilities}, Cost={request.FactionData.Price}, " +
                $"Money={_economyModel.Money}");
            _purchaseProcessor.Handle(request);
        }

        private int CountReservedMiningFacilities()
        {
            int count = 0;
            foreach (KeyValuePair<MiningFacilityType, FactionData> option
                     in _factionModel.MiningFactions)
            {
                count += _unitLimitModel.GetReservedCount<MiningFacilityUnitRequest>(
                    option.Key.ToString());
            }

            return count;
        }

        private bool TrySelectMiningFacility(
            out KeyValuePair<MiningFacilityType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            foreach (KeyValuePair<MiningFacilityType, FactionData> option
                     in _factionModel.MiningFactions)
            {
                int reservedCount =
                    _unitLimitModel.GetReservedCount<MiningFacilityUnitRequest>(
                        option.Key.ToString());
                if (option.Value.AvailableLevel > _factionModel.CurrentLevel ||
                    reservedCount >= option.Value.MaxCount)
                {
                    continue;
                }

                if (!found || option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    found = true;
                }
            }

            return found;
        }

        private bool TrySelectShip(out KeyValuePair<ShipType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            bool preferExpensive = _gameModel.EnemyDifficulty >= EnemyAiDifficulty.Hard;
            foreach (KeyValuePair<ShipType, FactionData> option
                     in _factionModel.ShipFactionData)
            {
                if (!IsAffordableAndAvailable(option.Value))
                {
                    continue;
                }

                if (!found ||
                    preferExpensive && option.Value.Price > selected.Value.Price ||
                    !preferExpensive && option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    found = true;
                }
            }

            return found;
        }

        private bool TrySelectCheapestAffordable<TKey>(
            IReadOnlyDictionary<TKey, FactionData> options,
            out KeyValuePair<TKey, FactionData> selected)
        {
            if (!TrySelectCheapestAvailable(options, out selected))
            {
                return false;
            }

            return selected.Value.Price <= _economyModel.Money;
        }

        private bool TrySelectCheapestAvailable<TKey>(
            IReadOnlyDictionary<TKey, FactionData> options,
            out KeyValuePair<TKey, FactionData> selected)
        {
            selected = default;
            bool found = false;
            foreach (KeyValuePair<TKey, FactionData> option in options)
            {
                if (option.Value.AvailableLevel > _factionModel.CurrentLevel)
                {
                    continue;
                }

                if (!found || option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    found = true;
                }
            }

            return found;
        }

        private bool IsAffordableAndAvailable(FactionData data)
        {
            return data.AvailableLevel <= _factionModel.CurrentLevel &&
                data.Price <= _economyModel.Money;
        }
    }
}
