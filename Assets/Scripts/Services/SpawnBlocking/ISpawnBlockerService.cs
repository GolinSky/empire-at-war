using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.OwnedAreas;
using UnityEngine;

namespace EmpireAtWar.Services.SpawnBlocking
{
    /// <summary>
    /// Circles where reinforcements of hostile teams cannot arrive. <see cref="PlayerId.None"/> is never
    /// allied, so neutral blockers (uncaptured relays, asteroids) block every team. Distances ignore height.
    /// </summary>
    public interface ISpawnBlockerService
    {
        IReadOnlyList<OwnedCircle> Blockers { get; }

        /// <summary>Adds a blocker, or updates the owner and radius of one already registered.</summary>
        void Register(PlayerId owner, Transform source, float radius);

        void Unregister(Transform source);

        bool IsBlocked(PlayerId team, Vector3 position);
    }
}
