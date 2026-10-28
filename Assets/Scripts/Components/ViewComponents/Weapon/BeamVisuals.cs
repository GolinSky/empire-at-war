using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Presentation-only charge, sustained contact, and residual energy for a heavy beam.</summary>
    public sealed class BeamVisuals : MonoBehaviour
    {
        private const float CHARGE_DURATION = 0.12f;
        private const float SHUTDOWN_DURATION = 0.18f;
        private const float RESIDUAL_DURATION = 0.6f;
        private static readonly int BEAM_AGE = Shader.PropertyToID("_BeamAge");
        private static readonly int BEAM_FADE = Shader.PropertyToID("_BeamFade");
        private static readonly int BEAM_LENGTH = Shader.PropertyToID("_BeamLength");

        [SerializeField] private LineRenderer beam;
        [SerializeField] private ParticleSystem chargeEffect;
        [SerializeField] private ParticleSystem muzzleEffect;
        [SerializeField] private ParticleSystem contactEffect;
        [SerializeField] private ParticleSystem hullSparks;
        [SerializeField] private Light muzzleLight;

        private MaterialPropertyBlock _properties;
        private float _startedAt;
        private float _stoppedAt;
        private float _width;
        private bool _active;
        private bool _stopping;
        private bool _discharged;

        public bool IsComplete => !_active;

        public void Begin(float width, Vector3 origin)
        {
            ResetVisuals();
            _properties = new MaterialPropertyBlock();
            _width = width;
            _startedAt = Time.time;
            _active = true;
            chargeEffect.transform.position = origin;
            chargeEffect.Play(true);
            muzzleLight.enabled = true;
        }

        public void Tick(Vector3 origin, Vector3 end, bool hasContact, bool armorContact)
        {
            if (!_active) return;

            float age = Time.time - _startedAt;
            float fade = _stopping ? Mathf.Clamp01((Time.time - _stoppedAt) / SHUTDOWN_DURATION) : 0f;
            chargeEffect.transform.position = origin;
            muzzleEffect.transform.position = origin;
            muzzleLight.transform.position = origin;
            contactEffect.transform.position = end;
            hullSparks.transform.position = end;

            if (!_stopping && !_discharged && age >= CHARGE_DURATION)
            {
                chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                muzzleEffect.Play(true);
                _discharged = true;
            }

            if (!_stopping)
            {
                SetEmission(contactEffect, hasContact);
                SetEmission(hullSparks, hasContact && armorContact);
            }

            float flicker = 1f + 0.025f * Mathf.Sin(age * 93f) + 0.015f * Mathf.Sin(age * 137f);
            beam.widthMultiplier = _width * flicker * (1f - fade * 0.85f);
            _properties.SetFloat(BEAM_AGE, age);
            _properties.SetFloat(BEAM_FADE, 1f - fade);
            _properties.SetFloat(BEAM_LENGTH, Vector3.Distance(origin, end));
            beam.SetPropertyBlock(_properties);
            float charge = Mathf.Clamp01(age / CHARGE_DURATION);
            float discharge = Mathf.Max(0f, 1f - Mathf.Abs(age - CHARGE_DURATION) / 0.08f);
            muzzleLight.intensity = (2f + charge * 4f + discharge * 6f) * flicker * (1f - fade);
            muzzleLight.enabled = fade < 1f;
            if (_stopping && fade >= 1f) beam.enabled = false;
            if (_stopping && Time.time - _stoppedAt >= RESIDUAL_DURATION) ResetVisuals();
        }

        public void Stop()
        {
            if (!_active || _stopping) return;
            _stopping = true;
            _stoppedAt = Time.time;
            chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            muzzleEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            contactEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            hullSparks.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        public void ResetVisuals()
        {
            chargeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            contactEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hullSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleLight.enabled = false;
            _active = false;
            _stopping = false;
            _discharged = false;
            beam.SetPropertyBlock(null);
        }

        private static void SetEmission(ParticleSystem particles, bool emitting)
        {
            if (emitting && !particles.isEmitting) particles.Play(true);
            else if (!emitting && particles.isEmitting)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
