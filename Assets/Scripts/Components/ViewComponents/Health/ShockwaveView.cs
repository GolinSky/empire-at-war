using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    /// <summary>
    /// An expanding refraction sphere around a destroyed unit, drawn by <see cref="ShockwaveRefractionFeature"/>,
    /// with a flat fire ring on its equator. Both share one progress value.
    /// </summary>
    public sealed class ShockwaveView : MonoBehaviour
    {
        private static readonly int _progressId = Shader.PropertyToID("_Progress");

        [SerializeField] private MeshRenderer sphere;
        [Tooltip("Flat XZ fire ring; a child of this object, so it grows with the sphere.")]
        [SerializeField] private MeshRenderer fireRing;
        [SerializeField, Min(0.01f)] private float duration = 2.4f;
        [Tooltip("Sphere radius when the wave forms, as a multiple of the hull radius.")]
        [SerializeField, Min(0f)] private float startRadius = 0.3f;
        [Tooltip("Sphere radius when the wave fades out, as a multiple of the hull radius.")]
        [SerializeField, Min(0f)] private float endRadius = 2.5f;
        [Tooltip("Farthest the wave travels past the hull, in world units, so dreadnoughts don't get map-sized waves.")]
        [SerializeField, Min(0f)] private float maxReachPastHull = 250f;

        private MaterialPropertyBlock _block;
        private float _hullRadius;
        private float _age;

        /// <summary>Enabled shockwaves; the renderer feature copies the frame only while this is above zero.</summary>
        public static int ActiveCount { get; private set; }

        public bool IsAlive => _age < duration;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            ActiveCount++;
        }

        private void OnDisable()
        {
            ActiveCount--;
        }

        public void Play(Vector3 center, float hullRadius)
        {
            _hullRadius = hullRadius;
            _age = 0f;
            transform.SetPositionAndRotation(center, Quaternion.identity);
            gameObject.SetActive(true);
            Apply();
        }

        public void Advance(float deltaTime)
        {
            _age += deltaTime;
            Apply();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Apply()
        {
            float progress = Mathf.Clamp01(_age / duration);
            // Ease-out: the wave bursts out fast, then slows as it spreads.
            float eased = 1f - (1f - progress) * (1f - progress);
            float finalRadius = _hullRadius + Mathf.Min(_hullRadius * (endRadius - 1f), maxReachPastHull);
            float radius = Mathf.Lerp(_hullRadius * startRadius, finalRadius, eased);
            // The unit sphere mesh has radius 0.5.
            transform.localScale = Vector3.one * (radius * 2f);
            _block.SetFloat(_progressId, progress);
            sphere.SetPropertyBlock(_block);
            fireRing.SetPropertyBlock(_block);
        }
    }
}
