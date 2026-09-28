using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SuperWeapons;
using EmpireAtWar.Models.Economy;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.ReinforcementZones;
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
    }

    public sealed class CheatService : ICheatService
    {
        private readonly EconomyModel _economyModel;
        private readonly PlayerSlot _owner;
        private readonly ReinforcementModel _reinforcementModel;
        private readonly ShipFactory _shipFactory;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly IEntityLocator _entityLocator;
        private readonly SuperWeaponModel _superWeaponModel;
        private readonly RangeDebugModel _rangeDebugModel;

        public CheatService(
            EconomyModel economyModel,
            ReinforcementModel reinforcementModel,
            ShipFactory shipFactory,
            IReinforcementZonesSystem reinforcementZonesSystem,
            IEntityLocator entityLocator,
            SuperWeaponModel superWeaponModel,
            RangeDebugModel rangeDebugModel,
            PlayerSlot owner)
        {
            _owner = owner;
            _economyModel = economyModel;
            _reinforcementModel = reinforcementModel;
            _shipFactory = shipFactory;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _entityLocator = entityLocator;
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

            if (!_entityLocator.IsStationOperational(_owner.Id))
            {
                return false;
            }

            if (!_reinforcementZonesSystem.TryGetDefaultSpawnPosition(
                    _owner.Id,
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

        private void HandleShipDestroying(ShipType shipType)
        {
            _reinforcementModel.RemoveUnitCapacity(shipType);
        }
    }
}
