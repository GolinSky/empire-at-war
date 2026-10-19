using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>Puts a squadron into the battle, reusing a destroyed one of the same owner and type when possible.</summary>
    public interface ISquadronSpawner
    {
        Squadron Spawn(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation);

        /// <summary>Builds a new squadron with a saved id; pooled squadrons keep their own ids, so none is reused.</summary>
        Squadron SpawnRestored(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation,
            long entityId);
    }
}
