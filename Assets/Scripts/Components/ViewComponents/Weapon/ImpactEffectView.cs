using EmpireAtWar.Components.Weapon;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Shared, bounded world-space particle buffers; no GameObject or coroutine per impact.</summary>
    public sealed class ImpactEffectView : MonoBehaviour, IImpactEffectView
    {
        private const int SPARK_COUNT = 8;
        [SerializeField] private ParticleSystem shieldFlash;
        [SerializeField] private ParticleSystem shieldRing;
        [SerializeField] private ParticleSystem armorFlash;
        [SerializeField] private ParticleSystem armorSparks;
        private readonly System.Random _random = new System.Random();

        public void Emit(ImpactSurface surface, Vector3 position, Vector3 direction, float size)
        {
            float scale = Mathf.Clamp(size, 0.75f, 2.5f);
            if (surface == ImpactSurface.Shield)
            {
                EmitParticle(shieldFlash, position, Vector3.zero, scale * 2f, 0.16f);
                EmitParticle(shieldRing, position, Vector3.zero, scale * 3f, 0.35f);
                return;
            }

            EmitParticle(armorFlash, position, Vector3.zero, scale * 1.8f, 0.18f);
            Vector3 outward = -direction.normalized;
            for (int i = 0; i < SPARK_COUNT; i++)
            {
                Vector3 spread = new Vector3((float)_random.NextDouble() * 2f - 1f,
                    (float)_random.NextDouble() * 2f - 1f, (float)_random.NextDouble() * 2f - 1f);
                Vector3 velocity = (outward + spread) * (scale * 7f);
                EmitParticle(armorSparks, position, velocity, scale * 0.18f,
                    0.2f + (float)_random.NextDouble() * 0.25f);
            }
        }

        private static void EmitParticle(ParticleSystem system, Vector3 position, Vector3 velocity,
            float size, float lifetime)
        {
            ParticleSystem.EmitParams particle = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startSize = size,
                startLifetime = lifetime
            };
            system.Emit(particle, 1);
        }
    }
}
