using System;
using System.Collections;
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
        
        protected override void OnInitialize()
        {
            pipelineView.Init();
            HandleSelectionChanged(Model.SelectionType);
            Model.OnSelectionTypeChanged += HandleSelectionChanged;
            foreach (var data in Model.FactionData)
            {
                FactionUnitUi unitUi = Instantiate(Model.ShipUnit, shipUnitParent);
                unitUi.SetData(data.Value, data.Key, HandleClick);
            }
            exitButton.onClick.AddListener(ExitUi);
        }
        
        protected override void OnDispose()
        {
            Model.OnSelectionTypeChanged -= HandleSelectionChanged;
            exitButton.onClick.RemoveListener(ExitUi);
        }

        private void ExitUi()
        {
            Command.CloseSelection();
        }

        private void HandleClick(ShipType shipType)
        {
            if (Command.TryPurchaseShip(shipType)) // todo: refactor
            {
                FactionData factionData = Model.FactionData[shipType];
                float buildTime = pipelineView.AddPipeline(shipType.ToString(),factionData.Icon, factionData.BuildTime);
                StartCoroutine(InvokeAfterDelay(() => Command.BuildShip(shipType), buildTime));
            }
        }

        private IEnumerator InvokeAfterDelay(Action action, float delay)
        {
            yield return new WaitForSeconds(delay);
            action.Invoke();
        }
        
        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType == SelectionType.Base;
        }
    }
}