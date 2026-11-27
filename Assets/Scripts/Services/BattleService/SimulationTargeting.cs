using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    /// <summary>
    /// Orders each simulation unit to attack its nearest living enemy, retargeting when that enemy dies; or, with
    /// attack-move on, sends each side's idle units as one attack-move group to the point between both fleets.
    /// </summary>
    public sealed class SimulationTargeting
    {
        private readonly IUnitOrderService _orders;
        private readonly Dictionary<long, long> _targets = new();
        private readonly HashSet<long> _livingIds = new();

        public SimulationTargeting(IUnitOrderService orders) => _orders = orders;

        private readonly List<IEntity> _idle = new();

        public void Clear() { _targets.Clear(); _livingIds.Clear(); }

        /// <summary>
        /// Units without an order attack-move together to the midpoint of both fleets' centres, so the two groups meet
        /// in the middle: one group order, so the formation rows and slot hold are tested in isolation. A unit gets a
        /// new order once its attack-move ends.
        /// </summary>
        public void CommandAttackMove(IReadOnlyList<IEntity> own, IReadOnlyList<IEntity> opponents)
        {
            if (own.Count == 0 || opponents.Count == 0) return;
            _idle.Clear();
            for (int i = 0; i < own.Count; i++)
                if (own[i].GetFacade<IUnitOrderObserverFacade>().CurrentOrder == UnitOrderType.None)
                    _idle.Add(own[i]);
            if (_idle.Count == 0) return;
            _orders.IssueAttackMove(_idle, (Center(own) + Center(opponents)) * 0.5f);
        }

        public void Command(IReadOnlyList<IEntity> own, IReadOnlyList<IEntity> opponents)
        {
            if (own.Count == 0 || opponents.Count == 0) return;
            _livingIds.Clear();
            for (int i = 0; i < opponents.Count; i++) _livingIds.Add(opponents[i].Id);
            Dictionary<IEntity, List<IEntity>> groups = null;
            for (int i = 0; i < own.Count; i++)
            {
                IEntity entity = own[i];
                if (_targets.TryGetValue(entity.Id, out long target) && _livingIds.Contains(target)) continue;
                IEntity nearest = Nearest(entity, opponents);
                groups ??= new Dictionary<IEntity, List<IEntity>>();
                if (!groups.TryGetValue(nearest, out List<IEntity> receivers))
                    groups.Add(nearest, receivers = new List<IEntity>());
                receivers.Add(entity);
            }
            if (groups == null) return;
            // A direct attack follows the enemy, so no order points at a position the enemy has left.
            foreach (var group in groups)
            {
                _orders.IssueAttack(group.Value, group.Key);
                foreach (IEntity entity in group.Value) _targets[entity.Id] = group.Key.Id;
            }
        }

        private static Vector3 Center(IReadOnlyList<IEntity> units)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < units.Count; i++) sum += units[i].GetFacade<IEntityTransformFacade>().Transform.position;
            return sum / units.Count;
        }

        private static IEntity Nearest(IEntity entity, IReadOnlyList<IEntity> opponents)
        {
            Vector3 origin = entity.GetFacade<IEntityTransformFacade>().Transform.position;
            IEntity nearest = opponents[0];
            float distance = (nearest.GetFacade<IEntityTransformFacade>().Transform.position - origin).sqrMagnitude;
            for (int i = 1; i < opponents.Count; i++)
            {
                IEntity enemy = opponents[i];
                float candidate = (enemy.GetFacade<IEntityTransformFacade>().Transform.position - origin).sqrMagnitude;
                if (candidate >= distance) continue;
                nearest = enemy;
                distance = candidate;
            }
            return nearest;
        }
    }
}
