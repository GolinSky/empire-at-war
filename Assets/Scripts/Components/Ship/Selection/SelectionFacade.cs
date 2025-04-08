using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class SelectionFacade : PlaceholderFactory<IModel,IMovable, SelectionComponent>
    {
        private readonly INavigationService _navigationService;

        public SelectionFacade(INavigationService navigationService)
        {
            _navigationService = navigationService;
        }
        
        public override SelectionComponent Create(IModel model, IMovable movable)
        {
            return new SelectionComponent(model, _navigationService, movable);
        }
    }
}