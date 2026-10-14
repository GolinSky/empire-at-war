using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class ExplosionVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private Renderer[] renderers;

        public IReadOnlyList<Renderer> Renderers => renderers;
        public bool IsPlaying => particles.IsAlive(true);

        public void Play() => particles.Play();

        public void StopEmitting() => particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        public void Clear() => particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
