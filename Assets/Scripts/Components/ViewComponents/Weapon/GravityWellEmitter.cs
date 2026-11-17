using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Gravity well generator domes at full load: charging particles and glow on each dome plus a bridge wash.
    /// Emission runs only while the ability is active and the ship is on screen; particles drain out when it ends.
    /// </summary>
    public sealed class GravityWellEmitter : MonoBehaviour, IGravityWellEmitterFacade, IRespawnable, IDespawnable
    {
        [SerializeField] private ParticleSystem[] systems;
        [SerializeField] private float cullRadius = 120f;

        private bool _onScreen = true;

        public Bounds CullBounds => new Bounds(transform.position, Vector3.one * (cullRadius * 2f));

        public void SetEmitting(bool emitting)
        {
            // The ship's view can already be destroyed when the ability stops during scope teardown.
            if (this == null) return;
            foreach (ParticleSystem system in systems)
            {
                if (emitting) system.Play(true);
                else system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
            if (emitting) ApplyEmission(_onScreen);
        }

        public void SetOnScreen(bool onScreen)
        {
            if (_onScreen == onScreen || this == null) return;
            _onScreen = onScreen;
            ApplyEmission(onScreen);
        }

        public void Respawn() => Clear();

        public void Despawn() => Clear();

        private void ApplyEmission(bool enabled)
        {
            foreach (ParticleSystem system in systems)
            {
                ParticleSystem.EmissionModule emission = system.emission;
                emission.enabled = enabled;
            }
        }

        private void Clear()
        {
            _onScreen = true;
            ApplyEmission(true);
            foreach (ParticleSystem system in systems) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
