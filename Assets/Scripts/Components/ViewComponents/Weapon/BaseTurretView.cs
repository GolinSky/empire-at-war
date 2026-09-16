using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
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
        
        private bool _leaseActive;
        private int _leaseId;
        private bool _retireAfterCompletion;

        public event Action<BaseTurretView, int> EffectCompleted;

        public bool IsBusy => _leaseActive;
        public int LeaseId => _leaseId;

        public virtual void SetData(ProjectileData projectileData, float attackDistance)
        {
            _projectileData = projectileData;
        }
        public abstract void Attack(IHardPointModel hardPointModel, out float duration);
        public virtual void SetParent(Transform parent){}
        public virtual void ResetParent(){}

        protected void BeginLease(float duration)
        {
            if (_leaseActive)
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                throw new InvalidOperationException("Cannot reuse an active projectile effect lease.");
            }

            _leaseId++;
            _leaseActive = true;
            _busyTimer
                .ChangeDelay(duration)
                .StartTimer();
        }

        protected void UpdateLeaseCompletion()
        {
            if (!_leaseActive || !_busyTimer.IsComplete)
            {
                return;
            }

            _leaseActive = false;
            OnLeaseCompleted();
            EffectCompleted?.Invoke(this, _leaseId);

            if (_retireAfterCompletion)
            {
                Destroy(gameObject);
            }
        }

        public void RetireAfterCompletion()
        {
            _retireAfterCompletion = true;
            if (!_leaseActive)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnLeaseCompleted(){}
    }
}
