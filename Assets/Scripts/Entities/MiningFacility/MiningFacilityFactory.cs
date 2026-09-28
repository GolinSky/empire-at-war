using EmpireAtWar.Models.Factions;
using UnityEngine;
using EmpireAtWar.Models.Players;
using Zenject;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class MiningFacilityFactory : PlaceholderFactory<PlayerId, MiningFacilityType, Vector3, MiningFacility> {}
}
