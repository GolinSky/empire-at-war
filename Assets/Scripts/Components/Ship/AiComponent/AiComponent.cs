using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Models.Health;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Components;

namespace EmpireAtWar.Components.Ship.AiComponent
{
    public class AiComponent: Component
    {
        private readonly IMoveComponent moveComponent;
        private readonly IHealthComponent healthComponent;
        private IHealthModelObserver healthModelObserver;
        
        
        //todo: radar component
        public AiComponent(IModel model, IMoveComponent moveComponent, IHealthComponent healthComponent)
        {
            this.moveComponent = moveComponent;
            this.healthComponent = healthComponent;
            healthModelObserver = model.GetModelObserver<IHealthModelObserver>();
        }
    }
}