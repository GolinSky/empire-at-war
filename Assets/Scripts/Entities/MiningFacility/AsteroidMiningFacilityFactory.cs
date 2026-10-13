using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.IdGeneration;
using VContainer.Unity;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class AsteroidMiningFacilityFactory : MiningFacilityFactory
    {
        private const string ASTEROID_PREFIX = "Asteroid";
        protected override string AssetPrefix => ASTEROID_PREFIX;

        public AsteroidMiningFacilityFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership,
            ILocalPlayer localPlayer, IUniqueIdGenerator ids) : base(assets, scope, ownership, localPlayer, ids) { }
    }
}
