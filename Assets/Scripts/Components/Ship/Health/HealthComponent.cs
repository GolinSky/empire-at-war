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
        
        bool Destroyed { get; }
    }
    
    public class HealthComponent : BaseComponent<HealthModel>, IInitializable, ILateDisposable, IHealthComponent 
    {
        private readonly IMoveModelObserver moveModelObserver;
        
        public Vector3 Position => moveModelObserver.Position;
        public bool Destroyed => Model.IsDestroyed;

        public HealthComponent(IModel model) : base(model)
        {
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
        }

        public void Initialize()
        {
            Model.OnDestroy += Destroy;
        }

        public void LateDispose()
        {
            Model.OnDestroy -= Destroy;
        }
        
        private void Destroy()
        {
            
        }

        public void ApplyDamage(float damage)
        {
            Model.ApplyDamage(damage);
        }
    }
}