using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Players;
using UnityEngine;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;

namespace EmpireAtWar.MiningFacility
{
    public sealed class AsteroidMiningFacilityInstaller : MiningFacilityInstaller
    {
        public AsteroidMiningFacilityInstaller(MiningFacilityEntity view, MiningFacilityData data,
            ILocalPlayer localPlayer, PlayerId owner, long entityId, Vector3 position, MiningFacilityType type)
            : base(view, data, localPlayer, owner, entityId, position, type) { }
    }
}
