using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Factions
{
    public interface IFactionView
    {
        void BuyUnit(UnitRequest shipUnitRequest);
    }
    public class FactionUiView : View<IPlayerFactionModelObserver, IFactionCommand>, IFactionView
    {
        [SerializeField] private Canvas controlCanvas;
        [SerializeField] private Button exitButton;
        [SerializeField] private Transform shipUnitParent;
        [SerializeField] private BuildPipelineView pipelineView;

        private List<FactionUnitUi> factionUnitsUi = new List<FactionUnitUi>();
        private Dictionary<string, UnitRequest> unitRequests = new Dictionary<string, UnitRequest>();
 
        protected override void OnInitialize()
        {
            pipelineView.Init();
            HandleSelectionChanged(Model.SelectionType);
            foreach (var data in Model.FactionData)
            {
                FactionUnitUi unitUi = Instantiate(Model.ShipUnit, shipUnitParent);
                unitUi.SetData(data.Value,this, ConstructShipUnit(data.Value, data.Key));
                factionUnitsUi.Add(unitUi);
                if (data.Value.AvailableLevel > Model.CurrentLevel)
                {
                    unitUi.SetActive(false);
                }
            }

            // FactionData levelData = Model.GetCurrentLevelFactionData();
            //
            // FactionUnitUi factionUnitUi = Instantiate(Model.ShipUnit, shipUnitParent);
            // factionUnitUi.SetData(levelData, HandleLevel);
            
            Model.OnSelectionTypeChanged += HandleSelectionChanged;
            Model.OnLevelUpgraded += UpdateUnits;
            Model.OnUnitBuild += BuildShip;
            exitButton.onClick.AddListener(ExitUi);
            pipelineView.OnFinishSequence += HandleEndOfBuilding;
        }

        private UnitRequest ConstructShipUnit(FactionData factionData, ShipType shipType)
        {
            UnitRequest unitRequest = new ShipUnitRequest(factionData, shipType);
            unitRequests.Add(unitRequest.Id, unitRequest);
            return unitRequest;
        }

        private void HandleLevel()
        {
            
        }

        protected override void OnDispose()
        {
            Model.OnSelectionTypeChanged -= HandleSelectionChanged;
            Model.OnLevelUpgraded -= UpdateUnits;
            Model.OnUnitBuild -= BuildShip;
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
                Command.BuildShip(unitRequest);
            }
        }
        
        private void UpdateUnits(int level)
        {
            foreach (FactionUnitUi factionUnitUi in factionUnitsUi)
            {
                factionUnitUi.SetActive(level <= factionUnitUi.Level);
            }
        }

        private void ExitUi()
        {
            Command.CloseSelection();
        }

        private void BuildShip(UnitRequest unitRequest)
        {
            pipelineView.AddPipeline(unitRequest.Id, unitRequest.FactionData.Icon, unitRequest.FactionData.BuildTime);
        }

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType == SelectionType.Base;
        }

        public void BuyUnit(UnitRequest shipUnitRequest)
        {
            Command.TryPurchaseShip(shipUnitRequest);
        }
    }
}