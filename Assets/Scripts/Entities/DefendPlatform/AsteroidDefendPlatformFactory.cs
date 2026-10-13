using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.IdGeneration;
using VContainer.Unity;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class AsteroidDefendPlatformFactory : DefendPlatformFactory
    {
        private const string ASTEROID_PREFIX = "Asteroid";
        protected override string AssetPrefix => ASTEROID_PREFIX;

        public AsteroidDefendPlatformFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership,
            ILocalPlayer localPlayer, IUniqueIdGenerator ids) : base(assets, scope, ownership, localPlayer, ids) { }
    }
}
