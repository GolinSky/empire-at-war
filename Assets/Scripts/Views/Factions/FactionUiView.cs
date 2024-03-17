using System;
using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.Factions
{
    public interface IFactionView
    {
        void BuyUnit(UnitRequest shipUnitRequest, FactionData factionData);
    }
    public class FactionUiView : View<IPlayerFactionModelObserver, IFactionCommand>, IFactionView
    {
        [SerializeField] private Canvas controlCanvas;
        [SerializeField] private Button exitButton;
        [SerializeField] private Transform shipUnitParent;
        [SerializeField] private BuildPipelineView pipelineView;

        private Dictionary<string, UnitRequest> unitRequests = new Dictionary<string, UnitRequest>();
        private List<UnitRequest> buildingUnits = new List<UnitRequest>();
        private List<FactionUnitUi> factionUnitsUi = new List<FactionUnitUi>();

        private FactionUnitUi levelFactionUnitUi;
        private UnitRequest currentLevelUnitRequest;

        [Inject]
        private IUnitRequestFactory UnitRequestFactory { get; }
        protected override void OnInitialize()
        {
            pipelineView.Init();
            HandleSelectionChanged(Model.SelectionType);
            foreach (var data in Model.FactionData)
            {
                FactionUnitUi unitUi = Instantiate(Model.ShipUnit, shipUnitParent);
                unitUi.SetData(data.Value,this, ConstructUnitRequest(data.Key, data.Value));
                factionUnitsUi.Add(unitUi);
                if (data.Value.AvailableLevel > Model.CurrentLevel)
                {
                    unitUi.SetActive(false);
                }
            }

            currentLevelUnitRequest = ConstructLevelUnitRequest();
            
            foreach (var pair in Model.MiningFactions)
            {
                FactionUnitUi unitUi = Instantiate(Model.ShipUnit, shipUnitParent);
                unitUi.SetData(pair.Value,this, ConstructUnitRequest(pair.Key, pair.Value));
                factionUnitsUi.Add(unitUi);
                if (pair.Value.AvailableLevel > Model.CurrentLevel)
                {
                    unitUi.SetActive(false);
                }
            }
            
            Model.OnSelectionTypeChanged += HandleSelectionChanged;
            Model.OnLevelUpgraded += UpdateUnits;
            Model.OnUnitBuild += BuildUnit;
            exitButton.onClick.AddListener(ExitUi);
            pipelineView.OnFinishSequence += HandleEndOfBuilding;
        }

        private UnitRequest ConstructUnitRequest(ShipType shipType, FactionData factionData)
        {
            UnitRequest unitRequest = UnitRequestFactory.ConstructUnitRequest(factionData, shipType);
            unitRequests.Add(unitRequest.Id, unitRequest);
            return unitRequest;
        }
        
        private UnitRequest ConstructUnitRequest(MiningFacilityType facilityType, FactionData factionData)
        {
            UnitRequest unitRequest = UnitRequestFactory.ConstructUnitRequest(factionData, facilityType);
            unitRequests.Add(unitRequest.Id, unitRequest);
            return unitRequest;
        }
        
        private UnitRequest ConstructLevelUnitRequest()
        {
            FactionData levelData = Model.GetCurrentLevelFactionData();
            if (levelData != null)
            {
                levelFactionUnitUi = Instantiate(Model.ShipUnit, shipUnitParent);
                LevelUnitRequest levelUnitRequest = UnitRequestFactory.ConstructUnitRequest(levelData, Model.CurrentLevel);
                levelFactionUnitUi.SetData(levelData, this, levelUnitRequest);
                unitRequests.Add(levelUnitRequest.Id, levelUnitRequest);
                return levelUnitRequest;
            }

            return null;
        }
        protected override void OnDispose()
        {
            Model.OnSelectionTypeChanged -= HandleSelectionChanged;
            Model.OnLevelUpgraded -= UpdateUnits;
            Model.OnUnitBuild -= BuildUnit;
            exitButton.onClick.RemoveListener(ExitUi);
            pipelineView.OnFinishSequence -= HandleEndOfBuilding;
        }
        
        private void HandleEndOfBuilding(bool isSuccess, string id)
        {
            UnitRequest unitRequest = unitRequests[id];

            if (!isSuccess)
            {
                Command.RevertBuilding(unitRequest);
            }
            else
            {
                Command.BuildUnit(unitRequest);
            }
            buildingUnits.Remove(unitRequest);
        }
        
        private void UpdateUnits(int level)
        {
            foreach (FactionUnitUi factionUnitUi in factionUnitsUi)
            {
                factionUnitUi.SetActive(factionUnitUi.Level <= level);
            }

            levelFactionUnitUi.Destroy();
            if (currentLevelUnitRequest != null)
            {
                unitRequests.Remove(currentLevelUnitRequest.Id);
            }
            currentLevelUnitRequest = ConstructLevelUnitRequest();
        
        }

        private void ExitUi()
        {
            Command.CloseSelection();
        }

        private void BuildUnit(UnitRequest unitRequest)
        {
            buildingUnits.Add(unitRequest);
            pipelineView.AddPipeline(unitRequest.Id, unitRequest.FactionData.Icon, unitRequest.FactionData.BuildTime);
        }

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType == SelectionType.Base;
        }

        public void BuyUnit(UnitRequest shipUnitRequest, FactionData factionData)
        {
            int amount = buildingUnits.Count(x => x.Id.Equals(shipUnitRequest.Id));
            if (factionData.MaxCount > amount)
            {
                Command.TryPurchaseUnit(shipUnitRequest);
            }
        }
    }
}