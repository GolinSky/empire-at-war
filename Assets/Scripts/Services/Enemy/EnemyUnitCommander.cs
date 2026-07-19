using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Ship;
using Zenject;

namespace EmpireAtWar.Services.Enemy
{
    public class EnemyUnitCommander : IInitializable, ILateDisposable
    {
        private readonly IShipService _shipService;
        private readonly IEntityLocator _entityLocator;
        private IEntity _mainTarget;

        public EnemyUnitCommander(IShipService shipService, IEntityLocator entityLocator)
        {
            _shipService = shipService;
            _entityLocator = entityLocator;
        }

        public void Initialize()
        {
            _shipService.ShipAdded += HandleShipAdded;
            _entityLocator.EntityAdded += HandleEntityAdded;
            _entityLocator.EntityRemoved += HandleEntityRemoved;

            FindMainTarget();
            IssueAttackOrders();
        }

        public void LateDispose()
        {
            _shipService.ShipAdded -= HandleShipAdded;
            _entityLocator.EntityAdded -= HandleEntityAdded;
            _entityLocator.EntityRemoved -= HandleEntityRemoved;
        }

        private void HandleShipAdded(IShipEntity ship)
        {
            if (_mainTarget != null && ship.PlayerType == PlayerType.Opponent)
            {
                ship.AssignAttackTarget(_mainTarget);
            }
        }

        private void HandleEntityAdded(IEntity entity)
        {
            if (_mainTarget == null && IsPlayerSpaceStation(entity))
            {
                _mainTarget = entity;
                IssueAttackOrders();
            }
        }

        private void HandleEntityRemoved(IEntity entity)
        {
            if (_mainTarget != entity)
            {
                return;
            }

            _mainTarget = null;
            FindMainTarget();
            IssueAttackOrders();
        }

        private void FindMainTarget()
        {
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (IsPlayerSpaceStation(entity))
                {
                    _mainTarget = entity;
                    return;
                }
            }
        }

        private void IssueAttackOrders()
        {
            if (_mainTarget == null)
            {
                return;
            }

            foreach (IShipEntity ship in _shipService.Ships)
            {
                if (ship.PlayerType == PlayerType.Opponent)
                {
                    ship.AssignAttackTarget(_mainTarget);
                }
            }
        }

        private static bool IsPlayerSpaceStation(IEntity entity)
        {
            return entity.PlayerType == PlayerType.Player && entity.Model is ISpaceStationModelObserver;
        }
    }
}
