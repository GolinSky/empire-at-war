using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class SelectionFacade : PlaceholderFactory<IModel,IMovable, PlayerSelectionComponent>
    {
        private readonly ISelectionService _selectionService;

        public SelectionFacade(ISelectionService selectionService)
        {
            _selectionService = selectionService;
        }
        
        public override PlayerSelectionComponent Create(IModel model, IMovable movable)
        {
            return new PlayerSelectionComponent(model, _selectionService, movable);
        }
    }
}