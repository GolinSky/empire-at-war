using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class ExplosionVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private Renderer[] renderers;

        [SerializeField] private ParticleSystem[] sustainedEmitters = System.Array.Empty<ParticleSystem>();
        private bool _emitting = true;

        public IReadOnlyList<Renderer> Renderers => renderers;
        public bool HasSustainedEmission => sustainedEmitters.Length > 0;

        public void SetSustainedEmission(bool enabled)
        {
            if (_emitting == enabled) return;
            _emitting = enabled;
            foreach (ParticleSystem emitter in sustainedEmitters)
            {
                var emission = emitter.emission;
                emission.enabled = enabled;
            }
        }

        public bool IsPlaying => particles.IsAlive(true);
        public bool HasExpired => particles == null;

        public void Play() => particles.Play();

        public void StopEmitting() => particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        public void Clear() => particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
