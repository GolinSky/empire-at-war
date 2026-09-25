using EmpireAtWar.Components.AttackComponent;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class IonPulseShot : ShotEffect
    {
        [SerializeField] private LineRenderer pulse;
        [SerializeField] private ParticleSystem impactFlash;
        [SerializeField, Min(0.01f)] private float flightDuration = 0.85f;
        [SerializeField, Min(0.01f)] private float pulseLength = 22f;
        [SerializeField, Min(0.01f)] private float flashSize = 32f;
        [SerializeField, Min(0.01f)] private float flashDuration = 0.3f;

        private Transform _target;
        private Vector3 _aimOffset;
        private Vector3 _origin;
        private Vector3 _aimPoint;
        private float _elapsed;
        private bool _flying;
        private bool _flashing;

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            _target = target;
            _aimOffset = aimOffset;
            _origin = muzzle.position;
            _aimPoint = ResolveAimPoint(_origin, target.position + aimOffset);
            _elapsed = 0f;
            _flying = true;
            _flashing = false;
            pulse.widthMultiplier = profile.Size.x;
            pulse.startColor = profile.Color;
            pulse.endColor = profile.Color;
            pulse.positionCount = 2;
            pulse.SetPosition(0, _origin);
            pulse.SetPosition(1, _origin);
            pulse.enabled = true;
            impactFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return flightDuration;
        }

        protected override void Update()
        {
            if (_flying)
            {
                _elapsed += Time.deltaTime;
                if (_target != null)
                    _aimPoint = ResolveAimPoint(_origin, _target.position + _aimOffset);
                float progress = Mathf.Clamp01(_elapsed / flightDuration);
                Vector3 direction = (_aimPoint - _origin).normalized;
                Vector3 head = Vector3.Lerp(_origin, _aimPoint, progress);
                Vector3 halfPulse = direction * (pulseLength * 0.5f);
                pulse.SetPosition(0, head - halfPulse);
                pulse.SetPosition(1, head + halfPulse);
                if (progress >= 1f)
                {
                    _flying = false;
                    _flashing = true;
                    _elapsed = 0f;
                    pulse.enabled = false;
                    impactFlash.transform.position = _aimPoint;
                    impactFlash.Play(true);
                    impactFlash.Emit(new ParticleSystem.EmitParams
                    {
                        position = Vector3.zero,
                        startSize = flashSize,
                        startLifetime = flashDuration,
                        startColor = Color.white
                    }, 1);
                    // Ion supplies its own blue-white flash, including on unshielded hulls.
                }
            }
            else if (_flashing)
            {
                _elapsed += Time.deltaTime;
                _flashing = _elapsed < flashDuration;
            }
            base.Update();
        }

        protected override bool IsVisualComplete() => !_flying && !_flashing;
    }
}
