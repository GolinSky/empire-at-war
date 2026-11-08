using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>Puts a squadron into the battle, reusing a destroyed one of the same owner and type when possible.</summary>
    public interface ISquadronSpawner
    {
        Squadron Spawn(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation);

        /// <summary>
        /// Places a squadron with a given id (save load, online replica). Pooled squadrons keep their own ids, so
        /// only a dead or despawned squadron with that same id is reused; otherwise a new one is built.
        /// </summary>
        Squadron SpawnRestored(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation,
            long entityId);
    }
}
