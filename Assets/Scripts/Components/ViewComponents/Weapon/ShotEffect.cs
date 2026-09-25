using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Visual of one shot. Subclasses only draw; <see cref="ShotEffectPool"/> reuses instances through the lease.
    /// </summary>
    public abstract class ShotEffect : MonoBehaviour
    {
        private readonly ITimer _busyTimer = TimerFactory.ConstructTimer();
        private bool _leaseActive;
        private int _leaseId;
        private bool _retireAfterCompletion;
        private ImpactEffectPresenter _impactPresenter;
        private IHealthModelObserver _impactTarget;
        private DamageType _impactDamageType;
        private ImpactSurface _impactSurface;
        private float _impactSize;
        private bool _impactPending;
        private bool _impactCaptured;

        public event Action<ShotEffect, int> EffectCompleted;
        public event Action<ShotEffect, int> EffectDestroyed;

        public int LeaseId => _leaseId;
        protected bool HasImpact => _impactPending && _impactSurface != ImpactSurface.None;

        public void PrepareImpact(ImpactEffectPresenter presenter, IHealthModelObserver target,
            DamageType damageType, float size, bool isHit)
        {
            _impactPresenter = presenter;
            _impactTarget = isHit ? target : null;
            _impactDamageType = damageType;
            _impactSize = size;
            _impactPending = isHit;
            _impactCaptured = false;
            _impactSurface = ImpactSurface.None;
        }

        protected void CaptureImpact()
        {
            if (!_impactPending || _impactCaptured) return;
            _impactSurface = _impactPresenter.ResolveSurface(_impactTarget, _impactDamageType);
            _impactCaptured = true;
        }

        protected void CompleteImpact(Vector3 position, Vector3 direction)
        {
            if (!_impactPending) return;
            CaptureImpact();
            _impactPending = false;
            _impactTarget = null;
            _impactPresenter.Play(_impactSurface, position, direction, _impactSize);
        }

        /// <summary>Plays the shot from <paramref name="muzzle"/> towards <paramref name="target"/> + offset.</summary>
        /// <returns>Seconds until the shot reaches its aim point.</returns>
        public float Fire(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            float travelTime = Play(muzzle, target, aimOffset, profile);
            BeginLease(travelTime);
            return travelTime;
        }

        protected abstract float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile);

        protected virtual void OnLeaseCompleted() { }

        protected virtual bool IsVisualComplete() => true;

        protected virtual void Update()
        {
            if (!_leaseActive || !_busyTimer.IsComplete || !IsVisualComplete())
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

        private void BeginLease(float duration)
        {
            if (_leaseActive)
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                throw new InvalidOperationException("Cannot reuse an active shot effect lease.");
            }

            _leaseId++;
            _leaseActive = true;
            _busyTimer
                .ChangeDelay(duration)
                .StartTimer();
        }

        private void OnDestroy()
        {
            EffectDestroyed?.Invoke(this, _leaseId);
        }
    }
}
