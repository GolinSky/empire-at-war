using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Torpedo / concussion missile: a glowing head with a particle trail that arcs into the target.</summary>
    public class MissileShot : ShotEffect
    {
        [SerializeField] private ParticleSystem head;
        [SerializeField] private ParticleSystem trail;
        [Tooltip("Peak height of the flight arc, relative to the flight distance.")]
        [SerializeField] private float arcHeight = 0.1f;

        private Transform _target;
        private Vector3 _aimOffset;
        private Vector3 _start;
        private Vector3 _lastAimPoint;
        private Vector3 _arcNormal;
        private float _startTime;
        private float _travelTime;
        private bool _isFlying;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            _target = target;
            _aimOffset = aimOffset;
            _start = muzzle.position;
            _lastAimPoint = target.position + aimOffset;
            _arcNormal = Random.onUnitSphere;
            _startTime = Time.time;
            _travelTime = Vector3.Distance(_start, _lastAimPoint) / profile.ProjectileSpeed;
            _isFlying = true;
            transform.position = _start;

            ParticleSystem.MainModule headMain = head.main;
            headMain.startColor = profile.Color;
            headMain.startSize = profile.Size.x;
            ParticleSystem.MainModule trailMain = trail.main;
            trailMain.startColor = profile.Color;
            trailMain.startSize = profile.Size.x * 0.6f;
            head.Play(true);
            trail.Play(true);
            return _travelTime;
        }

        protected override void Update()
        {
            if (_isFlying)
            {
                Fly();
            }

            base.Update();
        }

        private void Fly()
        {
            if (_target != null) _lastAimPoint = _target.position + _aimOffset;

            Vector3 previousPosition = transform.position;
            float progress = _travelTime > 0f
                ? Mathf.Clamp01((Time.time - _startTime) / _travelTime) : 1f;
            float distance = Vector3.Distance(_start, _lastAimPoint);
            Vector3 arc = _arcNormal * (Mathf.Sin(progress * Mathf.PI) * arcHeight * distance);
            transform.position = Vector3.Lerp(_start, _lastAimPoint, progress) + arc;

            if (progress >= 1f)
            {
                transform.position = _lastAimPoint;
                CompleteImpact(_lastAimPoint, _lastAimPoint - previousPosition);
                _isFlying = false;
                _target = null;
                head.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        protected override bool IsVisualComplete() => !_isFlying && !trail.IsAlive(true);
    }
}
