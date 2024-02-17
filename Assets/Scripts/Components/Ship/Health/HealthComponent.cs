using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Model;
using UnityEngine;
using WorkShop.LightWeightFramework.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent:IComponent
    {
        void ApplyDamage(float damage, WeaponType weaponType);
        Vector3 Position { get; }
        
        bool Destroyed { get; }
        bool Equal(IModelObserver modelObserver);
    }
    
    public class HealthComponent : BaseComponent<HealthModel>, IInitializable, ILateDisposable, IHealthComponent 
    {
        private readonly IComponentHub componentHub;
        private readonly IMoveModelObserver moveModelObserver;

        private readonly IModel rootModel;
        
        public Vector3 Position => moveModelObserver.CurrentPosition;
        public bool Destroyed => Model.IsDestroyed;
        

        public HealthComponent(IModel model, IComponentHub componentHub) : base(model)
        {
            this.componentHub = componentHub;
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            rootModel = model;
        }

        public void Initialize()
        {
            Model.OnDestroy += Destroy;
            componentHub.Add(this);
        }

        public void LateDispose()
        {
            Model.OnDestroy -= Destroy;
            componentHub.Remove(this);
        }
        
        private void Destroy()
        {
            componentHub.Remove(this);
        }

        public void ApplyDamage(float damage, WeaponType weaponType)
        {
            Model.ApplyDamage(damage, weaponType, moveModelObserver.IsMoving);
        }

        public bool Equal(IModelObserver modelObserver)
        {
            return rootModel == modelObserver;
        }
    }
}