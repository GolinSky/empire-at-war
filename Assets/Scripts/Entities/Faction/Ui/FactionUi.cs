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
        [SerializeField] private FactionUnitUi stationUnitPrefab;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private RectTransform unitsRow;
        [SerializeField] private UnityEngine.UI.ContentSizeFitter layoutFitter;
        [Tooltip("The panel's BuildPanel anchor also marks individual production cards.")]
        [SerializeField] private TutorialAnchor tutorialAnchor;
        private readonly List<FactionUnitUi> _factionUnitsUi =
            new List<FactionUnitUi>();
        private readonly Dictionary<ResearchType, FactionUnitUi> _researchUnitsUi =
            new Dictionary<ResearchType, FactionUnitUi>();
        private readonly Dictionary<ResearchType, ResearchBuffIcon> _researchBuffs = new();
        private (FactionUnitUi Station, FactionUnitUi Research) _levelCards;
        private (FactionUnitUi Station, FactionUnitUi Research) _secondLevelCards;
        private SecondStationLevelModel _secondStation;
        private StationLevelData _stationLevels;
        private FactionRoster _roster;
        private UiContentLayout _contentLayout;
        private FactionProductionMenus _productionMenus;

        private bool _isInitialized;
        private bool _isRouteActive = true;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void SetProductionMenus(FactionProductionMenus productionMenus) =>
            _productionMenus = productionMenus;

        public void SetContentLayout(UiContentLayout layout)
        {
            _contentLayout = layout;
            layout.AttachContent((RectTransform)transform, layoutFitter);
        }

        public void Initialize()
        {
            if (_model == null || _research == null || _presenter == null || _roster == null ||
                _unitRequestFactory == null || _secondStation == null || _stationLevels == null ||
                _productionMenus == null)
            {
                throw new InvalidOperationException(
                    "Faction UI dependencies must be set before initialization.");
            }

            if (_contentLayout == null)
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

            foreach (ResearchType researchType in _research.ResearchTypes)
                CreateResearchUnit(researchType);

            tooltipHover.Register(_productionMenus.ResearchTooltip);
            foreach (ResearchType researchType in _research.ResearchTypes.OrderByDescending(ResearchGroup.IsEconomy))
            {
                ResearchBuffIcon buff = _productionMenus.CreateResearchBuff();
                _researchBuffs.Add(researchType, buff);
                RefreshResearchBuff(researchType);
                tooltipHover.Register(buff.TooltipTrigger);
                buff.gameObject.SetActive(true);
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
            _productionMenus.Close();
            tooltipHover.Unregister(_productionMenus.ResearchTooltip);
            foreach (ResearchBuffIcon buff in _researchBuffs.Values)
            {
                tooltipHover.Unregister(buff.TooltipTrigger);
                buff.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(buff.gameObject);
                else DestroyImmediate(buff.gameObject);
            }
            _researchBuffs.Clear();
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

        public bool BuyUnit(UnitRequest unitRequest)
        {
            return _presenter.TryPurchaseUnit(unitRequest);
        }

        public void RefreshAvailability(float credits, bool battleEnded)
        {
            if (!_isRouteActive) return;
            var queues = _model.GetProductionQueueSnapshots();
            foreach (FactionUnitUi unit in _factionUnitsUi)
            {
                bool queued = IsQueued(unit, queues);
                bool unlocked = _presenter.IsUnitAvailable(unit.FactionData);
                bool atLimit = _presenter.IsUnitLimitReached(unit.Request);
                unit.SetActive(IsInProductionMenu(unit, _model.SelectionType) &&
                               unlocked && !(atLimit && HidesAtLimit(unit)));
                unit.RenderAvailability(unlocked,
                    credits >= unit.FactionData.Price, queued, battleEnded, atLimit);
            }
            if (_isInitialized) _productionMenus.RefreshAvailableCards(_factionUnitsUi);
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

        private FactionUnitUi AddUi(UnitRequest unitRequest) => AddUi(unitRequest, GetRow(unitRequest));

        private FactionUnitUi AddUi(UnitRequest unitRequest, RectTransform row)
        {
            FactionUnitUi unitUi = Instantiate(row == unitsRow ? stationUnitPrefab : factionUnitPrefab, row);
            unitUi.SetData(unitRequest.FactionData, this, unitRequest);
            if (unitRequest is ShipUnitRequest or SquadronUnitRequest)
                unitUi.SetClassFrame(_presenter.GetUnitClass(unitRequest));
            tooltipHover.Register(unitUi.TooltipTrigger);
            _factionUnitsUi.Add(unitUi);
            return unitUi;
        }

        private RectTransform GetRow(UnitRequest request) => request switch
        {
            ShipUnitRequest or SquadronUnitRequest => unitsRow,
            ResearchUnitRequest => _productionMenus.ResearchCards,
            DefendPlatformUnitRequest or MiningFacilityUnitRequest or SecondStationUnitRequest =>
                _productionMenus.StructureCards,
            _ => throw new ArgumentOutOfRangeException(nameof(request))
        };

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
            RefreshResearchBuff(researchType);
            RefreshUnitVisibility(_model.SelectionType);
        }

        private void RefreshResearchBuff(ResearchType type)
        {
            int count = _research.GetCompletedTiers(type);
            ResearchTierData tier = _roster.ResearchLines[type].Tiers[Math.Max(0, count - 1)];
            _researchBuffs[type].Render(type, tier.FactionData.Icon, count);
        }

        private void CreateLevelUnit()
        {
            FactionData levelData = _model.GetCurrentLevelFactionData();
            if (levelData == null)
            {
                return;
            }

            _levelCards = AddLevelCards(_unitRequestFactory.ConstructUnitRequest(levelData, _model.CurrentLevel));
        }

        private void UpdateUnits(int level)
        {
            RemoveLevelCards(ref _levelCards);
            CreateLevelUnit();
            RefreshUnitVisibility(_model.SelectionType);
        }

        // The second station's level-up cards exist only while that station stands.
        private void CreateSecondLevelUnit()
        {
            if (!_secondStation.IsBuilt) return;
            FactionData levelData = _stationLevels.GetSecondStationLevelFactionData(_secondStation.CurrentLevel);
            if (levelData == null) return;
            _secondLevelCards = AddLevelCards(
                _unitRequestFactory.ConstructSecondStationLevelRequest(levelData, _secondStation.CurrentLevel));
        }

        private void UpdateSecondLevelUnit(int level)
        {
            RemoveLevelCards(ref _secondLevelCards);
            CreateSecondLevelUnit();
            RefreshUnitVisibility(_model.SelectionType);
        }

        private void HandleSecondStationBuiltChanged(bool isBuilt) => UpdateSecondLevelUnit(_secondStation.CurrentLevel);

        private (FactionUnitUi Station, FactionUnitUi Research) AddLevelCards(UnitRequest request) =>
            (AddUi(request, unitsRow), AddUi(request, _productionMenus.ResearchCards));

        private void RemoveLevelCards(ref (FactionUnitUi Station, FactionUnitUi Research) cards)
        {
            if (cards.Station == null) return;

            _factionUnitsUi.Remove(cards.Station);
            _factionUnitsUi.Remove(cards.Research);
            cards.Station.SetActive(false);
            cards.Research.SetActive(false);
            cards.Station.Destroy();
            cards.Research.Destroy();
            cards = default;
        }

        private static bool IsQueued(FactionUnitUi unit, IReadOnlyList<ProductionQueueSnapshot> queues)
        {
            foreach (ProductionQueueSnapshot queue in queues)
                if (queue.UnitRequest.Id == unit.RequestId) return true;
            return false;
        }

        // Heroes, structure population caps, station upgrades and research tiers hide at their limit.
        // Ship and squadron queue limits only block the card: it becomes buyable again as the queue drains.
        private static bool HidesAtLimit(FactionUnitUi unit) =>
            unit.FactionData.IsHero || unit.Request is MiningFacilityUnitRequest or DefendPlatformUnitRequest
                or LevelUnitRequest or ResearchUnitRequest or SecondStationUnitRequest or SecondStationLevelUnitRequest;

        private bool IsInProductionMenu(FactionUnitUi card, SelectionType selectionType) => card.Request switch
        {
            ResearchUnitRequest or DefendPlatformUnitRequest or MiningFacilityUnitRequest
                or SecondStationUnitRequest => true,
            LevelUnitRequest => card.transform.parent != unitsRow ||
                                selectionType == SelectionType.Base && !_model.IsSecondStationSelected,
            SecondStationLevelUnitRequest => card.transform.parent != unitsRow ||
                                             selectionType == SelectionType.Base && _model.IsSecondStationSelected,
            _ => selectionType == SelectionType.Base
        };

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            RefreshUnitVisibility(selectionType);
        }

        private void RefreshUnitVisibility(SelectionType selectionType)
        {
            if (!_isRouteActive)
            {
                foreach (FactionUnitUi unitUi in _factionUnitsUi)
                    unitUi.SetActive(false);
                return;
            }

            FactionUnitUi firstShip = null, firstScout = null, level = null;
            FactionUnitUi economyResearch = null, fleetResearch = null, newShip = null, miningFacility = null;
            bool isNewShipCarrier = false;
            foreach (FactionUnitUi unitUi in _factionUnitsUi
                         .OrderBy(unit => _presenter.GetUnitSortOrder(unit.Request))
                         .ThenBy(unit => unit.Request is ShipUnitRequest or SquadronUnitRequest
                             ? unit.FactionData.Price : 0)
                         .ThenByDescending(unit => _presenter.IsUnitAvailable(unit.FactionData)))
            {
                unitUi.transform.SetAsLastSibling();
                bool isShown = _presenter.IsUnitAvailable(unitUi.FactionData) &&
                               !(HidesAtLimit(unitUi) && _presenter.IsUnitLimitReached(unitUi.Request)) &&
                               IsInProductionMenu(unitUi, selectionType);
                unitUi.SetActive(isShown);
                if (!isShown) continue;
                switch (unitUi.Request)
                {
                    case ShipUnitRequest:
                        // Hints ask for several of the marked ship (Queue 6); a hero is built only once.
                        bool isRepeatable = !unitUi.FactionData.IsHero;
                        if (firstShip == null && isRepeatable) firstShip = unitUi;
                        if (_presenter.IsUnlockedAtCurrentLevel(unitUi.FactionData) &&
                            IsBetterNewShip(unitUi, newShip, isNewShipCarrier, out bool isCarrier))
                        {
                            newShip = unitUi;
                            isNewShipCarrier = isCarrier;
                        }
                        // Scout eligibility follows unit class, independently of production sorting.
                        if (firstScout == null && isRepeatable && _presenter.IsScout(unitUi.Request))
                            firstScout = unitUi;
                        break;
                    case SquadronUnitRequest:
                        if (firstScout == null) firstScout = unitUi;
                        break;
                    case ResearchUnitRequest research:
                        bool isEconomy = ResearchGroup.IsEconomy(research.Key);
                        if (isEconomy && economyResearch == null) economyResearch = unitUi;
                        if (!isEconomy && fleetResearch == null) fleetResearch = unitUi;
                        break;
                    case LevelUnitRequest when unitUi.transform.parent == unitsRow:
                        level = unitUi;
                        break;
                    case MiningFacilityUnitRequest:
                        if (miningFacility == null) miningFacility = unitUi;
                        break;
                }
            }
            MarkTutorialButton(TutorialAnchorId.BuildShipButton, firstShip);
            MarkTutorialButton(TutorialAnchorId.ScoutShipButton, firstScout);
            MarkTutorialButton(TutorialAnchorId.LevelUpButton, level);
            MarkTutorialButton(TutorialAnchorId.EconomyResearchButton, economyResearch);
            MarkTutorialButton(TutorialAnchorId.FleetResearchButton, fleetResearch);
            MarkTutorialButton(TutorialAnchorId.NewShipButton, newShip);
            MarkTutorialButton(TutorialAnchorId.MiningFacilityButton, miningFacility);
            if (_isInitialized) _productionMenus.RefreshAvailableCards(_factionUnitsUi);
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
            else tutorialAnchor.Mark(id, (RectTransform)unitUi.transform, unitUi.TutorialHighlight);
        }
    }
}
