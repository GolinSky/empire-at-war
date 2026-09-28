using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Squadrons
{
    public class SquadronFactory : PlaceholderFactory<PlayerId, SquadronType, Vector3, Quaternion, Squadron>
    {
    }
}
