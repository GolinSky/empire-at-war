using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Reinforcement;
using LightWeightFramework.Controller;
using WorkShop.LightWeightFramework.Command;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand
    {
        private readonly INavigationService navigationService;
        private readonly IReinforcementService reinforcementService;

        public FactionController(PlayerFactionModel model, INavigationService navigationService, IReinforcementService reinforcementService) : base(model)
        {
            this.navigationService = navigationService;
            this.reinforcementService = reinforcementService;
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
            reinforcementService.AddReinforcement(shipType);
        }
    }
}