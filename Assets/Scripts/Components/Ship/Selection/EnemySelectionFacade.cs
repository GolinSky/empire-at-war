using EmpireAtWar.Services.BattleService;
using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionFacade:PlaceholderFactory<IModel, EnemySelectionComponent>
    {
        private readonly IBattleService battleService;

        public EnemySelectionFacade(IBattleService battleService)
        {
            this.battleService = battleService;
        }

        public override EnemySelectionComponent Create(IModel model)
        {
            return new EnemySelectionComponent(model, battleService);
        }
    }
}