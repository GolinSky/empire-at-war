using System.Collections.Generic;
using EmpireAtWar.Mvc;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.IdGeneration;
using EmpireAtWar.MiningFacility;
using UnityEngine;
using VContainer.Unity;

namespace EmpireAtWar.Entities.MiningFacility
{
    public class MiningFacilityFactory : BaseFactory
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly IUniqueIdGenerator _ids;
        private readonly MiningFacilityCatalog _catalog;
        private readonly IPlayerRoster _roster;
        private readonly IReadOnlyList<ICaptureSiteDirectory> _sites;
        private readonly IGameModelObserver _game;

        public MiningFacilityFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids, MiningFacilityCatalog catalog, IPlayerRoster roster,
            IReadOnlyList<ICaptureSiteDirectory> sites, IGameModelObserver game) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
            _catalog = catalog;
            _roster = roster;
            _sites = sites;
            _game = game;
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
            float income = GetIncome(type, data);
            return Spawn(Assets.LoadComponent<MiningFacility>(assetName + "View"), position, Quaternion.identity,
                (builder, view) => new MiningFacilityInstaller(view, data,
                    _localPlayer, owner, entityId, position, type, income, buildTime).Install(builder));
        }

        /// <summary>
        /// A mine on a capture site pays its share of the map's <see cref="MiningPool"/>, so income per player is the
        /// same on every map size and player count. Station miners, and mines in Survival or on a map without mining
        /// spots, pay the income of their data.
        /// </summary>
        private float GetIncome(MiningFacilityType type, MiningFacilityData data)
        {
            if (type != MiningFacilityType.AsteroidMiner || _game.GameMode == GameMode.Survival) return data.Income;
            int spots = 0;
            foreach (ICaptureSiteDirectory directory in _sites)
                foreach (ICaptureSite site in directory.Sites)
                    if (site.Kind == CaptureSiteKind.Mining) spots++;
            return spots == 0 ? data.Income : MiningPool.GetSpotIncome(_roster.Players.Count, spots);
        }
    }
}
