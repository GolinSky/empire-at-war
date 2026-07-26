using System.Collections.Generic;
using EmpireAtWar.Components.Movement.Formation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public class EnemyUnitCommander : IInitializable, ILateDisposable
    {
        private const float FORMATION_SPACING = 12f;

        private readonly IShipService _shipService;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;

        public EnemyUnitCommander(
            IShipService shipService,
            IReinforcementZonesSystem reinforcementZonesSystem)
        {
            _shipService = shipService;
            _reinforcementZonesSystem = reinforcementZonesSystem;
        }

        public void Initialize()
        {
            _shipService.ShipAdded += HandleShipAdded;
            _reinforcementZonesSystem.OwnershipChanged += IssueCaptureOrders;
            IssueCaptureOrders();
        }

        public void LateDispose()
        {
            _shipService.ShipAdded -= HandleShipAdded;
            _reinforcementZonesSystem.OwnershipChanged -= IssueCaptureOrders;
        }

        private void HandleShipAdded(IShipEntity ship)
        {
            if (ship.PlayerType == PlayerType.Opponent)
            {
                IssueCaptureOrders();
            }
        }

        private void IssueCaptureOrders()
        {
            List<IShipEntity> ships = new List<IShipEntity>();
            List<FormationPoint> positions = new List<FormationPoint>();
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (ship.PlayerType == PlayerType.Opponent)
                {
                    ships.Add(ship);
                    positions.Add(new FormationPoint(ship.WorldPosition.x, ship.WorldPosition.z));
                }
            }

            if (ships.Count == 0)
            {
                return;
            }

            FormationPoint fleetCenter = FormationModel.CalculateCenter(positions);
            if (_reinforcementZonesSystem.TryGetCaptureTarget(
                    PlayerType.Opponent,
                    new Vector3(fleetCenter.X, 0f, fleetCenter.Z),
                    out Vector3 target))
            {
                FormationPoint targetCenter = new FormationPoint(target.x, target.z);
                for (int i = 0; i < ships.Count; i++)
                {
                    FormationPoint destination = FormationModel.CalculateGridDestination(
                        i,
                        ships.Count,
                        targetCenter,
                        FORMATION_SPACING);
                    ships[i].AssignMoveTarget(new Vector3(destination.X, 0f, destination.Z));
                }

                return;
            }

            foreach (IShipEntity ship in ships)
            {
                ship.HoldPosition();
            }
        }
    }
}
