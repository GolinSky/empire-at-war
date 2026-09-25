using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Laser / turbolaser bolt: one particle flying from the muzzle to the aim point.</summary>
    public class BoltShot : ShotEffect
    {
        [SerializeField] private ParticleSystem vfx;

        private Transform _target;
        private Vector3 _aimOffset;
        private Vector3 _start;
        private Vector3 _lastAimPoint;
        private float _startTime;
        private float _travelTime;
        private float _flightDistance;
        private bool _isFlying;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            _target = target;
            _aimOffset = aimOffset;
            _start = muzzle.position;
            _lastAimPoint = ResolveAimPoint(_start, target.position + aimOffset);
            _startTime = Time.time;
            _flightDistance = Vector3.Distance(_start, _lastAimPoint);
            _travelTime = _flightDistance / profile.ProjectileSpeed;
            _isFlying = true;
            transform.SetPositionAndRotation(_start, muzzle.rotation);
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
            main.startSpeed = profile.ProjectileSpeed;
            main.startLifetime = _travelTime;
            ParticleSystem.EmissionModule emission = vfx.emission;
            emission.enabled = false;
            vfx.Play(true);
            vfx.Emit(new ParticleSystem.EmitParams
            {
                position = Vector3.zero,
                velocity = Vector3.forward * profile.ProjectileSpeed
            }, 1);
            return _travelTime;
        }

        protected override void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileTurretUpdate.Auto())
            {
#endif
            if (_isFlying)
            {
                if (_target != null) _lastAimPoint = ResolveAimPoint(_start, _target.position + _aimOffset);
                Vector3 direction = _lastAimPoint - _start;
                float progress = _travelTime > 0f
                    ? Mathf.Clamp01((Time.time - _startTime) / _travelTime) : 1f;
                // Stretch the local flight axis so the particle's lifetime endpoint follows a moving target.
                transform.localScale = new Vector3(1f, 1f,
                    _flightDistance > 0f ? direction.magnitude / _flightDistance : 1f);
                if (direction.sqrMagnitude > 0f)
                    transform.rotation = Quaternion.LookRotation(direction);

                if (progress >= 1f)
                {
                    _isFlying = false;
                    _target = null;
                    vfx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    CompleteImpact(_lastAimPoint, direction);
                }
            }

            base.Update();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        protected override bool IsVisualComplete() => !_isFlying;
    }
}
