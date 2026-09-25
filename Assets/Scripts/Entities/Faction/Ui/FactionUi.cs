using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Presenters.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Views.Factions
{
    public interface IFactionView
    {
        void BuyUnit(UnitRequest unitRequest);
    }

    public interface IFactionUi
    {
        void SetModel(IPlayerFactionModelObserver model);
        void SetResearch(IFactionResearchModelObserver research);
        void SetPresenter(IFactionPresenter presenter);
        void SetData(FactionsData factionsData);
        void SetUnitRequestFactory(IUnitRequestFactory unitRequestFactory);
        void SetParent(Transform parent);
        void Show();
        void Hide();
        void Initialize();
        void Dispose();
    }

    public class FactionUi : BaseUi, IFactionUi, IFactionView
    {
        [SerializeField] private FactionUnitUi factionUnitPrefab;

        private readonly List<FactionUnitUi> _factionUnitsUi =
            new List<FactionUnitUi>();

        private readonly Dictionary<ResearchType, FactionUnitUi> _researchUnitsUi =
            new Dictionary<ResearchType, FactionUnitUi>();

        private FactionUnitUi _levelFactionUnitUi;
        private IPlayerFactionModelObserver _model;
        private IFactionResearchModelObserver _research;
        private IFactionPresenter _presenter;
        private FactionsData _factionsData;
        private IUnitRequestFactory _unitRequestFactory;
        private Transform _unitParent;
        private bool _isInitialized;
        private bool _isRouteActive = true;

        public void SetModel(IPlayerFactionModelObserver model)
        {
            _model = model;
        }

        public void SetResearch(IFactionResearchModelObserver research)
        {
            _research = research;
        }

        public void SetPresenter(IFactionPresenter presenter)
        {
            _presenter = presenter;
        }

        public void SetData(FactionsData factionsData)
        {
            _factionsData = factionsData;
        }

        public void SetUnitRequestFactory(IUnitRequestFactory unitRequestFactory)
        {
            _unitRequestFactory = unitRequestFactory;
        }

        public override void SetParent(Transform parent)
        {
            base.SetParent(parent);
            _unitParent = parent;

            for (int i = 0; i < _factionUnitsUi.Count; i++)
            {
                _factionUnitsUi[i].transform.SetParent(parent, false);
            }
        }

        public void Initialize()
        {
            if (_model == null || _research == null || _presenter == null || _factionsData == null ||
                _unitRequestFactory == null)
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

            foreach (var data in _factionsData.GetShipFactionData(_model.FactionType))
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            foreach (var data in _factionsData.GetSquadronFactionData(_model.FactionType))
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            CreateLevelUnit();

            foreach (var data in _factionsData.MiningFactionsData)
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            foreach (var data in _factionsData.DefendPlatformDictionary)
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            foreach (var data in _factionsData.SuperWeaponFactionData)
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
            _research.OnResearchCompleted -= UpdateResearchUnit;
            _isInitialized = false;
        }

        public void BuyUnit(UnitRequest unitRequest)
        {
            _presenter.TryPurchaseUnit(unitRequest);
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
            _factionUnitsUi.Add(unitUi);
            return unitUi;
        }

        private void CreateResearchUnit(ResearchType researchType)
        {
            if (_research.TryGetNextTier(researchType, out ResearchTierData tier))
            {
                _researchUnitsUi[researchType] = AddUi(
                    _unitRequestFactory.ConstructUnitRequest(tier.FactionData, researchType));
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

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            RefreshUnitVisibility(selectionType);
        }

        private void RefreshUnitVisibility(SelectionType selectionType)
        {
            bool isSelectionVisible =
                _isRouteActive && selectionType == SelectionType.Base;

            for (int i = 0; i < _factionUnitsUi.Count; i++)
            {
                FactionUnitUi unitUi = _factionUnitsUi[i];
                unitUi.SetActive(
                    isSelectionVisible && unitUi.Level <= _model.CurrentLevel);
            }
        }
    }
}
