using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Ship;
using UnityEngine;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using ShipEntity = EmpireAtWar.Ship.Ship;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Services.Battle
{
    /// <summary>
    /// Routes battle-scope callers to each player's own unit factories; the factories create the units.
    /// Code inside a player scope calls its factories directly.
    /// </summary>
    public sealed class PlayerFactoryLookup
    {
        private readonly Dictionary<PlayerId, PlayerUnitFactories> _players = new();

        public void Register(PlayerId owner, PlayerUnitFactories factories) => _players.Add(owner, factories);

        /// <summary>An AI takeover rebuilds the player's scope and registers its new factories.</summary>
        public void Unregister(PlayerId owner) => _players.Remove(owner);

        public void Clear() => _players.Clear();

        public void Spawn(PlayerId owner, UnitTypeId unit, Vector3 position)
        {
            PlayerUnitFactories factories = _players[owner];
            if (unit.IsShip) factories.Ships.Create(owner, unit.ShipType, position);
            else factories.Squadrons.Spawn(owner, unit.SquadronType, position, Quaternion.identity);
        }

        /// <summary>Places a saved ship with its saved id and facing, already arrived.</summary>
        public ShipEntity SpawnRestoredShip(PlayerId owner, ShipType shipType, Vector3 position, Quaternion rotation,
            long entityId) =>
            _players[owner].Ships.Create(owner, shipType, position, entityId, ShipSpawnPose.Restored(rotation));

        /// <summary>Online client: a host ship still jumping in; it lands at <paramref name="position"/>.</summary>
        public ShipEntity SpawnArrivingShip(PlayerId owner, ShipType shipType, Vector3 position, long entityId) =>
            _players[owner].Ships.Create(owner, shipType, position, entityId, ShipSpawnPose.Hyperspace);

        public Squadron SpawnRestoredSquadron(PlayerId owner, SquadronType squadronType, Vector3 position,
            Quaternion rotation, long entityId) =>
            _players[owner].Squadrons.SpawnRestored(owner, squadronType, position, rotation, entityId);

        public SpaceStationEntity SpawnRestoredStation(PlayerId owner, FactionType faction, Vector3 position, long entityId) =>
            _players[owner].Stations.Create(owner, faction, position, entityId);

        public MiningFacilityEntity SpawnRestoredMiningFacility(PlayerId owner, MiningFacilityType type, Vector3 position,
            long entityId, float buildDuration) =>
            _players[owner].MiningFacilities.Create(owner, type, position, entityId, buildDuration);

        public DefendPlatformEntity SpawnRestoredDefendPlatform(PlayerId owner, DefendPlatformType type, Vector3 position,
            long entityId, float buildDuration) =>
            _players[owner].DefendPlatforms.Create(owner, type, position, entityId, buildDuration);
    }
}
