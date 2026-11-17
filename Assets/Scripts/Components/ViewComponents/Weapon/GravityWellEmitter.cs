using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Gravity well generator domes at full load: charging particles and glow on each dome plus a bridge wash.
    /// Emission runs only while the ability is active; particles drain out when it ends.
    /// </summary>
    public sealed class GravityWellEmitter : MonoBehaviour, IGravityWellEmitterFacade, IRespawnable, IDespawnable
    {
        [SerializeField] private Transform[] domeAnchors;
        [SerializeField] private ParticleSystem[] systems;

        public void SetEmitting(bool emitting)
        {
            // The ship's view can already be destroyed when the ability stops during scope teardown.
            if (this == null) return;
            foreach (ParticleSystem system in systems)
            {
                if (emitting) system.Play(true);
                else system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        public void Respawn() => Clear();

        public void Despawn() => Clear();

        private void Clear()
        {
            foreach (ParticleSystem system in systems) system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}
