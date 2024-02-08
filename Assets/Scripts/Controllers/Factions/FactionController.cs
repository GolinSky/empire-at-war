using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Views.Ship;
using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Command;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand
    {
        private readonly INavigationService navigationService;
        private readonly ShipFacadeFactory shipFacadeFactory;

        public FactionController(PlayerFactionModel model, INavigationService navigationService,  ShipFacadeFactory shipFacadeFactory) : base(model)
        {
            this.navigationService = navigationService;
            this.shipFacadeFactory = shipFacadeFactory;
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
            Model.SelectionType = selectionType;
        }
        
        public void CloseSelection()
        {
            navigationService.RemoveSelectable();
        }

        public void BuildShip(ShipType shipType)
        {
            shipFacadeFactory.Create(PlayerType.Player, shipType);
        }

        public bool TryGetCommand<TCommand>(out TCommand command) where TCommand : ICommand
        {
            throw new System.NotImplementedException();
        }
    }
}