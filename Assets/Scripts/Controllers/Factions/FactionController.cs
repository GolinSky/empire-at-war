using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Skirmish;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<PlayerFactionModel>, IInitializable, ILateDisposable
    {
        private readonly INavigationService navigationService;

        public FactionController(PlayerFactionModel model, INavigationService navigationService, SkirmishGameData skirmishGameData) : base(model)
        {
            this.navigationService = navigationService;
            model.FactionData = model.GetFactionData(skirmishGameData.PlayerFactionType);
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
    }
}