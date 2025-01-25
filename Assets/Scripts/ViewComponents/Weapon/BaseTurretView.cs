using EmpireAtWar.Models.Weapon;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public abstract class BaseTurretView : MonoBehaviour
    {
        protected readonly ITimer _attackTimer = TimerFactory.ConstructTimer();
        protected readonly ITimer _delayTimer = TimerFactory.ConstructTimer();
        protected IHardPointView _hardPointView;
        protected ProjectileData _projectileData;
        
        public virtual bool IsBusy => !_delayTimer.IsComplete;
        public abstract float Speed { get; }

        public virtual void SetData(ProjectileData projectileData, float attackDistance)
        {
            _projectileData = projectileData;
        }
        public abstract void Attack(IHardPointView hardPointView, float duration);
        public virtual void SetParent(Transform parent){}
        public virtual void ResetParent(){}
    }
}