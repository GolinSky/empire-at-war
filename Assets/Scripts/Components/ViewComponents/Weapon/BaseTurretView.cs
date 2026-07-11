using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Models.Health;
using EmpireAtWar.ViewComponents.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public abstract class BaseTurretView : MonoBehaviour
    {
        protected readonly ITimer _attackTimer = TimerFactory.ConstructTimer();
        protected readonly ITimer _busyTimer = TimerFactory.ConstructTimer();
        protected IHardPointModel _hardPointModel;
        protected ProjectileData _projectileData;
        
        public virtual bool IsBusy => !_busyTimer.IsComplete;

        public virtual void SetData(ProjectileData projectileData, float attackDistance)
        {
            _projectileData = projectileData;
        }
        public abstract void Attack(IHardPointModel hardPointModel, out float duration);
        public virtual void SetParent(Transform parent){}
        public virtual void ResetParent(){}
    }
}