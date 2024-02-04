using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent:IComponent
    {
        void ApplyDamage(float damage);
        Vector3 Position { get; }
    }
    public class HealthComponent : BaseComponent<HealthModel>, IInitializable, IHealthComponent
    {
        private readonly IMoveModelObserver moveModelObserver;

        public HealthComponent(IModel model) : base(model)
        {
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
        }

        public void Initialize()
        {
        }

        public void ApplyDamage(float damage)
        {
            Model.ApplyDamage(damage);
        }

        public Vector3 Position => moveModelObserver.Position;
    }
}