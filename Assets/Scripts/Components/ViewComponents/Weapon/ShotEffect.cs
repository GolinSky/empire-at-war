using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using UnityEngine;
using Utilities.ScriptUtils.Time;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Visual of one shot. Subclasses only draw; <see cref="ShotEffectPool"/> reuses instances through the lease.
    /// Bolts are the exception: <see cref="BoltShot"/> is authoring data drawn by <see cref="BoltFlightBatch"/>.
    /// </summary>
    public abstract class ShotEffect : MonoBehaviour
    {
        private readonly ITimer _busyTimer = TimerFactory.ConstructTimer();
        private IHealthModelObserver _impactTarget;
        private IncomingMissile _missile;

        [SerializeField] private MuzzleFlashView muzzleFlash;
        private ImpactEffectPresenter _impactPresenter;

        private DamageType _impactDamageType;
        private ImpactSurface _impactSurface;

        private float _impactSize;
        private float _impactDamage;

        private int _leaseId;

        private bool _leaseActive;
        private bool _retireAfterCompletion;
        private bool _impactPending;
        private bool _impactCaptured;

        public event Action<ShotEffect, int> EffectCompleted;

        public event Action<ShotEffect, int> EffectDestroyed;

        public int LeaseId => _leaseId;
        protected bool IsStrikecraftTarget { get; private set; }
        /// <summary>How long after its scheduled time this shot was fired; flying shots start that far ahead.</summary>
        protected float LateSeconds { get; private set; }
        /// <summary>Moves the whole target unit; read it for the unit's travel, free of its turning.</summary>
        protected Transform TargetPivot { get; private set; }
        protected bool HasArmorImpact => _impactTarget != null &&
            _impactPresenter.ResolveSurface(_impactTarget, _impactDamageType) == ImpactSurface.Armor;
        protected bool IsImpactTargetDestroyed => _impactTarget != null && _impactTarget.IsDestroyed;

        public void PrepareImpact(ImpactEffectPresenter presenter, IHealthModelObserver target,
            DamageType damageType, float damage, float size, bool isHit)
        {
            _impactPresenter = presenter;
            IsStrikecraftTarget = target.ShipClass.IsStrikecraft();
            _impactTarget = isHit ? target : null;
            _impactDamageType = damageType;
            _impactSize = size;
            _impactDamage = damage;
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
            _impactPresenter.Play(_impactTarget, _impactSurface, position, direction, _impactSize, _impactDamage);
        }

        protected Vector3 ResolveAimPoint(Vector3 origin, Vector3 target)
        {
            return _impactTarget == null ? target :
                _impactPresenter.GetImpactPosition(_impactTarget, _impactDamageType, origin, target);
        }

        /// <summary>
        /// Flight time from the horizontal distance only, so shots between ships on different heights
        /// fly faster and take as long as shots between ships on the same height.
        /// </summary>
        protected static float GetTravelTime(Vector3 start, Vector3 end, float speed)
        {
            Vector2 horizontal = new Vector2(end.x - start.x, end.z - start.z);
            return horizontal.magnitude / speed;
        }

        /// <summary>
        /// Seconds from firing until the shot lands, read from the prefab so combat can time a shot it does not draw.
        /// A shot fired late starts that far into its flight. Drawn shots use the same rule.
        /// </summary>
        public virtual float GetFlightSeconds(Vector3 muzzle, Vector3 aimPoint, WeaponProfile profile, float lateSeconds)
        {
            float travelTime = GetTravelTime(muzzle, aimPoint, profile.ProjectileSpeed);
            return travelTime - Mathf.Min(lateSeconds, travelTime);
        }

        internal MuzzleFlashView MuzzleFlash => muzzleFlash;

        /// <summary>Stops the visual early if missile defense shoots this shot down.</summary>
        public void TrackInterception(IncomingMissile missile)
        {
            _missile = missile;
            missile.Intercepted += HandleIntercepted;
        }

        /// <summary>Keeps the tracked missile's position on the drawn shot, so point defense aims at the visual.</summary>
        protected void ReportFlightPosition(Vector3 position)
        {
            if (_missile != null) _missile.ReportPosition(position);
        }

        /// <summary>Plays the shot from <paramref name="muzzle"/> towards <paramref name="target"/> + offset.</summary>
        /// <returns>Seconds until the shot reaches its aim point.</returns>
        public float Fire(Transform muzzle, Transform target, Transform targetPivot, Vector3 aimOffset,
            WeaponProfile profile, float lateSeconds = 0f)
        {
            LateSeconds = lateSeconds;
            TargetPivot = targetPivot;
            muzzleFlash.Play(muzzle, ResolveAimPoint(muzzle.position, target.position + aimOffset),
                profile.Color, profile.Size.x);
            float travelTime = Play(muzzle, target, aimOffset, profile);
            BeginLease(travelTime);
            return travelTime;
        }

        protected abstract float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile);

        /// <summary>Seconds after arrival over which the shot's damage is spread; 0 lands it all on arrival.</summary>
        public virtual float DamageDuration => 0f;

        /// <summary>Seconds the lease outlasts the flight: trail fade, beam hold. Sizes pools and in-flight caps.</summary>
        public virtual float PostImpactSeconds => 0f;

        public void ResetForPool()
        {
            muzzleFlash.Stop();
            ResetVisuals();
            enabled = false;
        }

        protected abstract void ResetVisuals();

        protected virtual void OnLeaseCompleted() { }

        protected virtual void OnIntercepted() { }

        protected virtual bool IsVisualComplete() => true;

        protected virtual void Update()
        {
            muzzleFlash.Tick();
            if (!_leaseActive || !_busyTimer.IsComplete || !IsVisualComplete() || muzzleFlash.IsPlaying)
            {
                return;
            }

            _leaseActive = false;
            OnLeaseCompleted();
            _impactTarget = null;
            TargetPivot = null;
            ReleaseMissile();
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

        public void CancelAndDestroy()
        {
            _impactPending = false;
            _leaseActive = false;
            ReleaseMissile();
            ResetForPool();
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
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

        private void HandleIntercepted()
        {
            _impactPending = false;
            OnIntercepted();
        }

        private void ReleaseMissile()
        {
            if (_missile == null) return;
            _missile.Intercepted -= HandleIntercepted;
            _missile = null;
        }

        private void OnDestroy()
        {
            ReleaseMissile();
            EffectDestroyed?.Invoke(this, _leaseId);
        }
    }
}
