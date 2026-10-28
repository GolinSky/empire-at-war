using EmpireAtWar.Mvc;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using VContainer.Unity;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class MiningFacilityFactory : BaseFactory
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly IUniqueIdGenerator _ids;
        private readonly MiningFacilityCatalog _catalog;

        public MiningFacilityFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids, MiningFacilityCatalog catalog) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
            _catalog = catalog;
        }

        public virtual MiningFacility Create(PlayerId owner, MiningFacilityType type, Vector3 position, float? buildDuration = null) =>
            Create(owner, type, position, _ids.GenerateUniqueId(), buildDuration);

        /// <summary>Creates a facility with a known id, e.g. one restored from a save.</summary>
        public MiningFacility Create(PlayerId owner, MiningFacilityType type, Vector3 position, long entityId, float? buildDuration = null)
        {
            string assetName = MiningFacilityAssets.GetName(type);
            MiningFacilityData data = Assets.Load<MiningFacilityData>(assetName + "Data");
            float buildTime = buildDuration ?? (type == MiningFacilityType.AsteroidMiner
                ? Assets.Load<CaptureSiteData>(nameof(CaptureSiteData)).GetCost(SiteFacilityType.Mining).BuildTime
                : _catalog.Get(type == MiningFacilityType.BattleMiner ? MiningFacilityType.CommonMiner : type).BuildTime);
            return Spawn(Assets.LoadComponent<MiningFacility>(assetName + "View"), position, Quaternion.identity,
                (builder, view) => new EmpireAtWar.MiningFacility.MiningFacilityInstaller(view, data,
                    _localPlayer, owner, entityId, position, type, buildTime).Install(builder));
        }
    }
}
