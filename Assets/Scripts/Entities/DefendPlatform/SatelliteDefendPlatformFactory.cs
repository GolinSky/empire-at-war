using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.IdGeneration;
using VContainer.Unity;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class SatelliteDefendPlatformFactory : DefendPlatformFactory
    {

        public SatelliteDefendPlatformFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership,
            ILocalPlayer localPlayer, IUniqueIdGenerator ids) : base(assets, scope, ownership, localPlayer, ids) { }
    }
}
