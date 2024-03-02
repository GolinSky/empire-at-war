using System;
using System.Collections.Generic;
using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Factions
{
    public class FactionUiView : View<IPlayerFactionModelObserver, IFactionCommand>
    {
        [SerializeField] private Canvas controlCanvas;
        [SerializeField] private Button exitButton;
        [SerializeField] private Transform shipUnitParent;
        [SerializeField] private BuildPipelineView pipelineView;

        private List<FactionUnitUi> factionUnitsUi = new List<FactionUnitUi>();

        protected override void OnInitialize()
        {
            pipelineView.Init();
            HandleSelectionChanged(Model.SelectionType);
            foreach (var data in Model.FactionData)
            {
                FactionUnitUi unitUi = Instantiate(Model.ShipUnit, shipUnitParent);
                unitUi.SetData(data.Value, data.Key, HandleClick);
                factionUnitsUi.Add(unitUi);
                if (data.Value.AvailableLevel > Model.CurrentLevel)
                {
                    unitUi.SetActive(false);
                }
            }

            Model.OnSelectionTypeChanged += HandleSelectionChanged;
            Model.OnLevelUpgraded += UpdateUnits;
            Model.OnShipBuild += BuildShip;
            exitButton.onClick.AddListener(ExitUi);
            pipelineView.OnFinishSequence += HandleEndOfBuilding;
        }

        protected override void OnDispose()
        {
            Model.OnSelectionTypeChanged -= HandleSelectionChanged;
            Model.OnLevelUpgraded -= UpdateUnits;
            Model.OnShipBuild -= BuildShip;
            exitButton.onClick.RemoveListener(ExitUi);
            pipelineView.OnFinishSequence -= HandleEndOfBuilding;
        }
        
        private void HandleEndOfBuilding(bool isSuccess, string id)
        {
            if (!isSuccess)
            {
                Command.RevertBuilding(id);
            }
            else
            {
                if (Enum.TryParse(id, out ShipType shipType))
                {
                    Command.BuildShip(shipType);
                }
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

        private void BuildShip(ShipType shipType)
        {
            FactionData factionData = Model.FactionData[shipType];
            pipelineView.AddPipeline(shipType.ToString(), factionData.Icon, factionData.BuildTime);
        }

        private void HandleClick(ShipType shipType)
        {
            Command.TryPurchaseShip(shipType);
        }

        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType == SelectionType.Base;
        }
    }
}