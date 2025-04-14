using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ship;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController: Controller<ShipUiModel>, IInitializable, ILateDisposable, IShipUiCommand
    {
        private readonly INavigationService _navigationService;

        
        public ShipUiController(ShipUiModel model, INavigationService navigationService) : base(model)
        {
            _navigationService = navigationService;
        }

        public void Initialize()
        {
            _navigationService.OnTypeChanged += UpdateType;
        }

        public void LateDispose()
        {
            _navigationService.OnTypeChanged -= UpdateType;
        }
        
        private void UpdateType(SelectionType selectionType)
        {
            if (selectionType == SelectionType.Ship)
            {
                IShipModelObserver shipModelObserver = _navigationService.Selectable.ModelObserver.GetModelObserver<IShipModelObserver>();
                if (shipModelObserver != null)
                {
                    Model.ShipIcon = Model.GetShipIcon(shipModelObserver.ShipType);
                }
            }
            Model.UpdateSelection(selectionType);
        }

        public void CloseSelection()
        {
            _navigationService.RemoveSelectable();
        }
    }
}