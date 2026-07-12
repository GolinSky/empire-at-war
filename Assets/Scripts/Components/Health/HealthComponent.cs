using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Movement;
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
        bool Destroyed { get; }
        IHealthModelObserver HealthModelObserver { get; }
    }

    public class HealthComponent : MonoComponent<HealthModel>, IInitializable, ILateDisposable, IHealthComponent, ITickable
    {
        [field:SerializeField] public List<HardPointView> ShipUnits { get; set; }

        private IDefaultMoveModelObserver _defaultMoveModelObserver;
        private ITimer _refreshShieldsTimer;
        
        private float _originShieldValue;
        
        public bool Destroyed => Model.IsDestroyed;
        public IHealthModelObserver HealthModelObserver => Model;
        
        [Inject]
        private void Construct(IModel model)
        {
            SetModel(model.GetModel<HealthModel>());
            _defaultMoveModelObserver = model.GetModelObserver<IDefaultMoveModelObserver>();
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
            bool isMoving = _defaultMoveModelObserver is { IsMoving: true };
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
                    //Model.RegenerateShields(Model.ShieldRegenerateValue);
                }
            }
        }
    }
}
