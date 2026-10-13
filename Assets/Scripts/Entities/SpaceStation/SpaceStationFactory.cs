using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using VContainer.Unity;

namespace EmpireAtWar.Entities.SpaceStation
{
    public class SpaceStationFactory : BaseFactory
    {
        private readonly ILocalPlayer _localPlayer;
        private readonly IUniqueIdGenerator _ids;

        public SpaceStationFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
        }

        public virtual SpaceStation Create(PlayerId owner, FactionType factionType, Vector3 position)
        {
            SpaceStationData data = Assets.Load<SpaceStationData>(nameof(SpaceStationData));
            long entityId = _ids.GenerateUniqueId();
            return Spawn(Assets.LoadComponent<SpaceStation>(factionType + "SpaceStationView"), position, Quaternion.identity,
                (builder, view) => new EmpireAtWar.SpaceStation.SpaceStationInstaller(view, data, _localPlayer,
                    owner, entityId, position, factionType).Install(builder));
        }
    }
}
