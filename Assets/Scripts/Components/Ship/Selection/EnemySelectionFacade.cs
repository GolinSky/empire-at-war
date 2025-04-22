using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.BattleService;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionFacade:PlaceholderFactory<IModel, IMovable, EnemySelectionComponent>
    {
        private readonly ISelectionService _selectionService;

        public EnemySelectionFacade(ISelectionService selectionService)
        {
            _selectionService = selectionService;
        }

        public override EnemySelectionComponent Create(IModel model, IMovable movable)
        {
            return new EnemySelectionComponent(model, movable, _selectionService);
        }
    }
}