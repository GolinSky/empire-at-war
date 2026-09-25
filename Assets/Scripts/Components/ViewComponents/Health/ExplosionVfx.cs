using System.Collections.Generic;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Health
{
    public sealed class ExplosionVfx : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private Renderer[] renderers;

        public IReadOnlyList<Renderer> Renderers => renderers;

        public void Play() => particles.Play();
    }
}
