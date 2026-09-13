using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyProductionStrategy
    {
        private const float MINIMUM_PRODUCTION_INTERVAL = 1f;

        private readonly EnemyFactionData _factionModel;
        private readonly IEnemyPurchaseProcessor _purchaseProcessor;
        private readonly IUnitRequestFactory _requestFactory;
        private readonly IEconomyModelObserver _economyModel;
        private readonly IEnemyAiStateProvider _stateProvider;
        private readonly IGameModelObserver _gameModel;
        private readonly EnemyProductionDecisionModel _decisionModel;
        private readonly EnemyUnitLimitModel _unitLimitModel;
        private readonly ReinforcementData _reinforcementData;
        private readonly IEnemyStructurePlacementService _structurePlacementService;

        private float _decisionTimer;
        private int _observedReleaseVersion;

        public EnemyProductionStrategy(
            EnemyFactionData factionModel,
            IEnemyPurchaseProcessor purchaseProcessor,
            IUnitRequestFactory requestFactory,
            IEconomyModelObserver economyModel,
            IEnemyAiStateProvider stateProvider,
            IGameModelObserver gameModel,
            EnemyProductionDecisionModel decisionModel,
            EnemyUnitLimitModel unitLimitModel,
            ReinforcementData reinforcementData,
            IEnemyStructurePlacementService structurePlacementService)
        {
            _factionModel = factionModel ?? throw new ArgumentNullException(nameof(factionModel));
            _purchaseProcessor = purchaseProcessor ?? throw new ArgumentNullException(nameof(purchaseProcessor));
            _requestFactory = requestFactory ?? throw new ArgumentNullException(nameof(requestFactory));
            _economyModel = economyModel ?? throw new ArgumentNullException(nameof(economyModel));
            _stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
            _gameModel = gameModel ?? throw new ArgumentNullException(nameof(gameModel));
            _decisionModel = decisionModel ?? throw new ArgumentNullException(nameof(decisionModel));
            _unitLimitModel = unitLimitModel ?? throw new ArgumentNullException(nameof(unitLimitModel));
            _reinforcementData = reinforcementData ??
                throw new ArgumentNullException(nameof(reinforcementData));
            _structurePlacementService = structurePlacementService ??
                throw new ArgumentNullException(nameof(structurePlacementService));
        }

        public void Start()
        {
            _decisionTimer = 0f;
            _observedReleaseVersion = _unitLimitModel.ReleaseVersion;
        }

        public void Tick(float deltaTime)
        {
            if (_observedReleaseVersion != _unitLimitModel.ReleaseVersion)
            {
                _observedReleaseVersion = _unitLimitModel.ReleaseVersion;
                _decisionTimer = 0f;
            }

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
            int shipCount = CountReservedShips();
            int miningFacilityCount = CountReservedMiningFacilities();
            int defensePlatformCount = CountReservedDefensePlatforms();
            bool canPlaceStructure = _structurePlacementService.TryGetPosition(out _);
            bool hasMiningSelection = TrySelectMiningFacility(
                out KeyValuePair<MiningFacilityType, FactionData> mining);
            bool hasMiningOption = canPlaceStructure && hasMiningSelection;
            bool canBuildMining = hasMiningOption && IsAffordable(mining.Value);
            bool hasDefenseSelection = TrySelectDefense(
                out KeyValuePair<DefendPlatformType, FactionData> defense);
            bool hasDefenseOption = canPlaceStructure && hasDefenseSelection;
            bool canBuildDefense = hasDefenseOption && IsAffordable(defense.Value);

            bool isUltraHard = _gameModel.EnemyDifficulty == EnemyAiDifficulty.UltraHard;
            bool hasShipOption = TrySelectShip(
                shipCount,
                out KeyValuePair<ShipType, FactionData> ship);
            bool canBuildShip = hasShipOption && IsAffordable(ship.Value);
            FactionData levelData = _factionModel.GetCurrentLevelFactionData();
            bool hasLevelUpOption = levelData != null;
            bool canLevelUp = levelData != null && levelData.Price <= _economyModel.Money;
            int miningFacilityTarget = isUltraHard
                ? profile.MinimumMiningFacilities + _stateProvider.ActiveShipCount / 2
                : profile.MinimumMiningFacilities;
            int defensePlatformTarget = isUltraHard
                ? 1 + _stateProvider.ActiveShipCount / 3
                : 1;

            EnemyProductionCategory category = _decisionModel.Evaluate(
                new EnemyProductionSnapshot(
                    _stateProvider.CurrentState,
                    _gameModel.EnemyDifficulty,
                    miningFacilityCount,
                    shipCount,
                    _unitLimitModel.ShipOrdersCount,
                    defensePlatformCount,
                    _factionModel.CurrentLevel,
                    miningFacilityTarget,
                    defensePlatformTarget,
                    hasMiningOption,
                    hasShipOption,
                    canBuildShip,
                    canBuildMining,
                    hasDefenseOption,
                    canBuildDefense,
                    hasLevelUpOption,
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
                return;
            }

            Debug.Log(
                $"[EnemyAI:Production] State={_stateProvider.CurrentState}, " +
                $"FactionLevel={_factionModel.CurrentLevel}, Category={category}, " +
                $"Unit={request.Id}, Mining={miningFacilityCount}/{miningFacilityTarget}, " +
                $"Defense={defensePlatformCount}/{defensePlatformTarget}, " +
                $"Cost={request.FactionData.Price}, " +
                $"Money={_economyModel.Money}");
            _purchaseProcessor.Handle(request);
        }

        private int CountReservedShips()
        {
            int count = 0;
            foreach (KeyValuePair<ShipType, FactionData> option
                     in _factionModel.ShipFactionData)
            {
                count += _unitLimitModel.GetReservedCount<ShipUnitRequest>(
                    option.Key.ToString());
            }

            return count;
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

        private int CountReservedDefensePlatforms()
        {
            int count = 0;
            foreach (KeyValuePair<DefendPlatformType, FactionData> option
                     in _factionModel.DefendPlatforms)
            {
                count += _unitLimitModel.GetReservedCount<DefendPlatformUnitRequest>(
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
                if (option.Value.AvailableLevel > _factionModel.CurrentLevel ||
                    !CanReserve<MiningFacilityUnitRequest>(
                        option.Key.ToString(),
                        option.Value))
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

        private bool TrySelectShip(
            int shipCount,
            out KeyValuePair<ShipType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            float bestPriority = float.MaxValue;
            foreach (KeyValuePair<ShipType, FactionData> option
                     in _factionModel.ShipFactionData)
            {
                if (!IsAffordableAndAvailable(option.Value) ||
                    !CanReserve<ShipUnitRequest>(
                        option.Key.ToString(),
                        option.Value))
                {
                    continue;
                }

                int reservedCount = _unitLimitModel.GetReservedCount<ShipUnitRequest>(
                    option.Key.ToString());
                float priority = _decisionModel.CalculateShipPriority(
                    _stateProvider.CurrentState,
                    shipCount,
                    reservedCount,
                    option.Value.BuildTime,
                    option.Value.UnitCapacity);
                if (!found || priority < bestPriority ||
                    priority == bestPriority && option.Value.Price < selected.Value.Price)
                {
                    selected = option;
                    bestPriority = priority;
                    found = true;
                }
            }

            return found;
        }

        private bool TrySelectDefense(
            out KeyValuePair<DefendPlatformType, FactionData> selected)
        {
            selected = default;
            bool found = false;
            foreach (KeyValuePair<DefendPlatformType, FactionData> option
                     in _factionModel.DefendPlatforms)
            {
                if (!IsAvailable(option.Value) ||
                    !CanReserve<DefendPlatformUnitRequest>(
                        option.Key.ToString(),
                        option.Value))
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

        private bool CanReserve<TRequest>(string requestId, FactionData data)
        {
            return _unitLimitModel.CanReserve<TRequest>(
                requestId,
                data.MaxCount,
                data.UnitCapacity,
                _reinforcementData.MaxUnitCapacity);
        }

        private bool IsAffordableAndAvailable(FactionData data)
        {
            return IsAvailable(data) && IsAffordable(data);
        }

        private bool IsAvailable(FactionData data)
        {
            return data.AvailableLevel <= _factionModel.CurrentLevel;
        }

        private bool IsAffordable(FactionData data)
        {
            return data.Price <= _economyModel.Money;
        }
    }
}
