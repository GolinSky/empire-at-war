using System.Collections.Generic;
using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Ship;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController: Controller<ShipUiModel>, IInitializable, ILateDisposable, IShipUiCommand
    {
        private readonly INavigationService navigationService;

        
        public ShipUiController(ShipUiModel model, INavigationService navigationService) : base(model)
        {
            this.navigationService = navigationService;
        }

        public void Initialize()
        {
            navigationService.OnTypeChanged += UpdateType;
        }

        public void LateDispose()
        {
            navigationService.OnTypeChanged -= UpdateType;
        }
        
        private void UpdateType(SelectionType selectionType)
        {
            if (selectionType == SelectionType.Ship)
            {
                IShipModelObserver shipModelObserver = navigationService.Selectable.ModelObserver.GetModelObserver<IShipModelObserver>();
                if (shipModelObserver != null)
                {
                    Model.ShipIcon = Model.GetShipIcon(shipModelObserver.ShipType);
                }
            }
            Model.UpdateSelection(selectionType);
        }

        public void CloseSelection()
        {
            navigationService.RemoveSelectable();
        }
    }
}