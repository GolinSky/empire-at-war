using LightWeightFramework.Model;
using Zenject;

namespace EmpireAtWar.Components.Ship.Selection
{
    public class EnemySelectionFacade:PlaceholderFactory<IModel, EnemySelectionComponent>
    {

        public EnemySelectionFacade()
        {
        }

        public override EnemySelectionComponent Create(IModel model)
        {
            return new EnemySelectionComponent(model);
        }
    }
}