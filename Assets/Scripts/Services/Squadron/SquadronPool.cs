using System.Collections.Generic;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>
    /// Reuses destroyed squadrons instead of building a new squadron context per launch, which costs several
    /// milliseconds. Owner and type change the installed bindings, so squadrons are pooled per owner and type.
    /// Despawned squadrons are destroyed with the battle scene.
    /// </summary>
    public sealed class SquadronPool : ISquadronSpawner, ITickable
    {
        // How long a destroyed squadron's wrecks and explosions stay in the battle before it is despawned.
        private const float DEATH_PRESENTATION_SECONDS = 6f;

        private readonly SquadronFactory _factory;
        private readonly Dictionary<(PlayerId, SquadronType), List<Squadron>> _despawned =
            new Dictionary<(PlayerId, SquadronType), List<Squadron>>();
        private readonly List<DyingSquadron> _dying = new List<DyingSquadron>();

        public SquadronPool(SquadronFactory factory)
        {
            _factory = factory;
        }

        public Squadron Spawn(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation)
        {
            (PlayerId, SquadronType) key = (owner, squadronType);
            Squadron squadron;
            if (TryTakeDespawned(key, out squadron))
            {
                squadron.Respawn(position, rotation);
            }
            else
            {
                squadron = _factory.Create(owner, squadronType, position, rotation);
            }

            // Respawning drops the previous life's listeners, so every life is watched anew.
            squadron.Released += () => HandleReleased(squadron, key);
            return squadron;
        }

        public void Tick()
        {
            for (int i = _dying.Count - 1; i >= 0; i--)
            {
                DyingSquadron dying = _dying[i];
                if (Time.time < dying.DespawnTime) continue;
                _dying.RemoveAt(i);
                dying.Squadron.Despawn();
                if (!_despawned.TryGetValue(dying.Key, out List<Squadron> squadrons))
                {
                    squadrons = new List<Squadron>();
                    _despawned.Add(dying.Key, squadrons);
                }

                squadrons.Add(dying.Squadron);
            }
        }

        // A squadron released without being destroyed is leaving with the scene and is not reused.
        private void HandleReleased(Squadron squadron, (PlayerId, SquadronType) key)
        {
            if (!squadron.IsDestroyed) return;
            _dying.Add(new DyingSquadron
            {
                Squadron = squadron,
                Key = key,
                DespawnTime = Time.time + DEATH_PRESENTATION_SECONDS
            });
        }

        private bool TryTakeDespawned((PlayerId, SquadronType) key, out Squadron squadron)
        {
            squadron = null;
            if (!_despawned.TryGetValue(key, out List<Squadron> squadrons)) return false;
            for (int i = 0; i < squadrons.Count; i++)
            {
                if (!squadrons[i].CanRespawn) continue;
                squadron = squadrons[i];
                squadrons.RemoveAt(i);
                return true;
            }

            return false;
        }

        private struct DyingSquadron
        {
            public Squadron Squadron;
            public (PlayerId, SquadronType) Key;
            public float DespawnTime;
        }
    }
}
