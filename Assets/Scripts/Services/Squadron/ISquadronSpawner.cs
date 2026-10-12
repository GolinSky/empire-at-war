using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Players;
using UnityEngine;

namespace EmpireAtWar.Services.Squadrons
{
    /// <summary>Puts a squadron into the battle, reusing a destroyed one of the same owner and type when possible.</summary>
    public interface ISquadronSpawner
    {
        Squadron Spawn(PlayerId owner, SquadronType squadronType, Vector3 position, Quaternion rotation);
    }
}
