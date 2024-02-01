using EmpireAtWar.Models.Ship;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Ship;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController: Controller<ShipUiModel>, IInitializable, ILateDisposable
    {
        private readonly INavigationService navigationService;
        private readonly IShipService shipService;

        public ShipUiController(ShipUiModel model, INavigationService navigationService, IShipService shipService) : base(model)
        {
            this.navigationService = navigationService;
            this.shipService = shipService;
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
                //temp solution
                IShipModelObserver shipModelObserver = navigationService.Selectable.ModelObserver.GetModelObserver<IShipModelObserver>();
                if (shipModelObserver != null)
                {
                    Model.ShipIcon = shipModelObserver.ShipIcon;
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