using System;
using EmpireAtWar.Components.Ui.Tooltip;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Entities.TutorialUi;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Entities.Faction.Ui
{
    public class FactionUi : BaseUi
    {
        private IPlayerFactionModelObserver _model;
        private IFactionResearchModelObserver _research;
        private IFactionPresenter _presenter;
        private IUnitRequestFactory _unitRequestFactory;

        [SerializeField] private FactionUnitUi factionUnitPrefab;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private UnityEngine.UI.GridLayoutGroup layoutGrid;
        [SerializeField] private UnityEngine.UI.ContentSizeFitter layoutFitter;
        [Tooltip("The panel's BuildPanel anchor; it also marks the level-up, research and first ship buttons.")]
        [SerializeField] private TutorialAnchor tutorialAnchor;
        private readonly List<FactionUnitUi> _factionUnitsUi =
            new List<FactionUnitUi>();
        private readonly Dictionary<ResearchType, FactionUnitUi> _researchUnitsUi =
            new Dictionary<ResearchType, FactionUnitUi>();
        private FactionUnitUi _levelFactionUnitUi;
        private FactionUnitUi _secondLevelUnitUi;
        private SecondStationLevelModel _secondStation;
        private StationLevelData _stationLevels;
        private FactionRoster _roster;
        private Transform _unitParent;
        private UiContentLayout _contentLayout;

        private bool _isInitialized;
        private bool _isRouteActive = true;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void SetContentLayout(UiContentLayout layout)
        {
            _unitParent = transform;
            _contentLayout = layout;
            layout.AttachContent((RectTransform)transform, layoutGrid, layoutFitter);
        }

        public void Initialize()
        {
            if (_model == null || _research == null || _presenter == null || _roster == null ||
                _unitRequestFactory == null || _secondStation == null || _stationLevels == null)
            {
                throw new InvalidOperationException(
                    "Faction UI dependencies must be set before initialization.");
            }

            if (_unitParent == null)
            {
                throw new InvalidOperationException(
                    "Faction UI route parent must be set before initialization.");
            }

            if (_isInitialized)
            {
                return;
            }

            foreach (var data in _roster.Ships)
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            foreach (var data in _roster.Squadrons)
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            CreateLevelUnit();

            foreach (var data in _roster.MiningFacilities)
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            foreach (var data in _roster.DefendPlatforms)
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            AddUi(_unitRequestFactory.ConstructSecondStationRequest(_stationLevels.GetSecondStationFactionData(_roster.FactionType)));
            CreateSecondLevelUnit();

            foreach (var data in _roster.SuperWeapons)
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            foreach (ResearchType researchType in _research.ResearchTypes)
            {
                CreateResearchUnit(researchType);
            }

            _model.OnSelectionTypeChanged += HandleSelectionChanged;
            _model.OnLevelUpgraded += UpdateUnits;
            _secondStation.OnLevelUpgraded += UpdateSecondLevelUnit;
            _secondStation.BuiltChanged += HandleSecondStationBuiltChanged;
            _research.OnResearchCompleted += UpdateResearchUnit;
            _isInitialized = true;
            RefreshUnitVisibility(_model.SelectionType);
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _model.OnSelectionTypeChanged -= HandleSelectionChanged;
            _model.OnLevelUpgraded -= UpdateUnits;
            _secondStation.OnLevelUpgraded -= UpdateSecondLevelUnit;
            _secondStation.BuiltChanged -= HandleSecondStationBuiltChanged;
            _research.OnResearchCompleted -= UpdateResearchUnit;
            if (_contentLayout != null) _contentLayout.DetachContent();
            _isInitialized = false;
        }

        public void SetModel(IPlayerFactionModelObserver model)
        {
            _model = model;
        }

        public void SetResearch(IFactionResearchModelObserver research)
        {
            _research = research;
        }

        public void AssignPresenter(IFactionPresenter presenter)
        {
            if (_presenter != null)
                throw new InvalidOperationException("FactionUi presenter can only be assigned once.");
            _presenter = presenter;
        }

        public void SetData(FactionRoster roster)
        {
            _roster = roster;
        }

        public void SetUnitRequestFactory(IUnitRequestFactory unitRequestFactory)
        {
            _unitRequestFactory = unitRequestFactory;
        }

        public void SetSecondStation(SecondStationLevelModel secondStation, StationLevelData stationLevels)
        {
            _secondStation = secondStation;
            _stationLevels = stationLevels;
        }

        public void BuyUnit(UnitRequest unitRequest)
        {
            _presenter.TryPurchaseUnit(unitRequest);
        }

        public void RefreshAvailability(float credits, bool battleEnded)
        {
            if (!_isRouteActive || _model.SelectionType != SelectionType.Base) return;
            var queues = _model.GetProductionQueueSnapshots();
            foreach (FactionUnitUi unit in _factionUnitsUi)
            {
                bool queued = false;
                foreach (var queue in queues)
                    if (queue.UnitRequest.Id == unit.RequestId) queued = true;
                bool unlocked = _presenter.IsUnitAvailable(unit.FactionData);
                bool atLimit = _presenter.IsUnitLimitReached(unit.Request);
                unit.SetActive(unlocked && !(atLimit && HidesAtLimit(unit)));
                unit.RenderAvailability(unlocked,
                    credits >= unit.FactionData.Price, queued, battleEnded, atLimit);
            }
        }

        public override void Show()
        {
            _isRouteActive = true;
            base.Show();

            if (_model != null)
            {
                RefreshUnitVisibility(_model.SelectionType);
            }
        }

        public override void Hide()
        {
            _isRouteActive = false;

            if (_model != null)
            {
                RefreshUnitVisibility(_model.SelectionType);
            }

            base.Hide();
        }

        private FactionUnitUi AddUi(UnitRequest unitRequest)
        {
            FactionUnitUi unitUi = Instantiate(factionUnitPrefab, _unitParent);
            unitUi.SetData(unitRequest.FactionData, this, unitRequest);
            tooltipHover.Register(unitUi.TooltipTrigger);
            _factionUnitsUi.Add(unitUi);
            return unitUi;
        }

        private void CreateResearchUnit(ResearchType researchType)
        {
            if (_research.TryGetNextTier(researchType, out ResearchTierData tier))
            {
                _researchUnitsUi[researchType] = AddUi(
                    _unitRequestFactory.ConstructUnitRequest(tier.FactionData, researchType,
                        _research.GetCompletedTiers(researchType) + 1));
            }
        }

        private void UpdateResearchUnit(ResearchType researchType)
        {
            FactionUnitUi unitUi = _researchUnitsUi[researchType];
            _researchUnitsUi.Remove(researchType);
            _factionUnitsUi.Remove(unitUi);
            unitUi.Destroy();

            CreateResearchUnit(researchType);
            RefreshUnitVisibility(_model.SelectionType);
        }

        private void CreateLevelUnit()
        {
            FactionData levelData = _model.GetCurrentLevelFactionData();
            if (levelData == null)
            {
                return;
            }

            _levelFactionUnitUi = Instantiate(factionUnitPrefab, _unitParent);
            LevelUnitRequest levelUnitRequest =
                _unitRequestFactory.ConstructUnitRequest(
                    levelData,
                    _model.CurrentLevel);
            _levelFactionUnitUi.SetData(levelData, this, levelUnitRequest);
            tooltipHover.Register(_levelFactionUnitUi.TooltipTrigger);
            _factionUnitsUi.Add(_levelFactionUnitUi);
        }

        private void UpdateUnits(int level)
        {
            int unitIndex = _levelFactionUnitUi != null
                ? _factionUnitsUi.IndexOf(_levelFactionUnitUi)
                : _factionUnitsUi.Count;
            int siblingIndex = _levelFactionUnitUi != null
                ? _levelFactionUnitUi.transform.GetSiblingIndex()
                : _unitParent.childCount;

            if (_levelFactionUnitUi != null)
            {
                _factionUnitsUi.Remove(_levelFactionUnitUi);
                _levelFactionUnitUi.SetActive(false);
                _levelFactionUnitUi.Destroy();
                _levelFactionUnitUi = null;
            }

            CreateLevelUnit();
            if (_levelFactionUnitUi != null)
            {
                _factionUnitsUi.Remove(_levelFactionUnitUi);
                _factionUnitsUi.Insert(unitIndex, _levelFactionUnitUi);
                _levelFactionUnitUi.transform.SetSiblingIndex(siblingIndex);
            }

            RefreshUnitVisibility(_model.SelectionType);
        }

        // The second station's level-up card exists only while that station stands.
        private void CreateSecondLevelUnit()
        {
            if (!_secondStation.IsBuilt) return;
            FactionData levelData = _stationLevels.GetSecondStationLevelFactionData(_secondStation.CurrentLevel);
            if (levelData == null) return;
            _secondLevelUnitUi = AddUi(
                _unitRequestFactory.ConstructSecondStationLevelRequest(levelData, _secondStation.CurrentLevel));
        }

        private void UpdateSecondLevelUnit(int level)
        {
            if (_secondLevelUnitUi != null)
            {
                _factionUnitsUi.Remove(_secondLevelUnitUi);
                _secondLevelUnitUi.SetActive(false);
                _secondLevelUnitUi.Destroy();
                _secondLevelUnitUi = null;
            }

            CreateSecondLevelUnit();
            RefreshUnitVisibility(_model.SelectionType);
        }

        private void HandleSecondStationBuiltChanged(bool isBuilt) => UpdateSecondLevelUnit(_secondStation.CurrentLevel);

        // Heroes, structure population caps, station upgrades and research tiers hide at their limit.
        // Ship and squadron queue limits only block the card: it becomes buyable again as the queue drains.
        private static bool HidesAtLimit(FactionUnitUi unit) =>
            unit.FactionData.IsHero || unit.Request is MiningFacilityUnitRequest or DefendPlatformUnitRequest
                or LevelUnitRequest or ResearchUnitRequest or SecondStationUnitRequest or SecondStationLevelUnitRequest;

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            RefreshUnitVisibility(selectionType);
        }

        private void RefreshUnitVisibility(SelectionType selectionType)
        {
            bool isSelectionVisible =
                _isRouteActive && selectionType == SelectionType.Base;
            int siblingIndex = 0;

            FactionUnitUi firstShip = null, firstScout = null, firstResearch = null, level = null;
            FactionUnitUi economyResearch = null, fleetResearch = null, newShip = null, miningFacility = null;
            bool isNewShipCarrier = false;
            foreach (FactionUnitUi unitUi in _factionUnitsUi
                         .OrderBy(unit => _presenter.GetUnitSortOrder(unit.Request))
                         .ThenByDescending(unit => _presenter.IsUnitAvailable(unit.FactionData)))
            {
                unitUi.transform.SetSiblingIndex(siblingIndex++);
                bool isShown = isSelectionVisible && _presenter.IsUnitAvailable(unitUi.FactionData) &&
                               !(HidesAtLimit(unitUi) && _presenter.IsUnitLimitReached(unitUi.Request));
                unitUi.SetActive(isShown);
                if (!isShown) continue;
                switch (unitUi.Request)
                {
                    case ShipUnitRequest:
                        if (firstShip == null) firstShip = unitUi;
                        if (_presenter.IsUnlockedAtCurrentLevel(unitUi.FactionData) &&
                            IsBetterNewShip(unitUi, newShip, isNewShipCarrier, out bool isCarrier))
                        {
                            newShip = unitUi;
                            isNewShipCarrier = isCarrier;
                        }
                        // Sort order 0 is fighters, bombers and interceptors: the scouts.
                        if (firstScout == null && _presenter.GetUnitSortOrder(unitUi.Request) == 0) firstScout = unitUi;
                        break;
                    case SquadronUnitRequest:
                        if (firstScout == null) firstScout = unitUi;
                        break;
                    case ResearchUnitRequest research:
                        if (firstResearch == null) firstResearch = unitUi;
                        bool isEconomy = ResearchGroup.IsEconomy(research.Key);
                        if (isEconomy && economyResearch == null) economyResearch = unitUi;
                        if (!isEconomy && fleetResearch == null) fleetResearch = unitUi;
                        break;
                    case LevelUnitRequest:
                        level = unitUi;
                        break;
                    case MiningFacilityUnitRequest:
                        if (miningFacility == null) miningFacility = unitUi;
                        break;
                }
            }
            MarkTutorialButton(TutorialAnchorId.BuildShipButton, firstShip);
            MarkTutorialButton(TutorialAnchorId.ScoutShipButton, firstScout);
            MarkTutorialButton(TutorialAnchorId.ResearchButton, firstResearch);
            MarkTutorialButton(TutorialAnchorId.LevelUpButton, level);
            MarkTutorialButton(TutorialAnchorId.EconomyResearchButton, economyResearch);
            MarkTutorialButton(TutorialAnchorId.FleetResearchButton, fleetResearch);
            MarkTutorialButton(TutorialAnchorId.NewShipButton, newShip);
            MarkTutorialButton(TutorialAnchorId.MiningFacilityButton, miningFacility);
        }

        // The tutorial's new unit: a carrier first, otherwise the most expensive (strongest) new ship.
        private bool IsBetterNewShip(FactionUnitUi candidate, FactionUnitUi best, bool isBestCarrier, out bool isCarrier)
        {
            isCarrier = _presenter.IsCarrier(candidate.Request);
            if (best == null || isCarrier != isBestCarrier) return best == null || isCarrier;
            return candidate.FactionData.Price > best.FactionData.Price;
        }

        // Tutorial hints point at one deterministic button: the first shown in panel order.
        private void MarkTutorialButton(TutorialAnchorId id, FactionUnitUi unitUi)
        {
            if (unitUi == null) tutorialAnchor.Unmark(id);
            else tutorialAnchor.Mark(id, (RectTransform)unitUi.transform);
        }
    }
}
