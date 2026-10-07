using System;
using EmpireAtWar.Models.Players;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;
using EmpireAtWar.Entities.EnemyFaction.Models.Intel;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyProductionStrategy
    {
        private const float MINIMUM_PRODUCTION_INTERVAL = 1f;

        private const int SHIPS_PER_SQUADRON = 2;

        private readonly IEnemyPurchaseProcessor _purchaseProcessor;
        private readonly IUnitRequestFactory _requestFactory;
        private readonly IEconomyModelObserver _economyModel;
        private readonly IEnemyAiStateProvider _stateProvider;
        private readonly IEnemyStructurePlacementService _enemyStructurePlacementService;
        private readonly IStationRegistry _stationRegistry;

        private readonly EnemyFactionModel _factionModel;
        private readonly IFactionResearchModelObserver _research;
        private readonly PlayerSlot _owner;
        private readonly EnemyProductionDecisionModel _decisionModel;
        private readonly EnemyUnitLimitModel _unitLimitModel;
        private readonly ReinforcementData _reinforcementData;
        private readonly IPlayerRoster _playerRoster;
        private readonly ForceCompositionBuilder _forceBuilder;
        private readonly UnitCombatProfileCatalog _profileCatalog;
        private readonly EnemyCounterProductionModel _counterModel;
        private readonly HostileIntelModel _intel;

        private readonly ForceComposition _ownForce = new ForceComposition();
        private readonly ForceComposition _hostileForce = new ForceComposition();
        private readonly Dictionary<UnitTypeId, int> _liveUnitCounts = new Dictionary<UnitTypeId, int>();
        private readonly List<ProductionCandidate> _candidates = new List<ProductionCandidate>();
        private readonly List<UnitTypeId> _candidateIds = new List<UnitTypeId>();
        private readonly List<FactionData> _candidateData = new List<FactionData>();

        private float _decisionTimer;

        private int _observedReleaseVersion;

        public EnemyProductionStrategy(
            IEnemyPurchaseProcessor purchaseProcessor,
            IUnitRequestFactory requestFactory,
            IEconomyModelObserver economyModel,
            IEnemyAiStateProvider stateProvider,
            IEnemyStructurePlacementService enemyStructurePlacementService,
            IStationRegistry stationRegistry,
            EnemyFactionModel factionModel,
            IFactionResearchModelObserver research,
            EnemyProductionDecisionModel decisionModel,
            EnemyUnitLimitModel unitLimitModel,
            ReinforcementData reinforcementData,
            IPlayerRoster playerRoster,
            ForceCompositionBuilder forceBuilder,
            UnitCombatProfileCatalog profileCatalog,
            EnemyCounterProductionModel counterModel,
            TeamIntelRegistry intelRegistry,
            PlayerSlot owner)
        {
            _owner = owner;
            _playerRoster = playerRoster;
            _forceBuilder = forceBuilder;
            _profileCatalog = profileCatalog;
            _counterModel = counterModel;
            _intel = intelRegistry.Get(owner.Team);
            _factionModel = factionModel;
            _research = research;
            _purchaseProcessor = purchaseProcessor;
            _requestFactory = requestFactory;
            _economyModel = economyModel;
            _stateProvider = stateProvider;
            _decisionModel = decisionModel;
            _unitLimitModel = unitLimitModel;
            _reinforcementData = reinforcementData;
            _enemyStructurePlacementService = enemyStructurePlacementService;
            _stationRegistry = stationRegistry;
        }

        public void Start()
        {
            _decisionTimer = 0f;
            _observedReleaseVersion = _unitLimitModel.ReleaseVersion;
        }

        public void Tick(float deltaTime)
        {
            if (!_stationRegistry.IsStationOperational(_owner.Id))
            {
                return;
            }

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
                _owner.Difficulty);
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
            bool canPlaceStructure = _enemyStructurePlacementService.TryGetPosition(out _);
            bool hasMiningSelection = TrySelectMiningFacility(
                out KeyValuePair<MiningFacilityType, FactionData> mining);
            bool hasMiningOption = canPlaceStructure && hasMiningSelection;
            bool canBuildMining = hasMiningOption && IsAffordable(mining.Value);
            bool hasDefenseSelection = TrySelectDefense(
                out KeyValuePair<DefendPlatformType, FactionData> defense);
            bool hasDefenseOption = canPlaceStructure && hasDefenseSelection;
            bool canBuildDefense = hasDefenseOption && IsAffordable(defense.Value);

            KeyValuePair<SquadronType, FactionData> squadron = default;
            KeyValuePair<ShipType, FactionData> ship = default;
            bool isUltraHard = _owner.Difficulty == EnemyAiDifficulty.UltraHard;
            // A counter to the hostile composition replaces the size-based pick whenever one improves the matchup.
            // Squadrons support ships, at most one per SHIPS_PER_SQUADRON ships. Cheap squadrons always win on
            // gain per credit and never grow the fleet, so without the cap they would crowd out ships, mining
            // and station upgrades.
            // With no ship type unlocked (Empire at level 1) squadrons are the only units, so the cap is lifted.
            bool canOrderShips = CanOrderAnyShip();
            bool hasSquadronRoom = HasSquadronRoom(shipCount);
            bool hasCounter = TrySelectCounterUnit(
                canOrderShips && (_decisionModel.NeedsMinimumFleet(shipCount) || !hasSquadronRoom),
                hasSquadronRoom,
                out UnitTypeId counterId,
                out FactionData counterData);
            bool hasShip = !hasCounter && TrySelectShip(shipCount, out ship);
            bool hasSquadronFallback = !hasCounter && !hasShip && !canOrderShips &&
                                       TrySelectSquadron(shipCount, true, out squadron);
            bool hasShipOption = hasCounter || hasShip || hasSquadronFallback;
            FactionData shipChoice = hasCounter ? counterData : hasShip ? ship.Value : squadron.Value;
            bool canBuildShip = hasShipOption && IsAffordable(shipChoice);
            FactionData levelData = _factionModel.GetCurrentLevelFactionData();
            bool hasLevelUpOption = levelData != null;
            bool canLevelUp = levelData != null && levelData.Price <= _economyModel.Money;
            bool canResearchIncome = TrySelectResearch(true, out ResearchType incomeResearch,
                                         out ResearchTierData incomeTier) &&
                                     IsAffordable(incomeTier.FactionData);
            bool canResearchCombat = TrySelectResearch(false, out ResearchType combatResearch,
                                         out ResearchTierData combatTier) &&
                                     IsAffordable(combatTier.FactionData);
            int miningFacilityTarget = isUltraHard
                ? profile.MinimumMiningFacilities + _stateProvider.ActiveShipCount / 2
                : profile.MinimumMiningFacilities;
            int defensePlatformTarget = isUltraHard
                ? 1 + _stateProvider.ActiveShipCount / 3
                : 1;

            if (TryBuyScout(shipCount,
                    hasMiningSelection && miningFacilityCount < miningFacilityTarget,
                    canPlaceStructure))
            {
                return;
            }

            EnemyProductionCategory category = _decisionModel.Evaluate(
                new EnemyProductionSnapshot(
                    _stateProvider.CurrentState,
                    _owner.Difficulty,
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
                    canLevelUp,
                    canResearchIncome,
                    canResearchCombat,
                    canOrderShips));

            bool buildSquadron = !hasCounter &&
                                 category == EnemyProductionCategory.Ship &&
                                 (hasSquadronFallback ||
                                  TrySelectSquadron(shipCount, false, out squadron) &&
                                  IsAffordable(squadron.Value));
            UnitRequest request = category switch
            {
                EnemyProductionCategory.Ship when hasCounter && counterId.IsShip =>
                    _requestFactory.ConstructUnitRequest(counterData, counterId.ShipType),
                EnemyProductionCategory.Ship when hasCounter =>
                    _requestFactory.ConstructUnitRequest(counterData, counterId.SquadronType),
                EnemyProductionCategory.Ship when buildSquadron =>
                    _requestFactory.ConstructUnitRequest(squadron.Value, squadron.Key),
                EnemyProductionCategory.Ship =>
                    _requestFactory.ConstructUnitRequest(ship.Value, ship.Key),
                EnemyProductionCategory.Mining =>
                    _requestFactory.ConstructUnitRequest(mining.Value, mining.Key),
                EnemyProductionCategory.Defense =>
                    _requestFactory.ConstructUnitRequest(defense.Value, defense.Key),
                EnemyProductionCategory.Level =>
                    _requestFactory.ConstructUnitRequest(levelData, _factionModel.CurrentLevel),
                EnemyProductionCategory.Research when canResearchIncome =>
                    CreateResearchRequest(incomeResearch, incomeTier),
                EnemyProductionCategory.Research =>
                    CreateResearchRequest(combatResearch, combatTier),
                EnemyProductionCategory.None => null,
                _ => throw new ArgumentOutOfRangeException(nameof(category))
            };

            if (request == null)
            {
                return;
            }

            Debug.Log(
                $"[EnemyAI:Production] Player={_owner.Id}:{_owner.Faction}, State={_stateProvider.CurrentState}, " +
                $"FactionLevel={_factionModel.CurrentLevel}, Category={category}, Counter={hasCounter}, " +
                $"Unit={request.Id}, Mining={miningFacilityCount}/{miningFacilityTarget}, " +
                $"Defense={defensePlatformCount}/{defensePlatformTarget}, " +
                $"Cost={request.FactionData.Price}, " +
                $"Money={_economyModel.Money}");
            _purchaseProcessor.Purchase(request);
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

        /// <summary>
        /// Rates every buildable ship and squadron against all hostile units; units already bought but not yet
        /// in play count toward the own force so the same counter is not bought twice.
        /// </summary>
        private bool TrySelectCounterUnit(
            bool shipsOnly,
            bool hasSquadronRoom,
            out UnitTypeId selectedId,
            out FactionData selectedData)
        {
            selectedId = default;
            selectedData = null;
            PlayerId self = _owner.Id;
            // Counters answer what the team has seen of the enemy, not the whole hidden map.
            _forceBuilder.BuildKnown(_hostileForce, _intel, Time.time, sighting => true);
            if (_hostileForce.IsEmpty)
            {
                return false;
            }

            _forceBuilder.Build(_ownForce, entity => entity.Owner == self, _liveUnitCounts);
            _candidates.Clear();
            _candidateIds.Clear();
            _candidateData.Clear();
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                AddCounterOption<ShipUnitRequest>(UnitTypeId.Ship(option.Key), option.Key.ToString(), option.Value,
                    true, hasSquadronRoom);
            }

            foreach (KeyValuePair<SquadronType, FactionData> option in _factionModel.SquadronFactionData)
            {
                AddCounterOption<SquadronUnitRequest>(UnitTypeId.Squadron(option.Key), option.Key.ToString(),
                    option.Value, !shipsOnly, hasSquadronRoom);
            }

            if (!_counterModel.TrySelect(_ownForce, _hostileForce, _candidates, out int index))
            {
                return false;
            }

            selectedId = _candidateIds[index];
            selectedData = _candidateData[index];
            return true;
        }

        /// <summary>
        /// Counts pending units of this type into the own force; adds it as a candidate when
        /// <paramref name="isSelectable"/>. A carrier's hangar is valued only while squadrons have room, because
        /// hangar squadrons share the squadron cap.
        /// </summary>
        private void AddCounterOption<TRequest>(
            UnitTypeId unitTypeId,
            string requestId,
            FactionData data,
            bool isSelectable,
            bool hasSquadronRoom)
        {
            _liveUnitCounts.TryGetValue(unitTypeId, out int liveCount);
            int reservedCount = _unitLimitModel.GetReservedCount<TRequest>(requestId);
            int pendingCount = reservedCount - liveCount;
            for (int i = 0; i < pendingCount; i++)
            {
                _ownForce.AddNew(_profileCatalog.Get(unitTypeId), true);
            }

            if (!isSelectable || !IsAvailable(data) || !CanReserve<TRequest>(requestId, data))
            {
                return;
            }

            _candidates.Add(new ProductionCandidate(_profileCatalog.Get(unitTypeId), data.Price,
                Math.Max(reservedCount, liveCount), hasSquadronRoom));
            _candidateIds.Add(unitTypeId);
            _candidateData.Add(data);
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
                // Affordability is checked after selection so the AI saves for higher-tier ships.
                if (!IsAvailable(option.Value) ||
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
                    option.Value.UnitCapacity,
                    option.Value.MaxCount);
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

        /// <summary>True while bought squadrons number fewer than one per <see cref="SHIPS_PER_SQUADRON"/> ships.</summary>
        /// <remarks>Carrier hangars count toward the cap so carriers are no way around it.</remarks>
        private bool HasSquadronRoom(int shipCount)
        {
            int cap = shipCount / SHIPS_PER_SQUADRON;
            return cap > 0 && CountReservedSquadrons() + CountHangarSquadrons() < cap;
        }

        /// <summary>Squadrons the AI's bought ships field at once from their hangars.</summary>
        private int CountHangarSquadrons()
        {
            int count = 0;
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                int reserved = _unitLimitModel.GetReservedCount<ShipUnitRequest>(option.Key.ToString());
                if (reserved > 0)
                {
                    count += reserved * _profileCatalog.Get(UnitTypeId.Ship(option.Key)).Hangar.Count;
                }
            }

            return count;
        }

        private int CountReservedSquadrons()
        {
            int squadronCount = 0;
            foreach (KeyValuePair<SquadronType, FactionData> option
                     in _factionModel.SquadronFactionData)
            {
                squadronCount += _unitLimitModel.GetReservedCount<SquadronUnitRequest>(
                    option.Key.ToString());
            }

            return squadronCount;
        }

        /// <summary>True when any ship type is unlocked at the current level and within its limits.</summary>
        private bool CanOrderAnyShip()
        {
            foreach (KeyValuePair<ShipType, FactionData> option in _factionModel.ShipFactionData)
            {
                if (IsAvailable(option.Value) && CanReserve<ShipUnitRequest>(option.Key.ToString(), option.Value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Mining has no visible place to go: one fighter is bought to scout the fog for build space.
        /// The squadron commander flies a squadron to the scout target.
        /// </summary>
        private bool TryBuyScout(int shipCount, bool needsMining, bool canPlaceStructure)
        {
            if (!needsMining ||
                canPlaceStructure ||
                CountReservedSquadrons() > 0 ||
                !_enemyStructurePlacementService.TryGetScoutTarget(out _) ||
                !TrySelectSquadron(shipCount, true, out KeyValuePair<SquadronType, FactionData> scout) ||
                !IsAffordable(scout.Value))
            {
                return false;
            }

            UnitRequest request = _requestFactory.ConstructUnitRequest(scout.Value, scout.Key);
            Debug.Log(
                $"[EnemyAI:Production] Player={_owner.Id}:{_owner.Faction}, Category=Scout, Unit={request.Id}, " +
                $"Cost={request.FactionData.Price}, Money={_economyModel.Money}");
            _purchaseProcessor.Purchase(request);
            return true;
        }

        /// <summary>Picks the cheapest available squadron while <see cref="HasSquadronRoom"/>, or regardless of the ship ratio.</summary>
        private bool TrySelectSquadron(
            int shipCount,
            bool ignoreShipRatio,
            out KeyValuePair<SquadronType, FactionData> selected)
        {
            selected = default;
            if (!ignoreShipRatio && !HasSquadronRoom(shipCount))
            {
                return false;
            }

            bool found = false;
            foreach (KeyValuePair<SquadronType, FactionData> option
                     in _factionModel.SquadronFactionData)
            {
                if (!IsAvailable(option.Value) ||
                    !CanReserve<SquadronUnitRequest>(
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

        /// <summary>
        /// Picks the cheapest unlocked next tier, not already being researched, among the lines that raise income
        /// (<paramref name="raisesIncome"/>) or among the combat lines.
        /// </summary>
        private bool TrySelectResearch(
            bool raisesIncome,
            out ResearchType selectedType,
            out ResearchTierData selectedTier)
        {
            selectedType = default;
            selectedTier = null;
            foreach (ResearchType researchType in _research.ResearchTypes)
            {
                if (!_research.TryGetNextTier(researchType, out ResearchTierData tier) ||
                    RaisesIncome(tier) != raisesIncome ||
                    !IsAvailable(tier.FactionData) ||
                    !CanReserve<ResearchUnitRequest>(researchType.ToString(), tier.FactionData))
                {
                    continue;
                }

                if (selectedTier == null || tier.FactionData.Price < selectedTier.FactionData.Price)
                {
                    selectedType = researchType;
                    selectedTier = tier;
                }
            }

            return selectedTier != null;
        }

        private static bool RaisesIncome(ResearchTierData tier)
        {
            foreach (ResearchEffect effect in tier.Effects)
            {
                if (effect.Stat == ResearchStat.Income)
                {
                    return true;
                }
            }

            return false;
        }

        private UnitRequest CreateResearchRequest(ResearchType researchType, ResearchTierData tier)
        {
            return _requestFactory.ConstructUnitRequest(
                tier.FactionData,
                researchType,
                _research.GetCompletedTiers(researchType) + 1);
        }

        private bool CanReserve<TRequest>(string requestId, FactionData data)
        {
            return _unitLimitModel.CanReserve<TRequest>(
                requestId,
                data.MaxCount,
                data.UnitCapacity,
                _reinforcementData.MaxUnitCapacity);
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
