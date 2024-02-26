using System.Collections.Generic;
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
    public class ShipUiController: Controller<ShipUiModel>, IInitializable, ILateDisposable
    {
        private readonly INavigationService navigationService;

        private Dictionary<ShipType, ShipInfoUi> shipUiDictionary = new Dictionary<ShipType, ShipInfoUi>();
        
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
                    ShipInfoUi shipInfoUi = null;
                    if (!shipUiDictionary.TryGetValue(shipModelObserver.ShipType, out shipInfoUi))
                    {
                        shipInfoUi =
                            Object.Instantiate(Model.GetShipInfoUi(shipModelObserver.ShipType));
                        shipUiDictionary.Add(shipModelObserver.ShipType, shipInfoUi);
                    }

                    ShipInfoUi previousUi = Model.ShipInfoUi;
                    if (previousUi != null)
                    {
                        previousUi.SetActive(false);
                    }
                    //todo: disable prev ui
                    Model.ShipInfoUi = shipModelObserver.FillWithData(shipInfoUi);
                    Model.ShipInfoUi.SetActive(true);
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