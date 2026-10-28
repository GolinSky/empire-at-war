using EmpireAtWar.Mvc;
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

        public MiningFacilityFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
        }

        public virtual MiningFacility Create(PlayerId owner, MiningFacilityType type, Vector3 position) =>
            Create(owner, type, position, _ids.GenerateUniqueId());

        /// <summary>Creates a facility with a known id, e.g. one restored from a save.</summary>
        public MiningFacility Create(PlayerId owner, MiningFacilityType type, Vector3 position, long entityId)
        {
            string assetName = MiningFacilityAssets.GetName(type);
            MiningFacilityData data = Assets.Load<MiningFacilityData>(assetName + "Data");
            return Spawn(Assets.LoadComponent<MiningFacility>(assetName + "View"), position, Quaternion.identity,
                (builder, view) => new EmpireAtWar.MiningFacility.MiningFacilityInstaller(view, data,
                    _localPlayer, owner, entityId, position, type).Install(builder));
        }
    }
}
