using EmpireAtWar.Services.Audio;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class HarmonicBombView : MonoBehaviour
    {
        private const float SHADER_RADIUS = 0.9f;
        private static readonly int RADIUS_ID = Shader.PropertyToID("_Radius");
        private static readonly int WIDTH_ID = Shader.PropertyToID("_Width");
        private static readonly int COLOR_ID = Shader.PropertyToID("_Color");
        private static readonly int INTENSITY_ID = Shader.PropertyToID("_Intensity");
        private static readonly int OPACITY_ID = Shader.PropertyToID("_Opacity");
        private static readonly int CORE_ID = Shader.PropertyToID("_Core");
        private static readonly int EDGE_NOISE_ID = Shader.PropertyToID("_EdgeNoise");

        [SerializeField] private MeshRenderer coreFlash;
        [SerializeField] private MeshRenderer shockwaveRing;
        [SerializeField] private MeshRenderer secondaryRing;
        [SerializeField] private SfxProfile detonation;
        [SerializeField] private float duration = 0.8f;
        [SerializeField] private float flashDuration = 0.15f;
        [SerializeField] private float ringDelay = 0.08f;
        [SerializeField] private float expansionDuration = 0.52f;
        [SerializeField] private float secondaryDelay = 0.14f;
        [SerializeField] private Color coreColor = new Color(0.91f, 1f, 1f);
        [SerializeField] private Color ringColor = new Color(0.263f, 0.875f, 1f);
        [SerializeField] private Color secondaryColor = new Color(0f, 0.549f, 1f);

        private MaterialPropertyBlock _properties;
        private float _elapsed;
        private float _blastRadius;

        public SfxProfile Detonation => detonation;
        public float VisibilityRadius => _elapsed < 0f ? 2f : _blastRadius;

        public void ShowMarker(Vector3 position)
        {
            _properties ??= new MaterialPropertyBlock();
            _elapsed = -1f;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one;
            coreFlash.transform.localScale = Vector3.one * 4f;
            coreFlash.enabled = true;
            shockwaveRing.enabled = false;
            secondaryRing.enabled = false;
            SetAppearance(coreFlash, SHADER_RADIUS, 0.1f, coreColor, 2f, 1f, true);
            gameObject.SetActive(true);
        }

        public void BeginBlast(float radius)
        {
            _elapsed = 0f;
            _blastRadius = radius;
            float diameter = 2f * radius / SHADER_RADIUS;
            shockwaveRing.transform.localScale = Vector3.one * diameter;
            secondaryRing.transform.localScale = Vector3.one * diameter;
            AnimateBlast();
        }

        public void SetVisible(bool visible)
        {
            coreFlash.forceRenderingOff = !visible;
            shockwaveRing.forceRenderingOff = !visible;
            secondaryRing.forceRenderingOff = !visible;
        }

        public bool Advance(float deltaTime)
        {
            if (_elapsed < 0f) return false;
            _elapsed += deltaTime;
            AnimateBlast();
            return _elapsed >= duration;
        }

        private void AnimateBlast()
        {
            float flash = Mathf.Clamp01(_elapsed / flashDuration);
            coreFlash.enabled = flash < 1f;
            coreFlash.transform.localScale = Vector3.one * Mathf.Lerp(4f, _blastRadius * 0.28f, flash);
            SetAppearance(coreFlash, SHADER_RADIUS, 0.1f, coreColor, 10f, (1f - flash) * (1f - flash), true);
            AnimateRing(shockwaveRing, ringDelay, ringColor, 5f, 1f);
            AnimateRing(secondaryRing, secondaryDelay, secondaryColor, 2.5f, 0.55f);
        }

        private void AnimateRing(MeshRenderer target, float delay, Color color, float intensity, float strength)
        {
            float age = _elapsed - delay;
            target.enabled = age >= 0f && _elapsed < duration;
            float progress = Mathf.Clamp01(age / expansionDuration);
            float radius = SHADER_RADIUS * (1f - (1f - progress) * (1f - progress));
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(delay + 0.3f, duration, _elapsed));
            SetAppearance(target, radius, Mathf.Lerp(0.065f, 0.012f, progress) * strength,
                color, intensity, fade * strength, false);
        }

        private void SetAppearance(MeshRenderer target, float radius, float width, Color color,
            float intensity, float opacity, bool core)
        {
            _properties.Clear();
            _properties.SetFloat(RADIUS_ID, radius);
            _properties.SetFloat(WIDTH_ID, width);
            _properties.SetColor(COLOR_ID, color);
            _properties.SetFloat(INTENSITY_ID, intensity);
            _properties.SetFloat(OPACITY_ID, opacity);
            _properties.SetFloat(CORE_ID, core ? 1f : 0f);
            _properties.SetFloat(EDGE_NOISE_ID, core ? 0f : 0.15f);
            target.SetPropertyBlock(_properties);
        }
    }
}
