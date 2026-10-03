using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.ShipSpawning;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Ship;
using UnityEngine;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Services.Cheats
{
    public interface ICheatService
    {
        void AddMoney(float amount);

        void AddShipReinforcement(ShipUnitRequest request);

        bool ForceSpawnShipAtDefaultZone(ShipUnitRequest request);

        bool GrantSuperWeapon(SuperWeaponType type);

        void SetRangeDebug(bool isEnabled);

        int DestroyOwnShips();
    }

    public sealed class CheatService : ICheatService
    {
        private readonly IShipSpawnPoints _shipSpawnPoints;
        private readonly IEntityLocator _entityLocator;
        private readonly IStationRegistry _stationRegistry;

        private readonly EconomyModel _economyModel;
        private readonly PlayerSlot _owner;
        private readonly ReinforcementModel _reinforcementModel;
        private readonly ShipFactory _shipFactory;
        private readonly SuperWeaponModel _superWeaponModel;
        private readonly RangeDebugModel _rangeDebugModel;
        private readonly List<IEntity> _ownEntities = new List<IEntity>();

        public CheatService(
            IShipSpawnPoints shipSpawnPoints,
            IEntityLocator entityLocator,
            IStationRegistry stationRegistry,
            EconomyModel economyModel,
            ReinforcementModel reinforcementModel,
            ShipFactory shipFactory,
            SuperWeaponModel superWeaponModel,
            RangeDebugModel rangeDebugModel,
            PlayerSlot owner)
        {
            _owner = owner;
            _economyModel = economyModel;
            _reinforcementModel = reinforcementModel;
            _shipFactory = shipFactory;
            _shipSpawnPoints = shipSpawnPoints;
            _entityLocator = entityLocator;
            _stationRegistry = stationRegistry;
            _superWeaponModel = superWeaponModel;
            _rangeDebugModel = rangeDebugModel;
        }

        public void AddMoney(float amount)
        {
            if (amount <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            _economyModel.AddMoney(amount);
        }

        public void AddShipReinforcement(ShipUnitRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            _reinforcementModel.UpdateShipData(request);
            _reinforcementModel.AddReinforcement(request);
        }

        public bool ForceSpawnShipAtDefaultZone(ShipUnitRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (!_stationRegistry.IsStationOperational(_owner.Id))
            {
                return false;
            }

            if (!_shipSpawnPoints.TryGetDefaultZoneSpawnPosition(
                    _owner.Id,
                    request.Key,
                    out Vector3 spawnPosition))
            {
                return false;
            }

            _reinforcementModel.UpdateShipData(request);
            ShipEntity ship = _shipFactory.Create(_owner.Id, request.Key, spawnPosition);
            if (ship == null)
            {
                throw new InvalidOperationException($"Failed to create ship {request.Key}.");
            }

            ship.OnRelease += HandleShipDestroying;
            _reinforcementModel.AddUnitCapacity(request.Key);
            return true;
        }

        /// <summary>Makes the weapon ready to fire for free. A weapon already charging or ready is left alone.</summary>
        public bool GrantSuperWeapon(SuperWeaponType type)
        {
            if (!_superWeaponModel.CanPurchase(type))
            {
                return false;
            }

            _superWeaponModel.StartCharging(type);
            _superWeaponModel.CompleteCharging(type);
            return true;
        }

        public void SetRangeDebug(bool isEnabled)
        {
            _rangeDebugModel.IsEnabled = isEnabled;
        }

        /// <summary>Destroys every ship the player owns. Returns how many were destroyed.</summary>
        public int DestroyOwnShips()
        {
            // Copied first: destroyed ships leave the locator while we iterate.
            _ownEntities.Clear();
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Owner == _owner.Id) _ownEntities.Add(entity);
            }

            int destroyed = 0;
            foreach (IEntity entity in _ownEntities)
            {
                if (!entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IEntityDestroyFacade destroyFacade))
                {
                    destroyFacade.Destroy();
                    destroyed++;
                }
            }

            _ownEntities.Clear();
            return destroyed;
        }

        private void HandleShipDestroying(ShipType shipType)
        {
            _reinforcementModel.RemoveUnitCapacity(shipType);
        }
    }
}
