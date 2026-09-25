using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Beam that grows from the muzzle to the target, then holds. Used by beam hardpoints and the Proton Beam ability.</summary>
    public class BeamShot : ShotEffect
    {
        [SerializeField] private LineRenderer beam;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private float growthDuration = 0.3f;
        [SerializeField] private float holdDuration = 0.5f;

        private Transform _muzzle;
        private Transform _target;
        private Vector3 _aimOffset;
        private Vector3 _lastOrigin;
        private Vector3 _lastAimPoint;
        private float _startTime;
        private float _currentHoldDuration;
        private bool _isHitPlaying;

        /// <summary>Plays the beam outside the shot pool, e.g. from an ability.</summary>
        public void PlayBeam(Transform muzzle, Transform target, float holdDuration)
        {
            // Ability damage is resolved by its presenter immediately after this call.
            CaptureImpact();
            StartBeam(muzzle, target, Vector3.zero, holdDuration);
        }

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            beam.startColor = profile.Color;
            beam.endColor = profile.Color;
            beam.widthMultiplier = profile.Size.x;
            StartBeam(muzzle, target, aimOffset, holdDuration);
            return growthDuration;
        }

        private void StartBeam(Transform muzzle, Transform target, Vector3 aimOffset, float hold)
        {
            _muzzle = muzzle;
            _target = target;
            _aimOffset = aimOffset;
            _currentHoldDuration = hold;
            _startTime = Time.time;
            _lastOrigin = muzzle.position;
            _lastAimPoint = ResolveAimPoint(_lastOrigin, target.position + aimOffset);
            beam.positionCount = 2;
            beam.SetPosition(0, _lastOrigin);
            beam.SetPosition(1, _lastOrigin);
            beam.enabled = true;
            _isHitPlaying = false;
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileLaserUpdate.Auto())
            {
#endif
            if (beam.enabled)
            {
                UpdateBeam();
            }

            base.Update();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private void UpdateBeam()
        {
            // Keep the last known points when the shooter or the target is destroyed mid-beam.
            if (_muzzle != null) _lastOrigin = _muzzle.position;
            if (_target != null) _lastAimPoint = ResolveAimPoint(_lastOrigin, _target.position + _aimOffset);

            float elapsed = Time.time - _startTime;
            float growth = Mathf.Clamp01(elapsed / growthDuration);
            Vector3 end = Vector3.Lerp(_lastOrigin, _lastAimPoint, growth);
            beam.SetPosition(0, _lastOrigin);
            beam.SetPosition(1, end);

            hitEffect.transform.position = end;
            if (growth >= 1f && !_isHitPlaying)
            {
                CaptureImpact();
                _isHitPlaying = true;
                if (HasArmorImpact) hitEffect.Play(true);
                CompleteImpact(end, _lastAimPoint - _lastOrigin);
            }

            if (elapsed >= growthDuration + _currentHoldDuration)
            {
                StopBeam();
            }
        }

        public void StopBeam()
        {
            beam.enabled = false;
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _muzzle = null;
            _target = null;
        }

        protected override bool IsVisualComplete() => !beam.enabled;
    }
}
