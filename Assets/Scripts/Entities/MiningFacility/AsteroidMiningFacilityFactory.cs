using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.IdGeneration;
using VContainer.Unity;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class AsteroidMiningFacilityFactory : MiningFacilityFactory
    {
        public AsteroidMiningFacilityFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership,
            ILocalPlayer localPlayer, IUniqueIdGenerator ids, MiningFacilityCatalog catalog) : base(assets, scope, ownership, localPlayer, ids, catalog) { }
    }
}
