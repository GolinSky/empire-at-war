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
        private readonly SecondStationLevelModel _secondStationLevel;

        public SpaceStationFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ILocalPlayer localPlayer,
            IUniqueIdGenerator ids, SecondStationLevelModel secondStationLevel) : base(assets, scope, ownership)
        {
            _localPlayer = localPlayer;
            _ids = ids;
            _secondStationLevel = secondStationLevel;
        }

        public virtual SpaceStation Create(PlayerId owner, FactionType factionType, Vector3 position) =>
            Create(owner, factionType, position, _ids.GenerateUniqueId());

        /// <summary>Creates a station with a known id, e.g. one restored from a save.</summary>
        public SpaceStation Create(PlayerId owner, FactionType factionType, Vector3 position, long entityId) =>
            Create(owner, factionType, position, entityId, null);

        /// <summary>A bought second station; it uses the owner's second-station level.</summary>
        public SpaceStation CreateSecond(PlayerId owner, FactionType factionType, Vector3 position)
        {
            _secondStationLevel.ResetForNewStation();
            return CreateSecond(owner, factionType, position, _ids.GenerateUniqueId());
        }

        public SpaceStation CreateSecond(PlayerId owner, FactionType factionType, Vector3 position, long entityId) =>
            Create(owner, factionType, position, entityId, _secondStationLevel);

        private SpaceStation Create(PlayerId owner, FactionType factionType, Vector3 position, long entityId,
            SecondStationLevelModel secondStationLevel)
        {
            SpaceStationData data = Assets.Load<SpaceStationData>(nameof(SpaceStationData));
            return Spawn(Assets.LoadComponent<SpaceStation>(factionType + "SpaceStationView"), position, Quaternion.identity,
                (builder, view) => new EmpireAtWar.SpaceStation.SpaceStationInstaller(view, data, _localPlayer,
                    owner, entityId, position, factionType, secondStationLevel).Install(builder));
        }
    }
}
