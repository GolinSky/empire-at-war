using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Laser / turbolaser bolt: the transform flies from the muzzle and homes onto the moving aim point,
    /// arriving exactly when the scheduled damage lands. The particle stays at the transform origin.
    /// </summary>
    public class BoltShot : ShotEffect
    {
        // Stretched billboards take their axis from particle velocity; keep it tiny so the particle
        // never drifts off the flying transform.
        private const float STRETCH_AXIS_SPEED = 0.001f;
        private const float LIFETIME_MARGIN = 1f;

        [SerializeField] private ParticleSystem vfx;

        private Transform _target;
        private Vector3 _aimOffset;
        private Vector3 _lastAimPoint;
        private float _arrivalTime;
        private bool _isFlying;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            _target = target;
            _aimOffset = aimOffset;
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
            main.startSizeXMultiplier = profile.Size.x;
            main.startSizeYMultiplier = profile.Size.y;
            main.startSizeZMultiplier = profile.Size.z;
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
            if (_target != null) _lastAimPoint = ResolveAimPoint(position, _target.position + _aimOffset);

            float remaining = _arrivalTime - Time.time;
            if (remaining <= 0f)
            {
                Vector3 impactDirection = _lastAimPoint - position;
                transform.position = _lastAimPoint;
                _isFlying = false;
                _target = null;
                vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                CompleteImpact(_lastAimPoint, impactDirection.sqrMagnitude > 0f ? impactDirection : transform.forward);
                return;
            }

            // Cover this frame's share of the remaining gap so the bolt curves onto a moving target
            // and still lands on schedule.
            float step = Time.deltaTime / (remaining + Time.deltaTime);
            Vector3 next = Vector3.Lerp(position, _lastAimPoint, step);
            Vector3 heading = _lastAimPoint - next;
            transform.position = next;
            if (heading.sqrMagnitude > 0f)
                transform.rotation = Quaternion.LookRotation(heading);
        }

        protected override bool IsVisualComplete() => !_isFlying;
    }
}
