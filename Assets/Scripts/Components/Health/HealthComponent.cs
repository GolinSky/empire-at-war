using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Mvc;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health
{
    public interface IHealthComponent : IComponent
    {
        void ApplyDamage(float damage, WeaponType weaponType, int shipUnitId);
        bool Equal(IHealthModelObserver modelObserver);
        void SetMovementState(bool isMoving);
        bool Destroyed { get; }
        IHealthModelObserver HealthModelObserver { get; }
    }

    public class HealthComponent : MonoComponent<HealthModel>, IInitializable, ILateDisposable, IHealthComponent, ITickable
    {
        [field:SerializeField] public List<HardPointView> ShipUnits { get; set; }

        private ITimer _refreshShieldsTimer;
        private bool _isMoving;
        
        private float _originShieldValue;
        
        public bool Destroyed => Model.IsDestroyed;
        public IHealthModelObserver HealthModelObserver => Model;

        [Inject]
        private void Construct(HealthModel model)
        {
            SetModel(model);
        }

        public void Initialize()
        {
            Model.InjectDependency(ShipUnits);
            _originShieldValue = Model.Shields;
            _refreshShieldsTimer = TimerFactory.ConstructTimer(Model.ShieldRegenerateDelay);
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
            Model.ApplyDamage(damage, weaponType, _isMoving, shipUnitId);
        }

        public void SetMovementState(bool isMoving)
        {
            _isMoving = isMoving;
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
                    //Model.RegenerateShields(Model.ShieldRegenerateValue);
                }
            }
        }
    }
}
