using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    /// <summary>Orders each simulation unit to attack its nearest living enemy, retargeting when that enemy dies.</summary>
    public sealed class SimulationTargeting
    {
        private readonly IUnitOrderService _orders;
        private readonly Dictionary<long, long> _targets = new();
        private readonly HashSet<long> _livingIds = new();

        public SimulationTargeting(IUnitOrderService orders) => _orders = orders;

        public void Clear() { _targets.Clear(); _livingIds.Clear(); }

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
