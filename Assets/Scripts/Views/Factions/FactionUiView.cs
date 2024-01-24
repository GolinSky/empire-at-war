using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.Factions
{
    public class FactionUiView : View<IPlayerFactionModelObserver, IFactionCommand>
    {
        [SerializeField] private Canvas controlCanvas;
        [SerializeField] private Button exitButton;
        [Inject] private SkirmishGameData SkirmishGameData { get; }

        protected override void OnInitialize()
        {
            HandleSelectionChanged(Model.SelectionType);
            Model.OnSelectionTypeChanged += HandleSelectionChanged;
            foreach (var data in Model.GetFactionData(SkirmishGameData.PlayerFactionType))//hardcode
            {
                FactionUnitUi unitUi = Instantiate(Model.ShipUnit, controlCanvas.transform);
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
            Command.BuildShip(shipType);
        }
        
        private void HandleSelectionChanged(SelectionType selectionType)
        {
            controlCanvas.enabled = selectionType == SelectionType.Base;
        }
    }
}