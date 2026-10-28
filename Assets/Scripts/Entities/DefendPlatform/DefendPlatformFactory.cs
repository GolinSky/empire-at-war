using EmpireAtWar.Mvc;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using VContainer.Unity;

namespace EmpireAtWar.Entities.DefendPlatform
{
    public class DefendPlatformFactory : BaseFactory
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly IUniqueIdGenerator _ids;
        private readonly DefendPlatformCatalog _catalog;
        protected virtual string AssetPrefix => "";

        public DefendPlatformFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids, DefendPlatformCatalog catalog) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
            _catalog = catalog;
        }

        public virtual DefendPlatform Create(PlayerId owner, DefendPlatformType type, Vector3 position, float? buildDuration = null) =>
            Create(owner, type, position, _ids.GenerateUniqueId(), buildDuration);

        /// <summary>Creates a platform with a known id, e.g. one restored from a save.</summary>
        public DefendPlatform Create(PlayerId owner, DefendPlatformType type, Vector3 position, long entityId, float? buildDuration = null)
        {
            string dataPath = type switch
            {
                DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformData",
                DefendPlatformType.Empress => "AotrEmpressDefensePlatformData",
                DefendPlatformType.QuadLaserSatellite or DefendPlatformType.TurbolaserSatellite or
                    DefendPlatformType.IonSatellite or DefendPlatformType.MassDriverSatellite or
                    DefendPlatformType.MissileSatellite => type + "Data",
                _ => nameof(DefendPlatformData),
            };
            string prefabPath = type switch
            {
                DefendPlatformType.GolanIII => "AotrGolanIIIDefensePlatformView",
                DefendPlatformType.Empress => "AotrEmpressDefensePlatformView",
                DefendPlatformType.QuadLaserSatellite or DefendPlatformType.TurbolaserSatellite or
                    DefendPlatformType.IonSatellite or DefendPlatformType.MassDriverSatellite or
                    DefendPlatformType.MissileSatellite => type + "View",
                _ => "DefendPlatformView",
            };
            DefendPlatformData data = Assets.Load<DefendPlatformData>(AssetPrefix + dataPath);
            float buildTime = buildDuration ?? (type == DefendPlatformType.BattleAsteroid
                ? Assets.Load<CaptureSiteData>(nameof(CaptureSiteData)).GetCost(SiteFacilityType.BattleAsteroid).BuildTime
                : _catalog.Get(type).BuildTime);
            return Spawn(Assets.LoadComponent<DefendPlatform>(AssetPrefix + prefabPath), position, Quaternion.identity,
                (builder, view) => new DefendPlatformInstaller(view, data, _localPlayer, owner, entityId, position, type, buildTime).Install(builder));
        }
    }
}
