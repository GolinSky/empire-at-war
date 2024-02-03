using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Services.Battle;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionFacade:PlaceholderFactory<IModel, IHealthComponent, EnemySelectionComponent>
    {
        private readonly IBattleService battleService;

        public EnemySelectionFacade(IBattleService battleService)
        {
            this.battleService = battleService;
        }

        public override EnemySelectionComponent Create(IModel model, IHealthComponent healthComponent)
        {
            return new EnemySelectionComponent(model, battleService, healthComponent);
        }
    }
}