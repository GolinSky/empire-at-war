using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Movement;
using EmpireAtWar.Models.Weapon;
using LightWeightFramework.Model;
using Utilities.ScriptUtils.Time;
using LightWeightFramework.Components.Components;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent : IComponent
    {
        void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId);
        bool Equal(IHealthModelObserver modelObserver);
        bool Destroyed { get; }
    }

    public class HealthComponent : BaseComponent<HealthModel>, IInitializable, ILateDisposable, IHealthComponent, ITickable
    {
        private readonly ISimpleMoveModelObserver _simpleMoveModelObserver;
        private readonly IModel _rootModel;
        private readonly ITimer _refreshShieldsTimer;
        
        private float _originShieldValue;
        
        public bool Destroyed => Model.IsDestroyed;
        
        public HealthComponent(IModel model) : base(model)
        {
            _simpleMoveModelObserver = model.GetModelObserver<ISimpleMoveModelObserver>();
            _rootModel = model;
            _originShieldValue = Model.Shields;
            _refreshShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);
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

        public void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId)
        {
            bool isMoving = _simpleMoveModelObserver is { IsMoving: true };
            Model.ApplyDamage(damage, weaponType, isMoving, shipUnitId);
        }

        public bool Equal(IHealthModelObserver modelObserver)
        {
            return Model == modelObserver;
        }

        public void Tick()
        {
            if (!Model.IsLostShieldGenerator && Model.Shields < _originShieldValue)
            {
                if (_refreshShieldsTimer.IsComplete)
                {
                    _refreshShieldsTimer.StartTimer();
                    Model.RegenerateShields(Model.ShieldRegenerateValue);
                }
            }
        }
    }
}