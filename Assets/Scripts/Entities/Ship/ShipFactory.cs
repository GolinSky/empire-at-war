using System;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.VContainer;
using EmpireAtWar.Entities.Ship.Data;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Factory;
using EmpireAtWar.Services.IdGeneration;
using UnityEngine;
using VContainer.Unity;

namespace EmpireAtWar.Ship
{
    public class ShipFactory : BaseFactory
    {
        private readonly ShipsData _shipsData;
        private readonly ILocalPlayer _localPlayer;
        private readonly IUniqueIdGenerator _ids;
        private readonly FactionCatalog _factions;
        private readonly IPlayerRoster _players;

        public ShipFactory(IAssetService assets, LifetimeScope scope, EntityScopeOwnership ownership, ShipsData shipsData,
            ILocalPlayer localPlayer, IUniqueIdGenerator ids, FactionCatalog factions, IPlayerRoster players)
            : base(assets, scope, ownership)
        {
            _shipsData = shipsData;
            _localPlayer = localPlayer;
            _ids = ids;
            _factions = factions;
            _players = players;
        }

        public virtual Ship Create(PlayerId owner, ShipType shipType, Vector3 position) =>
            Create(owner, shipType, position, _ids.GenerateUniqueId(), ShipSpawnPose.Hyperspace);

        /// <summary>Jumps a ship in with the heading the player chose.</summary>
        public Ship Create(PlayerId owner, ShipType shipType, Vector3 position, Quaternion rotation) =>
            Create(owner, shipType, position, _ids.GenerateUniqueId(), ShipSpawnPose.HyperspaceFacing(rotation));

        /// <summary>Creates a ship with a known id; a restored pose places it without a hyperspace jump.</summary>
        public Ship Create(PlayerId owner, ShipType shipType, Vector3 position, long entityId, ShipSpawnPose pose)
        {
            ShipData data = Assets.Load<ShipData>(_shipsData.GetShipDataPath(shipType));
            FactionType shipFaction = _factions.GetShipFaction(shipType, _players.Get(owner).Faction);
            return Spawn(Assets.LoadComponent<Ship>(shipType + "ShipView"), position, Quaternion.identity,
                (builder, view) => new ShipInstaller(view, data, _localPlayer, owner, entityId, position, shipType, pose,
                    shipFaction).Install(builder));
        }
    }
}
