using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using Zenject;

namespace EmpireAtWar.Controllers.Factions
{
    public class FactionController : Controller<FactionModel>, IInitializable, ILateDisposable
    {
        private readonly INavigationService navigationService;

        public FactionController(FactionModel model, INavigationService navigationService) : base(model)
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
            Model.SelectionType = selectionType;
        }
    }
}