using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Laser / turbolaser bolt: tracks ships but keeps its launch aim against strikecraft,
    /// arriving exactly when the scheduled damage lands. The particle stays at the transform origin.
    /// The bolt always sits on the line from the muzzle to the predicted impact point, so steering
    /// is spread over the whole flight instead of swinging at the end.
    /// </summary>
    public class BoltShot : ShotEffect
    {
        // Stretched billboards take their axis from particle velocity; keep it tiny so the particle
        // never drifts off the flying transform.
        private const float STRETCH_AXIS_SPEED = 0.001f;
        private const float LIFETIME_MARGIN = 1f;
        // How fast the estimated target velocity follows the measured one, per second.
        private const float VELOCITY_SMOOTHING_RATE = 15f;
        // Velocity is measured only this long after launch, then the lead is locked. A lead that keeps
        // updating multiplies every target manoeuvre by the remaining flight time and jerks the bolt.
        private const float LEAD_SETTLE_TIME = 0.15f;

        [SerializeField] private ParticleSystem vfx;
        private Transform _target;

        private Vector3 _aimOffset;
        private Vector3 _start;
        private Vector3 _lastAimPoint;
        private Vector3 _flightAimPoint;
        private Vector3 _lastPivotPosition;
        private Vector3 _targetVelocity;

        [Tooltip("Multiplies the weapon profile size for the bolt particle only (not the muzzle flash).")]
        [SerializeField] private float sizeScale = 3f;
        private float _arrivalTime;
        private float _travelTime;

        private bool _isFlying;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            Vector3 start = muzzle.position;
            _target = IsStrikecraftTarget ? null : target;
            _aimOffset = aimOffset;
            _start = start;
            _lastAimPoint = ResolveAimPoint(start, target.position + aimOffset);
            _flightAimPoint = _lastAimPoint;
            _lastPivotPosition = TargetPivot.position;
            _targetVelocity = Vector3.zero;
            float travelTime = GetTravelTime(start, _lastAimPoint, profile.ProjectileSpeed);
            _travelTime = travelTime;
            _arrivalTime = Time.time + travelTime;
            _isFlying = true;
            Vector3 direction = _lastAimPoint - start;
            transform.SetPositionAndRotation(start, direction.sqrMagnitude > 0f
                ? Quaternion.LookRotation(direction) : muzzle.rotation);
            transform.localScale = Vector3.one;

            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = vfx.main;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
            main.startColor = profile.Color;
            main.startSize3D = true;
            main.startSizeXMultiplier = profile.Size.x * sizeScale;
            main.startSizeYMultiplier = profile.Size.y * sizeScale;
            main.startSizeZMultiplier = profile.Size.z * sizeScale;
            main.startSpeed = 0f;
            main.startLifetime = travelTime + LIFETIME_MARGIN;
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = vfx.sizeOverLifetime;
            sizeOverLifetime.enabled = false;
            ParticleSystem.EmissionModule emission = vfx.emission;
            emission.enabled = false;
            vfx.Play(true);
            vfx.Emit(new ParticleSystem.EmitParams
            {
                position = Vector3.zero,
                velocity = Vector3.forward * STRETCH_AXIS_SPEED
            }, 1);
            return travelTime;
        }

        protected override void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileTurretUpdate.Auto())
            {
#endif
            if (_isFlying)
            {
                Fly();
            }

            base.Update();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private void Fly()
        {
            float remaining = Mathf.Max(_arrivalTime - Time.time, 0f);
            if (_target != null)
            {
                TrackTarget(remaining);
            }

            if (remaining <= 0f)
            {
                Vector3 impactDirection = _lastAimPoint - _start;
                transform.position = _lastAimPoint;
                _isFlying = false;
                _target = null;
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                CompleteImpact(_lastAimPoint, impactDirection.sqrMagnitude > 0f ? impactDirection : transform.forward);
                return;
            }

            // Anchored to the muzzle: any aim correction moves the bolt by correction * progress,
            // so the path bends gently over the whole flight and the heading never snaps.
            float progress = 1f - remaining / _travelTime;
            Vector3 path = _flightAimPoint - _start;
            transform.position = _start + path * progress;
            if (path.sqrMagnitude > 0f) transform.rotation = Quaternion.LookRotation(path);
        }

        // Leads the target by the ship's travel velocity measured at launch over the remaining flight time.
        // Velocity comes from the ship pivot, not the hardpoint: a ship turning to face a new target swings
        // its hardpoints in an arc, and extrapolating that arc as straight-line motion throws the bolt off.
        // Hardpoint swing is still followed through the tracked aim point.
        private void TrackTarget(float remaining)
        {
            Vector3 pivotPosition = TargetPivot.position;
            float deltaTime = Time.deltaTime;
            if (deltaTime > 0f && _travelTime - remaining < LEAD_SETTLE_TIME)
            {
                Vector3 measuredVelocity = (pivotPosition - _lastPivotPosition) / deltaTime;
                float blend = 1f - Mathf.Exp(-VELOCITY_SMOOTHING_RATE * deltaTime);
                _targetVelocity = Vector3.Lerp(_targetVelocity, measuredVelocity, blend);
            }

            _lastPivotPosition = pivotPosition;
            _lastAimPoint = ResolveAimPoint(_start, _target.position + _aimOffset);
            _flightAimPoint = _lastAimPoint + _targetVelocity * remaining;
        }

        protected override bool IsVisualComplete() => !_isFlying;
    }
}
