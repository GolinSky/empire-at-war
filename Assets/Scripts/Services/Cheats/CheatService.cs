using System;
using EmpireAtWar.Controllers.Factions;
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
    }

    public sealed class CheatService : ICheatService
    {
        private readonly EconomyModel _economyModel;
        private readonly ReinforcementModel _reinforcementModel;
        private readonly ShipFacadeFactory _shipFacadeFactory;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;

        public CheatService(
            EconomyModel economyModel,
            ReinforcementModel reinforcementModel,
            ShipFacadeFactory shipFacadeFactory,
            IReinforcementZonesSystem reinforcementZonesSystem)
        {
            _economyModel = economyModel ?? throw new ArgumentNullException(nameof(economyModel));
            _reinforcementModel = reinforcementModel ??
                throw new ArgumentNullException(nameof(reinforcementModel));
            _shipFacadeFactory = shipFacadeFactory ??
                throw new ArgumentNullException(nameof(shipFacadeFactory));
            _reinforcementZonesSystem = reinforcementZonesSystem ??
                throw new ArgumentNullException(nameof(reinforcementZonesSystem));
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

            if (!_reinforcementZonesSystem.TryGetDefaultSpawnPosition(
                    PlayerType.Player,
                    out Vector3 spawnPosition))
            {
                return false;
            }

            _reinforcementModel.UpdateShipData(request);
            ShipEntity ship = _shipFacadeFactory.Create(PlayerType.Player, request.Key, spawnPosition);
            if (ship == null)
            {
                throw new InvalidOperationException($"Failed to create ship {request.Key}.");
            }

            ship.OnRelease += HandleShipDestroying;
            _reinforcementModel.AddUnitCapacity(request.Key);
            return true;
        }

        private void HandleShipDestroying(ShipType shipType)
        {
            _reinforcementModel.RemoveUnitCapacity(shipType);
        }
    }
}
