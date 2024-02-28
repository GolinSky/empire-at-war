using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using EmpireAtWar.Services.ComponentHub;
using LightWeightFramework.Model;
using Utilities.ScriptUtils.Time;
using LightWeightFramework.Components.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent : IComponent
    {
        void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId);
        bool Equal(IModelObserver modelObserver);
        bool Destroyed { get; }
    }

    public class HealthComponent : BaseComponent<HealthModel>, IInitializable, ILateDisposable, IHealthComponent, ITickable
    {
        private readonly IMoveModelObserver moveModelObserver;
        private readonly IComponentHub componentHub;
        private readonly IModel rootModel;
        private readonly ITimer refreshShieldsTimer;
        private float originShieldValue;
        public bool Destroyed => Model.IsDestroyed;


        public HealthComponent(IModel model, IComponentHub componentHub) : base(model)
        {
            this.componentHub = componentHub;
            moveModelObserver = model.GetModelObserver<IMoveModelObserver>();
            rootModel = model;
            originShieldValue = Model.Shields;
            refreshShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);
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

        public void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId)
        {
            Model.ApplyDamage(damage, weaponType, moveModelObserver.IsMoving, shipUnitId);
        }

        public bool Equal(IModelObserver modelObserver)
        {
            return rootModel == modelObserver;
        }

        public void Tick()
        {
            if (!Model.IsLostShieldGenerator && Model.Shields < originShieldValue)
            {
                if (refreshShieldsTimer.IsComplete)
                {
                    refreshShieldsTimer.StartTimer();
                    Model.RegenerateShields(Model.ShieldRegenerateValue);
                }
            }
        }
    }
}