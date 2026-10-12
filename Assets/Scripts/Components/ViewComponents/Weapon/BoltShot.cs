using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Laser / turbolaser bolt: tracks ships but keeps its launch aim against strikecraft,
    /// arriving exactly when the scheduled damage lands. The particle stays at the transform origin.
    /// The bolt always sits on the line from the muzzle to the predicted impact point, so steering
    /// is spread over the whole flight instead of swinging at the end. <see cref="BoltFlightBatch"/> flies it.
    /// </summary>
    public class BoltShot : ShotEffect
    {
        // Stretched billboards take their axis from particle velocity; keep it tiny so the particle
        // never drifts off the flying transform.
        private const float STRETCH_AXIS_SPEED = 0.001f;
        private const float LIFETIME_MARGIN = 1f;
        private const float PARTICLE_BURST_TIME = 0.001f;
        // How fast the estimated target velocity follows the measured one, per second.
        private const float VELOCITY_SMOOTHING_RATE = 15f;
        // Velocity is measured only this long after launch, then the lead is locked. A lead that keeps
        // updating multiplies every target manoeuvre by the remaining flight time and jerks the bolt.
        private const float LEAD_SETTLE_TIME = 0.15f;
        // Seconds between shield-surface clips of a tracked aim point. In between, the clipped point moves with
        // the target; the landing frame always clips again, so impacts sit exactly on the shield.
        private const float SURFACE_REFRESH_INTERVAL = 0.1f;

        [SerializeField] private ParticleSystem vfx;
        private Transform _transform;
        private Transform _target;

        private Vector3 _aimOffset;
        private Vector3 _start;
        private Vector3 _lastAimPoint;
        private Vector3 _flightAimPoint;
        private Vector3 _lastPivotPosition;
        private Vector3 _targetVelocity;
        // Resolved aim point minus the raw target point, from the last shield clip.
        private Vector3 _surfaceOffset;

        [Tooltip("Multiplies the weapon profile size for the bolt particle only (not the muzzle flash).")]
        [SerializeField] private float sizeScale = 3f;
        private float _arrivalTime;
        private float _travelTime;
        private float _progress;
        private float _nextSurfaceRefresh;

        private bool _isFlying;

        /// <summary>Set by <see cref="BoltFlightBatch"/> while it lists this bolt.</summary>
        internal bool IsInFlightBatch { get; set; }

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            _transform = transform;
            Vector3 start = muzzle.position;
            _target = IsStrikecraftTarget ? null : target;
            _aimOffset = aimOffset;
            _start = start;
            Vector3 targetPoint = target.position + aimOffset;
            _lastAimPoint = ResolveAimPoint(start, targetPoint);
            _surfaceOffset = _lastAimPoint - targetPoint;
            // Random phase: a salvo's bolts do not all clip again on the same frame.
            _nextSurfaceRefresh = Time.time + SURFACE_REFRESH_INTERVAL * Random.value;
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
            main.startSpeed = STRETCH_AXIS_SPEED;
            main.startLifetime = travelTime + LIFETIME_MARGIN;
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = vfx.sizeOverLifetime;
            sizeOverLifetime.enabled = false;
            ParticleSystem.EmissionModule emission = vfx.emission;
            emission.enabled = true;
            // Bolt prefabs contain one static burst. Fly moves the local-space particles;
            // simulate just the initial emission, then leave simulation paused until impact.
            vfx.Simulate(PARTICLE_BURST_TIME, true, true, false);
            _progress = 0f;
            if (!IsInFlightBatch) BoltFlights.Add(this);
            return travelTime;
        }

        /// <summary>
        /// Called by <see cref="BoltFlightBatch"/> each frame: tracks the target and lands the bolt on arrival.
        /// </summary>
        /// <returns>False once the bolt no longer flies (landed or returned to its pool).</returns>
        internal bool Advance(float now, float deltaTime)
        {
            if (!_isFlying) return false;
            float remaining = Mathf.Max(_arrivalTime - now, 0f);
            if (_target != null) TrackTarget(now, remaining, deltaTime);

            if (remaining <= 0f)
            {
                Vector3 impactDirection = _lastAimPoint - _start;
                _transform.position = _lastAimPoint;
                _isFlying = false;
                _target = null;
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                CompleteImpact(_lastAimPoint, impactDirection.sqrMagnitude > 0f ? impactDirection : _transform.forward);
                return false;
            }

            // Anchored to the muzzle: any aim correction moves the bolt by correction * progress,
            // so the path bends gently over the whole flight and the heading never snaps.
            _progress = 1f - remaining / _travelTime;
            return true;
        }

        internal BoltPose Pose => new BoltPose
        {
            Start = _start,
            Path = _flightAimPoint - _start,
            Progress = _progress
        };

        // Leads the target by the ship's travel velocity measured at launch over the remaining flight time.
        // Velocity comes from the ship pivot, not the hardpoint: a ship turning to face a new target swings
        // its hardpoints in an arc, and extrapolating that arc as straight-line motion throws the bolt off.
        // Hardpoint swing is still followed through the tracked aim point.
        private void TrackTarget(float now, float remaining, float deltaTime)
        {
            // The lead locks after the settle time, so the pivot is read only while it is measured.
            if (deltaTime > 0f && _travelTime - remaining < LEAD_SETTLE_TIME)
            {
                Vector3 pivotPosition = TargetPivot.position;
                Vector3 measuredVelocity = (pivotPosition - _lastPivotPosition) / deltaTime;
                float blend = 1f - Mathf.Exp(-VELOCITY_SMOOTHING_RATE * deltaTime);
                _targetVelocity = Vector3.Lerp(_targetVelocity, measuredVelocity, blend);
                _lastPivotPosition = pivotPosition;
            }

            Vector3 targetPoint = _target.position + _aimOffset;
            if (remaining <= 0f || now >= _nextSurfaceRefresh)
            {
                _surfaceOffset = ResolveAimPoint(_start, targetPoint) - targetPoint;
                _nextSurfaceRefresh = now + SURFACE_REFRESH_INTERVAL;
            }

            _lastAimPoint = targetPoint + _surfaceOffset;
            _flightAimPoint = _lastAimPoint + _targetVelocity * remaining;
        }

        // A bolt returned to its pool mid-flight stops; the flight batch drops it on its next pass.
        protected override void ResetVisuals()
        {
            _isFlying = false;
            vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override bool IsVisualComplete() => !_isFlying;
    }
}
