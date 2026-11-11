using System;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Networking.Messages;
using EmpireAtWar.Services.Saves;
using UnityEngine;

namespace EmpireAtWar.Services.Networking
{
    /// <summary>
    /// Client: creates the host's units from spawn payloads through the same restore factories a loaded save uses,
    /// keeping the host's entity id, then applies the host's health.
    /// </summary>
    public sealed class ReplicaEntitySpawner
    {
        private readonly PlayerFactoryLookup _factories;
        private readonly IEntityLocator _entities;

        public ReplicaEntitySpawner(PlayerFactoryLookup factories, IEntityLocator entities)
        {
            _factories = factories;
            _entities = entities;
        }

        public void Spawn(EntitySpawnMessage message)
        {
            switch (message.Kind)
            {
                case EntitySpawnKind.Untracked:
                    return;
                case EntitySpawnKind.Unit:
                    SpawnUnit(JsonUtility.FromJson<UnitSave>(message.Payload), message.IsArriving);
                    return;
                case EntitySpawnKind.Structure:
                    SpawnStructure(JsonUtility.FromJson<StructureSave>(message.Payload));
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(message), message.Kind, "Unknown spawn kind.");
            }
        }

        public void ApplyHealth(long entityId, UnitHealthSave health)
        {
            if (!_entities.TryGetEntity(entityId, out IEntity entity)) return;
            if (entity.TryGetFacade(out IUnitSaveFacade unit)) unit.ApplyReplicaHealth(health);
            else if (entity.TryGetFacade(out IStructureSaveFacade structure)) structure.ApplyReplicaHealth(health);
        }

        private void SpawnUnit(UnitSave unit, bool isArriving)
        {
            // The save holds the landing point; the client plays the jump so arrival effects and sounds run.
            if (unit.UnitType.IsShip && isArriving)
                _factories.SpawnArrivingShip(unit.Owner, unit.UnitType.ShipType, unit.Position, unit.Id);
            else if (unit.UnitType.IsShip)
                _factories.SpawnRestoredShip(unit.Owner, unit.UnitType.ShipType, unit.Position, unit.Rotation, unit.Id);
            else
                _factories.SpawnRestoredSquadron(unit.Owner, unit.UnitType.SquadronType, unit.Position, unit.Rotation,
                    unit.Id);
            _entities.GetEntity(unit.Id).GetFacade<IUnitSaveFacade>().RestoreHealth(unit.Health);
        }

        private void SpawnStructure(StructureSave structure)
        {
            switch (structure.Kind)
            {
                case StructureKind.SpaceStation:
                    _factories.SpawnRestoredStation(structure.Owner, structure.StationFaction, structure.Position,
                        structure.Id);
                    break;
                case StructureKind.SecondStation:
                    _factories.SpawnRestoredSecondStation(structure.Owner, structure.StationFaction, structure.Position,
                        structure.Id);
                    break;
                case StructureKind.MiningFacility:
                    _factories.SpawnRestoredMiningFacility(structure.Owner, structure.MiningFacilityType,
                        structure.Position, structure.Id, structure.ConstructionBuildTime);
                    break;
                case StructureKind.DefendPlatform:
                    _factories.SpawnRestoredDefendPlatform(structure.Owner, structure.DefendPlatformType,
                        structure.Position, structure.Id, structure.ConstructionBuildTime);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(structure), structure.Kind, "Unknown structure kind.");
            }

            IEntity entity = _entities.GetEntity(structure.Id);
            if (entity.TryGetFacade(out IConstructionFacade construction))
                construction.Restore(structure.ConstructionProgress, structure.ConstructionBuildTime);
            entity.GetFacade<IStructureSaveFacade>().RestoreHealth(structure.Health);
        }
    }
}
