using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.EnemyFaction.Models;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ship;
using UnityEngine;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class EnemyPatrolShipController
    {
        // After "no safe target" the corvette fights with the fleet for a while: re-assigning it on the next decision
        // flipped its destination between the patrol point and the fleet order twice a second.
        private const float NO_TARGET_RETRY_SECONDS = 10f;

        private readonly EnemyPatrolService _patrol;
        private readonly EnemyPatrolSafety _safety;
        private readonly EnemyFactionModel _faction;
        private readonly IEntityLocator _entities;
        private readonly IUnitOrderService _orders;
        private readonly PlayerSlot _owner;
        private IShipEntity _ship;
        private Vector3 _issuedTarget;
        private bool _hasIssuedTarget;
        private float _retryAt = float.NegativeInfinity;

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

        /// <summary>One corvette keeps patrolling through ordinary fights and sieges: its firepower barely changes
        /// a battle, while the map knowledge it keeps fresh feeds capture, raid and recovery choices.
        /// It joins the fleet only when the station is about to fall or it is the last ship.</summary>
        public IShipEntity Select(IReadOnlyList<IShipEntity> ships, bool stationCritical)
        {
            IShipEntity selected = CanPatrol(_owner.Difficulty, ships.Count, stationCritical) &&
                                   Time.time >= _retryAt
                ? FindCheapest(ships) : null;
            if (selected != _ship)
            {
                if (_ship != null)
                {
                    _patrol.Release($"ship:{_ship.EntityId}");
                    Log(_ship, "released", stationCritical ? "stationCritical" :
                        ships.Count <= 1 ? "lastShip" : "unavailable");
                }
                _ship = selected;
                _hasIssuedTarget = false;
                if (_ship != null) Log(_ship, "assigned", "cheapestCorvette");
            }
            if (_ship == null) return null;
            // No safe point left: the patrol still goes out to the stalest reachable point, as intel and bait.
            if (!_patrol.TryGetTarget($"ship:{_ship.EntityId}", _ship.WorldPosition,
                    point => _safety.IsObjectiveSafe(_ship.EntityId, _ship.WorldPosition, point, _ship.NavigationRadius, true), out var target) &&
                !_patrol.TryGetTarget($"ship:{_ship.EntityId}", _ship.WorldPosition,
                    point => _safety.IsReachable(_ship.WorldPosition, point, _ship.NavigationRadius), out target))
            {
                Log(_ship, "released", "noSafeTarget");
                _ship = null;
                _hasIssuedTarget = false;
                _retryAt = Time.time + NO_TARGET_RETRY_SECONDS;
                return null;
            }
            // Re-issuing the same attack-move every decision rebuilt the route and braked the ship each time.
            if (_hasIssuedTarget && _issuedTarget == target && _ship.CurrentOrder == UnitOrderType.AttackMove)
                return _ship;
            _orders.IssueAttackMove(new[] { _entities.GetEntity(_ship.EntityId) }, target);
            _issuedTarget = target;
            _hasIssuedTarget = true;
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

        public static bool CanPatrol(EnemyAiDifficulty difficulty, int shipCount, bool stationCritical) =>
            difficulty != EnemyAiDifficulty.Easy && shipCount > 1 && !stationCritical;

        private void Log(IShipEntity ship, string action, string reason) => EnemyAiTelemetry.RecordEvent(
            _owner.Id, _owner.Faction, "patrolShip", $"ship={ship.EntityId};action={action};reason={reason}");
    }
}
