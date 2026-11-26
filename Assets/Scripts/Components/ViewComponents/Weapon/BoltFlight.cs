using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// One laser / turbolaser bolt in flight, with no GameObject. Tracks ships but keeps its launch aim against
    /// strikecraft, and lands exactly when the scheduled damage does. The bolt sits on the line from its anchor (the
    /// muzzle, then its position at each shield clip) to the predicted impact point, so steering is spread over the
    /// flight instead of swinging at the end. <see cref="BoltFlightBatch"/> flies and draws it.
    /// </summary>
    internal sealed class BoltFlight
    {
        // How fast the estimated target velocity follows the measured one, per second.
        private const float VELOCITY_SMOOTHING_RATE = 15f;
        // Velocity is measured only this long after launch, then the lead is locked. A lead that keeps
        // updating multiplies every target manoeuvre by the remaining flight time and jerks the bolt.
        private const float LEAD_SETTLE_TIME = 0.15f;
        // Seconds between shield-surface clips of a tracked aim point. In between, the clipped point moves with
        // the target; the landing frame always clips again, so impacts sit exactly on the shield.
        private const float SURFACE_REFRESH_INTERVAL = 0.1f;

        private readonly Action _onIntercepted;

        private Transform _target;
        private Transform _targetPivot;
        private IHealthModelObserver _impactTarget;
        private ImpactEffectPresenter _impacts;
        private IncomingMissile _missile;
        private DamageType _damageType;
        private float _impactSize;
        private float _impactDamage;
        private bool _impactPending;

        private Vector3 _aimOffset;
        private Vector3 _start;
        // Where the drawn flight line starts: the muzzle, then the bolt's position at each shield clip.
        private Vector3 _anchor;
        private Vector3 _lastAimPoint;
        private Vector3 _flightAimPoint;
        private Vector3 _lastPivotPosition;
        private Vector3 _targetVelocity;
        // Resolved aim point minus the raw target point, from the last shield clip.
        private Vector3 _surfaceOffset;

        private float _arrivalTime;
        private float _travelTime;
        // Flight time left when the bolt was last anchored.
        private float _anchorDuration;
        private float _progress;
        private float _nextSurfaceRefresh;

        internal BoltFlight()
        {
            _onIntercepted = HandleIntercepted;
        }

        internal bool IsFlying { get; private set; }
        internal float3 Size { get; private set; }
        internal Color32 Color { get; private set; }

        internal BoltPose Pose => new BoltPose
        {
            Start = _anchor,
            Path = _flightAimPoint - _anchor,
            Progress = _progress
        };

        /// <returns>Seconds until the bolt lands.</returns>
        internal float Launch(BoltShot prefab, WeaponProfile profile, Vector3 muzzle, Transform target,
            Transform targetPivot, Vector3 aimOffset, IHealthModelObserver targetHealth, ImpactEffectPresenter impacts,
            float impactDamage, float impactSize, bool isHit, IncomingMissile missile, float lateSeconds)
        {
            _impacts = impacts;
            _impactTarget = isHit ? targetHealth : null;
            _damageType = profile.DamageType;
            _impactSize = impactSize;
            _impactDamage = impactDamage;
            _impactPending = isHit;
            _missile = missile;
            if (missile != null) missile.Intercepted += _onIntercepted;

            _target = targetHealth.ShipClass.IsStrikecraft() ? null : target;
            _targetPivot = targetPivot;
            _aimOffset = aimOffset;
            _start = muzzle;
            Vector3 targetPoint = target.position + aimOffset;
            _lastAimPoint = ResolveAimPoint(muzzle, targetPoint);
            _surfaceOffset = _lastAimPoint - targetPoint;
            // Random phase: a salvo's bolts do not all clip again on the same frame.
            _nextSurfaceRefresh = Time.time + SURFACE_REFRESH_INTERVAL * Random.value;
            _flightAimPoint = _lastAimPoint;
            _lastPivotPosition = targetPivot.position;
            _targetVelocity = Vector3.zero;
            _travelTime = prefab.GetFlightSeconds(muzzle, _lastAimPoint, profile, 0f);
            float remaining = prefab.GetFlightSeconds(muzzle, _lastAimPoint, profile, lateSeconds);
            _anchor = muzzle;
            _anchorDuration = _travelTime;
            _arrivalTime = Time.time + remaining;
            _progress = _travelTime > 0f ? (_travelTime - remaining) / _travelTime : 0f;
            Size = profile.Size * prefab.SizeScale;
            Color = profile.Color;
            IsFlying = true;
            return remaining;
        }

        /// <summary>Tracks the target and lands the bolt on arrival.</summary>
        /// <returns>False once the bolt no longer flies.</returns>
        internal bool Advance(float now, float deltaTime)
        {
            if (!IsFlying) return false;
            float remaining = Mathf.Max(_arrivalTime - now, 0f);
            if (_target != null && IsImpactTargetDestroyed)
            {
                _target = null;
                _lastAimPoint = _flightAimPoint;
            }
            if (_target != null) TrackTarget(now, remaining, deltaTime);

            if (remaining <= 0f)
            {
                Vector3 impactDirection = _lastAimPoint - _start;
                CompleteImpact(_lastAimPoint, impactDirection.sqrMagnitude > 0f ? impactDirection : Vector3.forward);
                Stop();
                return false;
            }

            // Between shield clips any aim correction moves the bolt by correction * progress,
            // so the path bends gently instead of the heading snapping.
            _progress = 1f - remaining / _anchorDuration;
            return true;
        }

        /// <summary>Drops the bolt without an impact, e.g. when the battle resets.</summary>
        internal void Stop()
        {
            IsFlying = false;
            _target = null;
            _targetPivot = null;
            _impactTarget = null;
            _impactPending = false;
            if (_missile != null) _missile.Intercepted -= _onIntercepted;
            _missile = null;
        }

        private bool IsImpactTargetDestroyed => _impactTarget != null && _impactTarget.IsDestroyed;

        // Leads the target by the ship's travel velocity measured at launch over the remaining flight time.
        // Velocity comes from the ship pivot, not the hardpoint: a ship turning to face a new target swings
        // its hardpoints in an arc, and extrapolating that arc as straight-line motion throws the bolt off.
        // Hardpoint swing is still followed through the tracked aim point.
        private void TrackTarget(float now, float remaining, float deltaTime)
        {
            // The lead locks after the settle time, so the pivot is read only while it is measured.
            if (deltaTime > 0f && _travelTime - remaining < LEAD_SETTLE_TIME)
            {
                Vector3 pivotPosition = _targetPivot.position;
                Vector3 measuredVelocity = (pivotPosition - _lastPivotPosition) / deltaTime;
                float blend = 1f - Mathf.Exp(-VELOCITY_SMOOTHING_RATE * deltaTime);
                _targetVelocity = Vector3.Lerp(_targetVelocity, measuredVelocity, blend);
                _lastPivotPosition = pivotPosition;
            }

            Vector3 targetPoint = _target.position + _aimOffset;
            if (remaining <= 0f || now >= _nextSurfaceRefresh)
            {
                // A clip can jump the aim point by metres (shield collapsing mid-flight). Re-anchor at the bolt's
                // current position first, so the jump bends the rest of the flight instead of teleporting the bolt.
                if (remaining > 0f)
                {
                    _anchor += (_flightAimPoint - _anchor) * (1f - remaining / _anchorDuration);
                    _anchorDuration = remaining;
                }
                _surfaceOffset = ResolveAimPoint(_anchor, targetPoint) - targetPoint;
                _nextSurfaceRefresh = now + SURFACE_REFRESH_INTERVAL;
            }

            _lastAimPoint = targetPoint + _surfaceOffset;
            _flightAimPoint = _lastAimPoint + _targetVelocity * remaining;
        }

        private Vector3 ResolveAimPoint(Vector3 origin, Vector3 target) =>
            _impactTarget == null ? target : _impacts.GetImpactPosition(_impactTarget, _damageType, origin, target);

        private void CompleteImpact(Vector3 position, Vector3 direction)
        {
            if (!_impactPending) return;
            _impactPending = false;
            _impacts.Play(_impactTarget, _impacts.ResolveSurface(_impactTarget, _damageType), position, direction,
                _impactSize, _impactDamage);
        }

        // An intercepted bolt keeps flying but no longer plays its impact.
        private void HandleIntercepted() => _impactPending = false;
    }
}
