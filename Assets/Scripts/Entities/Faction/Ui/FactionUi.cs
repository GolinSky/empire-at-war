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
        void SetPresenter(IFactionPresenter presenter);
        void SetData(PlayerFactionData data);
        void SetUnitRequestFactory(IUnitRequestFactory unitRequestFactory);
        void SetParent(Transform parent);
        void Show();
        void Hide();
        void Initialize();
        void Dispose();
    }

    public class FactionUi : BaseUi, IFactionUi, IFactionView
    {
        private readonly List<FactionUnitUi> _factionUnitsUi =
            new List<FactionUnitUi>();

        private FactionUnitUi _levelFactionUnitUi;
        private IPlayerFactionModelObserver _model;
        private IFactionPresenter _presenter;
        private PlayerFactionData _data;
        private IUnitRequestFactory _unitRequestFactory;
        private Transform _unitParent;
        private bool _isInitialized;
        private bool _isRouteActive = true;

        public void SetModel(IPlayerFactionModelObserver model)
        {
            _model = model;
        }

        public void SetPresenter(IFactionPresenter presenter)
        {
            _presenter = presenter;
        }

        public void SetData(PlayerFactionData data)
        {
            _data = data;
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
            if (_model == null || _presenter == null || _data == null ||
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

            foreach (var data in _data.GetShipFactionData(_model.FactionType))
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            CreateLevelUnit();

            foreach (var data in _data.GetMiningFactionData())
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            foreach (var data in _data.GetDefendPlatformData())
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(
                    data.Value,
                    data.Key));
            }

            _model.OnSelectionTypeChanged += HandleSelectionChanged;
            _model.OnLevelUpgraded += UpdateUnits;
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

        private void AddUi(UnitRequest unitRequest)
        {
            FactionUnitUi unitUi = Instantiate(_data.FactionUnit, _unitParent);
            unitUi.SetData(unitRequest.FactionData, this, unitRequest);
            _factionUnitsUi.Add(unitUi);
        }

        private void CreateLevelUnit()
        {
            FactionData levelData = _model.GetCurrentLevelFactionData();
            if (levelData == null)
            {
                return;
            }

            _levelFactionUnitUi = Instantiate(_data.FactionUnit, _unitParent);
            LevelUnitRequest levelUnitRequest =
                _unitRequestFactory.ConstructUnitRequest(
                    levelData,
                    _model.CurrentLevel);
            _levelFactionUnitUi.SetData(levelData, this, levelUnitRequest);
            _factionUnitsUi.Add(_levelFactionUnitUi);
        }

        private void UpdateUnits(int level)
        {
            if (_levelFactionUnitUi != null)
            {
                _factionUnitsUi.Remove(_levelFactionUnitUi);
                _levelFactionUnitUi.Destroy();
            }

            CreateLevelUnit();
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
