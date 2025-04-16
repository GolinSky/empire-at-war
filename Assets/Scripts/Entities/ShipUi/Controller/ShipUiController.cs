using EmpireAtWar.Commands.ShipUi;
using EmpireAtWar.Models.ShipUi;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Ship;
using EmpireAtWar.Ui.Base;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.ShipUi
{
    public class ShipUiController: Controller<ShipUiModel>, IInitializable, ILateDisposable, IShipUiCommand
    {
        private readonly INavigationService _navigationService;
        private readonly IUiService _uiService;


        public ShipUiController(ShipUiModel model, INavigationService navigationService, IUiService uiService) : base(model)
        {
            _navigationService = navigationService;
            _uiService = uiService;
        }

        public void Initialize()
        {
            _navigationService.OnTypeChanged += UpdateType;
            _uiService.CreateUi(UiType.Ship);
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