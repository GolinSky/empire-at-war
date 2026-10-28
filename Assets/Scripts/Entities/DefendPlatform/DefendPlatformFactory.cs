using EmpireAtWar.Mvc;
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
        protected virtual string AssetPrefix => "";

        public DefendPlatformFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
        }

        public virtual DefendPlatform Create(PlayerId owner, DefendPlatformType type, Vector3 position) =>
            Create(owner, type, position, _ids.GenerateUniqueId());

        /// <summary>Creates a platform with a known id, e.g. one restored from a save.</summary>
        public DefendPlatform Create(PlayerId owner, DefendPlatformType type, Vector3 position, long entityId)
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
            return Spawn(Assets.LoadComponent<DefendPlatform>(AssetPrefix + prefabPath), position, Quaternion.identity,
                (builder, view) => new DefendPlatformInstaller(view, data, _localPlayer, owner, entityId, position, type).Install(builder));
        }
    }
}
