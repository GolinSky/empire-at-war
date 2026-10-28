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
        [Tooltip("Optional layered presentation; unassigned beam prefabs retain their existing visuals.")]
        [SerializeField] private BeamVisuals visuals;
        private Transform _muzzle;
        private Transform _target;

        private Vector3 _aimOffset;
        private Vector3 _lastOrigin;
        private Vector3 _lastAimPoint;

        [SerializeField] private float growthDuration = 0.3f;
        [SerializeField] private float holdDuration = 0.5f;
        [SerializeField] private float fadeDuration;
        private float _startTime;
        private float _stopTime;
        private Color _beamColor;
        private float _currentHoldDuration;

        public float FadeDuration => fadeDuration;

        private bool _isHitPlaying;
        private bool _isStopping;

        /// <summary>Plays the beam outside the shot pool, e.g. from an ability.</summary>
        public void PlayBeam(Transform muzzle, Transform target, float holdDuration, WeaponProfile profile)
        {
            // Ability damage is resolved by its presenter immediately after this call.
            CaptureImpact();
            ApplyProfile(profile);
            StartBeam(muzzle, target, Vector3.zero, holdDuration);
        }

        protected override float Play(Transform muzzle, Transform target, Vector3 aimOffset, WeaponProfile profile)
        {
            ApplyProfile(profile);
            StartBeam(muzzle, target, aimOffset, holdDuration);
            return growthDuration;
        }

        private void ApplyProfile(WeaponProfile profile)
        {
            _beamColor = profile.Color;
            beam.startColor = profile.Color;
            beam.endColor = profile.Color;
            beam.widthMultiplier = profile.Size.x;
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
            _isStopping = false;
            if (fadeDuration > 0f) SetOpacity(0f);
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (visuals != null)
            {
                visuals.Begin(beam.widthMultiplier, _lastOrigin);
                visuals.Tick(_lastOrigin, _lastOrigin, false, false);
            }
        }

        protected override void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileLaserUpdate.Auto())
            {
#endif
            if (beam.enabled || (visuals != null && !visuals.IsComplete))
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
            if (_isStopping)
            {
                if (visuals != null) visuals.Tick(_lastOrigin, beam.GetPosition(1), false, false);
                else
                {
                    float fade = Mathf.Clamp01((Time.time - _stopTime) / fadeDuration);
                    SetOpacity(1f - fade);
                    if (fade >= 1f) beam.enabled = false;
                }
                return;
            }
            // Keep the last known points when the shooter or the target is destroyed mid-beam.
            if (_muzzle != null) _lastOrigin = _muzzle.position;
            if (_target != null && !IsImpactTargetDestroyed)
                _lastAimPoint = ResolveAimPoint(_lastOrigin, _target.position + _aimOffset);

            float elapsed = Time.time - _startTime;
            if (fadeDuration > 0f) SetOpacity(Mathf.Clamp01(elapsed / fadeDuration));
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

            if (_isHitPlaying)
            {
                if (HasArmorImpact && !hitEffect.isPlaying) hitEffect.Play(true);
                else if (!HasArmorImpact && hitEffect.isPlaying)
                    hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            if (elapsed >= growthDuration + _currentHoldDuration)
            {
                StopBeam();
            }
            if (visuals != null)
                visuals.Tick(_lastOrigin, end, _isHitPlaying && !IsImpactTargetDestroyed, HasArmorImpact);
        }

        public void StopBeam()
        {
            if (_isStopping) return;
            if (visuals != null)
            {
                visuals.Stop();
                _isStopping = true;
            }
            else if (fadeDuration > 0f)
            {
                _beamColor = beam.startColor;
                _stopTime = Time.time;
                _isStopping = true;
            }
            else beam.enabled = false;
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _muzzle = null;
            _target = null;
        }

        // Beams deal damage continuously while they hold on the target.
        public override float DamageDuration => holdDuration;

        private void SetOpacity(float opacity)
        {
            Color color = _beamColor;
            color.a *= opacity;
            beam.startColor = color;
            beam.endColor = color;
        }

        protected override void ResetVisuals()
        {
            beam.enabled = false;
            _isStopping = false;
            if (visuals != null) visuals.ResetVisuals();
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        protected override bool IsVisualComplete() => !beam.enabled && (visuals == null || visuals.IsComplete);
    }
}
