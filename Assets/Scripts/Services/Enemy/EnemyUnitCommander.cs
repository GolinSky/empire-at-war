using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public class EnemyUnitCommander : IInitializable, ILateDisposable
    {
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
                AssignCaptureOrder(ship);
            }
        }

        private void IssueCaptureOrders()
        {
            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (ship.PlayerType == PlayerType.Opponent)
                {
                    AssignCaptureOrder(ship);
                }
            }
        }

        private void AssignCaptureOrder(IShipEntity ship)
        {
            if (_reinforcementZonesSystem.TryGetCaptureTarget(
                    PlayerType.Opponent,
                    ship.WorldPosition,
                    out Vector3 target))
            {
                ship.AssignMoveTarget(target);
                return;
            }

            ship.HoldPosition();
        }
    }
}
