using EmpireAtWar.Commands.Faction;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Economy;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.Reinforcement;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable, IFactionCommand
    {
        private readonly INavigationService navigationService;
        private readonly IReinforcementService reinforcementService;
        private readonly IEconomyService economyService;

        public FactionController(PlayerFactionModel model, INavigationService navigationService, IReinforcementService reinforcementService, IEconomyService economyService) : base(model)
        {
            this.navigationService = navigationService;
            this.reinforcementService = reinforcementService;
            this.economyService = economyService;
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

        public bool TryPurchaseShip(ShipType shipType)
        {
            return economyService.TryBuyUnit(Model.FactionData[shipType].Price);
        }
    }
}