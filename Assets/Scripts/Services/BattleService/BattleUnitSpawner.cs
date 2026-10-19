using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Movement;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Squadrons;
using EmpireAtWar.Ship;
using UnityEngine;
using DefendPlatformEntity = EmpireAtWar.Entities.DefendPlatform.DefendPlatform;
using MiningFacilityEntity = EmpireAtWar.Entities.MiningFacility.MiningFacility;
using SpaceStationEntity = EmpireAtWar.Entities.SpaceStation.SpaceStation;

namespace EmpireAtWar.Services.Battle
{
    public sealed class BattleUnitSpawner
    {
        private readonly Dictionary<PlayerId, (ShipFactory Ships, ISquadronSpawner Squadrons)> _players = new();
        private readonly Dictionary<PlayerId, StructureFactories> _structures = new();
        public void Register(PlayerId owner, ShipFactory ships, ISquadronSpawner squadrons, StructureFactories structures)
        {
            _players.Add(owner, (ships, squadrons));
            _structures.Add(owner, structures);
        }

        public void Clear()
        {
            _players.Clear();
            _structures.Clear();
        }

        public void Spawn(PlayerId owner, UnitTypeId unit, Vector3 position)
        {
            var factories = _players[owner];
            if (unit.IsShip) factories.Ships.Create(owner, unit.ShipType, position);
            else factories.Squadrons.Spawn(owner, unit.SquadronType, position, Quaternion.identity);
        }

        /// <summary>Places a saved ship with its saved id and facing, already arrived.</summary>
        public Ship.Ship SpawnRestoredShip(PlayerId owner, ShipType shipType, Vector3 position, Quaternion rotation,
            long entityId) =>
            _players[owner].Ships.Create(owner, shipType, position, entityId, ShipSpawnPose.Restored(rotation));

        public Squadron SpawnRestoredSquadron(PlayerId owner, SquadronType squadronType, Vector3 position,
            Quaternion rotation, long entityId) =>
            _players[owner].Squadrons.SpawnRestored(owner, squadronType, position, rotation, entityId);

        public SpaceStationEntity SpawnRestoredStation(PlayerId owner, FactionType faction, Vector3 position, long entityId) =>
            _structures[owner].Stations.Create(owner, faction, position, entityId);

        // Capture sites build asteroid miners; every other facility uses the standard prefab.
        public MiningFacilityEntity SpawnRestoredMiningFacility(PlayerId owner, MiningFacilityType type, Vector3 position,
            long entityId) => type == MiningFacilityType.AsteroidMiner
            ? _structures[owner].AsteroidMiningFacilities.Create(owner, type, position, entityId)
            : _structures[owner].MiningFacilities.Create(owner, type, position, entityId);

        public DefendPlatformEntity SpawnRestoredDefendPlatform(PlayerId owner, DefendPlatformType type, Vector3 position,
            long entityId) => type == DefendPlatformType.BattleAsteroid
            ? _structures[owner].AsteroidDefendPlatforms.Create(owner, type, position, entityId)
            : _structures[owner].DefendPlatforms.Create(owner, type, position, entityId);
    }
}
