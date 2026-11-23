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
        private static readonly int CORE_COLOR = Shader.PropertyToID("_CoreColor");
        private static readonly int BODY_COLOR = Shader.PropertyToID("_BodyColor");
        // HDR peaks of the HeavyLaserBeam material (core 3.5, body 2.2); the beam colour sets only the hue.
        private const float CORE_INTENSITY = 3.5f;
        private const float BODY_INTENSITY = 2.2f;
        private const float CORE_TINT = 0.15f;

        [SerializeField] private LineRenderer beam;
        [SerializeField] private ParticleSystem chargeEffect;
        [SerializeField] private ParticleSystem muzzleEffect;
        [SerializeField] private ParticleSystem contactEffect;
        [SerializeField] private ParticleSystem hullSparks;
        [SerializeField] private Light muzzleLight;

        private MaterialPropertyBlock _properties;
        private ParticleSystem[] _tintedEffects;
        private Color[] _authoredEffectColors;
        private Color _authoredLightColor;
        private float _startedAt;
        private float _stoppedAt;
        private float _width;
        private bool _active;
        private bool _stopping;
        private bool _discharged;

        public bool IsComplete => !_active;

        public void Begin(float width, Vector3 origin, Color color)
        {
            ResetVisuals();
            _properties ??= new MaterialPropertyBlock();
            Tint(color);
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

        // Beam body and core take the colour at the material's brightness; glows and the light keep their authored
        // saturation and brightness and take its hue.
        private void Tint(Color color)
        {
            if (_tintedEffects == null)
            {
                _tintedEffects = new[] { chargeEffect, muzzleEffect, contactEffect, hullSparks };
                _authoredEffectColors = new Color[_tintedEffects.Length];
                for (int i = 0; i < _tintedEffects.Length; i++)
                    _authoredEffectColors[i] = _tintedEffects[i].main.startColor.color;
                _authoredLightColor = muzzleLight.color;
            }

            float peak = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            Color unit = peak > 0f ? color / peak : Color.white;
            unit.a = 1f;
            Color body = unit * BODY_INTENSITY;
            body.a = 1f;
            Color core = Color.Lerp(Color.white, unit, CORE_TINT) * CORE_INTENSITY;
            core.a = 1f;
            _properties.SetColor(BODY_COLOR, body);
            _properties.SetColor(CORE_COLOR, core);
            beam.SetPropertyBlock(_properties);

            Color.RGBToHSV(color, out float hue, out _, out _);
            for (int i = 0; i < _tintedEffects.Length; i++)
            {
                ParticleSystem.MainModule main = _tintedEffects[i].main;
                main.startColor = WithHue(_authoredEffectColors[i], hue);
            }
            muzzleLight.color = WithHue(_authoredLightColor, hue);
        }

        private static Color WithHue(Color authored, float hue)
        {
            Color.RGBToHSV(authored, out _, out float saturation, out float value);
            Color tinted = Color.HSVToRGB(hue, saturation, value, true);
            tinted.a = authored.a;
            return tinted;
        }

        private static void SetEmission(ParticleSystem particles, bool emitting)
        {
            if (emitting && !particles.isEmitting) particles.Play(true);
            else if (!emitting && particles.isEmitting)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
