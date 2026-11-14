using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyPatrolShipController
    {
        private readonly EnemyPatrolService _patrol;
        private readonly EnemyPatrolSafety _safety;
        private readonly EnemyFactionModel _faction;
        private readonly IEntityLocator _entities;
        private readonly IUnitOrderService _orders;
        private readonly PlayerSlot _owner;
        private IShipEntity _ship;

        public EnemyPatrolShipController(EnemyPatrolService patrol, EnemyPatrolSafety safety,
            EnemyFactionModel faction, IEntityLocator entities, IUnitOrderService orders, PlayerSlot owner)
        {
            _patrol = patrol;
            _safety = safety;
            _faction = faction;
            _entities = entities;
            _orders = orders;
            _owner = owner;
        }

        public IShipEntity Select(IReadOnlyList<IShipEntity> ships, bool stationPressure, bool taskForceNeedsShip,
            bool recovery)
        {
            IShipEntity selected = CanPatrol(_owner.Difficulty, ships.Count, stationPressure, taskForceNeedsShip, recovery)
                ? FindCheapest(ships) : null;
            if (selected != _ship)
            {
                if (_ship != null)
                {
                    _patrol.Release($"ship:{_ship.EntityId}");
                    Log(_ship, "released", stationPressure ? "stationDefense" : recovery ? "recovery" :
                        taskForceNeedsShip ? "taskForce" : "unavailable");
                }
                _ship = selected;
                if (_ship != null) Log(_ship, "assigned", "cheapestCorvette");
            }
            if (_ship == null) return null;
            if (!_patrol.TryGetTarget($"ship:{_ship.EntityId}", _ship.WorldPosition,
                    point => _safety.IsObjectiveSafe(_ship.EntityId, _ship.WorldPosition, point, _ship.NavigationRadius, true), out var target))
            {
                Log(_ship, "released", "noSafeTarget");
                _ship = null;
                return null;
            }
            _orders.IssueAttackMove(new[] { _entities.GetEntity(_ship.EntityId) }, target);
            return _ship;
        }

        private IShipEntity FindCheapest(IReadOnlyList<IShipEntity> ships)
        {
            IShipEntity selected = null;
            float cheapest = float.MaxValue;
            foreach (IShipEntity ship in ships)
            {
                IEntity entity = _entities.GetEntity(ship.EntityId);
                if (entity.HealthModel.ShipClass != ShipClass.Corvette ||
                    !_faction.ShipFactionData.TryGetValue(entity.GetFacade<IUnitTypeFacade>().UnitTypeId.ShipType, out var data) ||
                    data.Price > cheapest) continue;
                if (data.Price == cheapest && selected != null && ship != _ship) continue;
                cheapest = data.Price;
                selected = ship;
            }
            return selected;
        }

        public static bool CanPatrol(EnemyAiDifficulty difficulty, int shipCount, bool stationPressure,
            bool taskForceNeedsShip, bool recovery) => difficulty != EnemyAiDifficulty.Easy && shipCount > 1 &&
                !stationPressure && !taskForceNeedsShip && !recovery;

        private void Log(IShipEntity ship, string action, string reason) => EnemyAiTelemetry.RecordEvent(
            _owner.Id, _owner.Faction, "patrolShip", $"ship={ship.EntityId};action={action};reason={reason}");
    }
}
