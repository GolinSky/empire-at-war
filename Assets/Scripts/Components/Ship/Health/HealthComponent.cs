using EmpireAtWar.Models.Health;
using LightWeightFramework.Model;
using WorkShop.LightWeightFramework.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent:IComponent
    {
        void ApplyDamage(float damage);
    }
    public class HealthComponent : BaseComponent<HealthModel>, IInitializable, IHealthComponent
    {
        public HealthComponent(IModel model) : base(model)
        {
        }

        public void Initialize()
        {
        }

        public void ApplyDamage(float damage)
        {
            Model.ApplyDamage(damage);
        }
    }
}