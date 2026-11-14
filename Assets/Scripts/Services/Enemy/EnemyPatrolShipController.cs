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

        /// <summary>One corvette keeps patrolling through ordinary fights and sieges: its firepower barely changes
        /// a battle, while the map knowledge it keeps fresh feeds capture, raid and recovery choices.
        /// It joins the fleet only when the station is about to fall or it is the last ship.</summary>
        public IShipEntity Select(IReadOnlyList<IShipEntity> ships, bool stationCritical)
        {
            IShipEntity selected = CanPatrol(_owner.Difficulty, ships.Count, stationCritical)
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

        public static bool CanPatrol(EnemyAiDifficulty difficulty, int shipCount, bool stationCritical) =>
            difficulty != EnemyAiDifficulty.Easy && shipCount > 1 && !stationCritical;

        private void Log(IShipEntity ship, string action, string reason) => EnemyAiTelemetry.RecordEvent(
            _owner.Id, _owner.Faction, "patrolShip", $"ship={ship.EntityId};action={action};reason={reason}");
    }
}
