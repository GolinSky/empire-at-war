using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ui.Base;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.Factions
{
    public interface IFactionView
    {
        void BuyUnit(UnitRequest shipUnitRequest, FactionData factionData);
    }
    public class FactionUi : BaseUi<IPlayerFactionModelObserver, IFactionCommand>, IFactionView, IInitializable, ILateDisposable
    {
        [SerializeField] private Canvas controlCanvas;
        [SerializeField] private Button exitButton;
        [SerializeField] private Transform shipUnitParent;
        [SerializeField] private BuildPipelineView pipelineView;
        [SerializeField] private Button triggerUiButton;

        private Dictionary<string, UnitRequest> _unitRequests = new Dictionary<string, UnitRequest>();
        private List<UnitRequest> _buildingUnits = new List<UnitRequest>();
        private List<FactionUnitUi> _factionUnitsUi = new List<FactionUnitUi>();

        private FactionUnitUi _levelFactionUnitUi;
        private UnitRequest _currentLevelUnitRequest;

        [Inject]
        private IUnitRequestFactory UnitRequestFactory { get; }
        
        
        public void Initialize()
        {
            pipelineView.Init();
            HandleSelectionChanged(Model.SelectionType);
            foreach (var data in Model.ShipFactionData)
            {
                AddUi(UnitRequestFactory.ConstructUnitRequest(data.Value, data.Key));
            }

            _currentLevelUnitRequest = ConstructLevelUnitRequest();
            
            foreach (var data in Model.MiningFactions)
            {
                AddUi(UnitRequestFactory.ConstructUnitRequest(data.Value, data.Key));
            }
            
            foreach (var data in Model.DefendPlatforms)
            {
                AddUi(UnitRequestFactory.ConstructUnitRequest(data.Value, data.Key));
            }
            
            Model.OnSelectionTypeChanged += HandleSelectionChanged;
            Model.OnLevelUpgraded += UpdateUnits;
            Model.OnUnitBuild += BuildUnit;
            exitButton.onClick.AddListener(ExitUi);
            pipelineView.OnFinishSequence += HandleEndOfBuilding;
            triggerUiButton.onClick.AddListener(Command.ChangeSelection);
        }
        
        public void LateDispose()
        {
            Model.OnSelectionTypeChanged -= HandleSelectionChanged;
            Model.OnLevelUpgraded -= UpdateUnits;
            Model.OnUnitBuild -= BuildUnit;
            exitButton.onClick.RemoveListener(ExitUi);
            pipelineView.OnFinishSequence -= HandleEndOfBuilding;
            triggerUiButton.onClick.RemoveListener(Command.ChangeSelection);
        }

        private void AddUi(UnitRequest unitRequest)
        {
            FactionUnitUi unitUi = Instantiate(Model.ShipUnit, shipUnitParent);
            unitUi.SetData(unitRequest.FactionData,this, unitRequest);
            _factionUnitsUi.Add(unitUi);
            if (unitRequest.FactionData.AvailableLevel > Model.CurrentLevel)
            {
                unitUi.SetActive(false);
            }
            _unitRequests.Add(unitRequest.Id, unitRequest);
        }
        
        private UnitRequest ConstructLevelUnitRequest()// refactor
        {
            FactionData levelData = Model.GetCurrentLevelFactionData();
            if (levelData != null)
            {
                _levelFactionUnitUi = Instantiate(Model.ShipUnit, shipUnitParent);
                LevelUnitRequest levelUnitRequest = UnitRequestFactory.ConstructUnitRequest(levelData, Model.CurrentLevel);
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
                Command.RevertBuilding(unitRequest);
            }
            else
            {
                Command.BuildUnit(unitRequest);
            }
            _buildingUnits.Remove(unitRequest);
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
            Command.CloseSelection();
        }

        private void BuildUnit(UnitRequest unitRequest)
        {
            _buildingUnits.Add(unitRequest);
            pipelineView.AddPipeline(unitRequest.Id, unitRequest.FactionData.Icon, unitRequest.FactionData.BuildTime);
        }

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType == SelectionType.Base;
        }

        public void BuyUnit(UnitRequest shipUnitRequest, FactionData factionData)
        {
            int amount = _buildingUnits.Count(x => x.Id.Equals(shipUnitRequest.Id));
            if (factionData.MaxCount > amount)
            {
                Command.TryPurchaseUnit(shipUnitRequest);
            }
        }
    }
}