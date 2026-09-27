using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Laser / turbolaser bolt: flies in a straight line to the aim point captured at fire time,
    /// arriving exactly when the scheduled damage lands. The particle stays at the transform origin.
    /// </summary>
    public class BoltShot : ShotEffect
    {
        // Stretched billboards take their axis from particle velocity; keep it tiny so the particle
        // never drifts off the flying transform.
        private const float STRETCH_AXIS_SPEED = 0.001f;
        private const float LIFETIME_MARGIN = 1f;

        [SerializeField] private ParticleSystem vfx;
        [Tooltip("Multiplies the weapon profile size for the bolt particle only (not the muzzle flash).")]
        [SerializeField] private float sizeScale = 3f;

        private Vector3 _lastAimPoint;
        private float _arrivalTime;
        private bool _isFlying;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            Vector3 start = muzzle.position;
            _lastAimPoint = ResolveAimPoint(start, target.position + aimOffset);
            float travelTime = Vector3.Distance(start, _lastAimPoint) / profile.ProjectileSpeed;
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
            Vector3 position = transform.position;

            float remaining = _arrivalTime - Time.time;
            if (remaining <= 0f)
            {
                Vector3 impactDirection = _lastAimPoint - position;
                transform.position = _lastAimPoint;
                _isFlying = false;
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                CompleteImpact(_lastAimPoint, impactDirection.sqrMagnitude > 0f ? impactDirection : transform.forward);
                return;
            }

            // Cover this frame's share of the remaining gap so the bolt lands on schedule.
            float step = Time.deltaTime / (remaining + Time.deltaTime);
            transform.position = Vector3.Lerp(position, _lastAimPoint, step);
        }

        protected override bool IsVisualComplete() => !_isFlying;
    }
}
