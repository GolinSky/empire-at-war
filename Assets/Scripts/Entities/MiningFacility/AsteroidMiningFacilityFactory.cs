using EmpireAtWar.Models.Players;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class AsteroidMiningFacilityFactory
        : PlaceholderFactory<PlayerId, MiningFacilityType, Vector3, MiningFacility>
    {
    }
}
