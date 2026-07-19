using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Presenters.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;

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
        void Initialize();
        void Dispose();
    }

    public class FactionUi : BaseUi, IFactionUi, IFactionView
    {
        [SerializeField] private Canvas controlCanvas;
        [SerializeField] private Button exitButton;
        [SerializeField] private Transform shipUnitParent;
        [SerializeField] private BuildPipelineView pipelineView;
        [SerializeField] private Button triggerUiButton;

        private readonly Dictionary<string, UnitRequest> _unitRequests = new();
        private readonly List<FactionUnitUi> _factionUnitsUi = new();

        private FactionUnitUi _levelFactionUnitUi;
        private UnitRequest _currentLevelUnitRequest;
        private IPlayerFactionModelObserver _model;
        private IFactionPresenter _presenter;
        private PlayerFactionData _data;
        private IUnitRequestFactory _unitRequestFactory;
        private bool _isInitialized;

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

        public void Initialize()
        {
            if (_model == null || _presenter == null || _data == null || _unitRequestFactory == null)
            {
                throw new InvalidOperationException("Faction UI dependencies must be set before initialization.");
            }

            if (_isInitialized)
            {
                return;
            }

            pipelineView.Init();
            HandleSelectionChanged(_model.SelectionType);
            foreach (var data in _data.GetShipFactionData(_model.FactionType))
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(data.Value, data.Key));
            }

            _currentLevelUnitRequest = ConstructLevelUnitRequest();
            
            foreach (var data in _data.GetMiningFactionData())
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(data.Value, data.Key));
            }
            
            foreach (var data in _data.GetDefendPlatformData())
            {
                AddUi(_unitRequestFactory.ConstructUnitRequest(data.Value, data.Key));
            }
            
            _model.OnSelectionTypeChanged += HandleSelectionChanged;
            _model.OnLevelUpgraded += UpdateUnits;
            _model.OnUnitBuild += BuildUnit;
            exitButton.onClick.AddListener(ExitUi);
            pipelineView.OnFinishSequence += HandleEndOfBuilding;
            triggerUiButton.onClick.AddListener(_presenter.ChangeSelection);
            _isInitialized = true;
        }
        
        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            _model.OnSelectionTypeChanged -= HandleSelectionChanged;
            _model.OnLevelUpgraded -= UpdateUnits;
            _model.OnUnitBuild -= BuildUnit;
            exitButton.onClick.RemoveListener(ExitUi);
            pipelineView.OnFinishSequence -= HandleEndOfBuilding;
            triggerUiButton.onClick.RemoveListener(_presenter.ChangeSelection);
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void AddUi(UnitRequest unitRequest)
        {
            FactionUnitUi unitUi = Instantiate(_data.FactionUnit, shipUnitParent);
            unitUi.SetData(unitRequest.FactionData,this, unitRequest);
            _factionUnitsUi.Add(unitUi);
            if (unitRequest.FactionData.AvailableLevel > _model.CurrentLevel)
            {
                unitUi.SetActive(false);
            }
            _unitRequests.Add(unitRequest.Id, unitRequest);
        }
        
        private UnitRequest ConstructLevelUnitRequest()// refactor
        {
            FactionData levelData = _model.GetCurrentLevelFactionData();
            if (levelData != null)
            {
                _levelFactionUnitUi = Instantiate(_data.FactionUnit, shipUnitParent);
                LevelUnitRequest levelUnitRequest = _unitRequestFactory.ConstructUnitRequest(levelData, _model.CurrentLevel);
                _levelFactionUnitUi.SetData(levelData, this, levelUnitRequest);
                _unitRequests.Add(levelUnitRequest.Id, levelUnitRequest);
                return levelUnitRequest;
            }

            return null;
        }
        
        private void HandleEndOfBuilding(bool isSuccess, string id)
        {
            UnitRequest unitRequest = _unitRequests[id];

            if (!isSuccess)
            {
                _presenter.RevertBuilding(unitRequest);
            }
            else
            {
                _presenter.BuildUnit(unitRequest);
            }
        }
        
        private void UpdateUnits(int level)
        {
            foreach (FactionUnitUi factionUnitUi in _factionUnitsUi)
            {
                factionUnitUi.SetActive(factionUnitUi.Level <= level);
            }

            _levelFactionUnitUi.Destroy();
            if (_currentLevelUnitRequest != null)
            {
                _unitRequests.Remove(_currentLevelUnitRequest.Id);
            }
            _currentLevelUnitRequest = ConstructLevelUnitRequest();
        
        }

        private void ExitUi()
        {
            _presenter.CloseSelection();
        }

        private void BuildUnit(UnitRequest unitRequest)
        {
            pipelineView.AddPipeline(unitRequest.Id, unitRequest.FactionData.Icon, unitRequest.FactionData.BuildTime);
        }

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType == SelectionType.Base;
        }

        public void BuyUnit(UnitRequest unitRequest)
        {
            _presenter.TryPurchaseUnit(unitRequest);
        }
    }
}
