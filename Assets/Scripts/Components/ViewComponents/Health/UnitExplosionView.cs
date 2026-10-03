using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class UnitExplosionView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private ParticleSystem envelope;
        [SerializeField] private Light flashLight;

        [SerializeField] private float flashDuration;
        private float _flashRange;
        private float _flashIntensity;
        private float _elapsed;

        // The centered flash has a constant start size and covers the hull at the start of the burst.
        public float Diameter => envelope.main.startSize.constant;
        public bool IsAlive => particles.IsAlive(true);

        private void Awake()
        {
            _flashRange = flashLight.range;
            _flashIntensity = flashLight.intensity;
        }

        public void Play(Vector3 position, float scale)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one * scale;
            gameObject.SetActive(true);
            _elapsed = 0f;
            flashLight.range = _flashRange * scale;
            flashLight.intensity = _flashIntensity;
            particles.Play(true);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            flashLight.intensity = Mathf.Lerp(_flashIntensity, 0f, _elapsed / flashDuration);
        }

        public void Hide()
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            flashLight.intensity = 0f;
            gameObject.SetActive(false);
        }
    }
}
