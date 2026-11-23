using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class UnitExplosionView : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private ParticleSystem envelope;
        [Tooltip("Size on top of covering the hull: flipbook fire fills only part of its billboard.")]
        [SerializeField, Min(0.01f)] private float sizeMultiplier = 1f;

        // The centered billboard has a constant start size and covers the hull at the start of the burst.
        public float Diameter => envelope.main.startSize.constant;
        public bool IsAlive => particles.IsAlive(true);

        public void Play(Vector3 position, float scale)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one * (scale * sizeMultiplier);
            gameObject.SetActive(true);
            particles.Play(true);
        }

        public void Hide()
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            gameObject.SetActive(false);
        }
    }
}
