using System.Collections.Generic;
using System.Linq;
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

        public SimulationTargeting(IUnitOrderService orders) => _orders = orders;

        public void Clear() => _targets.Clear();

        public void Command(IReadOnlyList<IEntity> own, IReadOnlyList<IEntity> opponents)
        {
            if (opponents.Count == 0) return;
            var livingIds = new HashSet<long>(opponents.Select(entity => entity.Id));
            var unassigned = own.Where(entity => !_targets.TryGetValue(entity.Id, out long target) || !livingIds.Contains(target));
            // A direct attack follows the enemy, so no order points at a position the enemy has left.
            foreach (var group in unassigned.GroupBy(entity => Nearest(entity, opponents)).ToArray())
            {
                _orders.IssueAttack(group.ToArray(), group.Key);
                foreach (IEntity entity in group) _targets[entity.Id] = group.Key.Id;
            }
        }

        private static IEntity Nearest(IEntity entity, IReadOnlyList<IEntity> opponents)
        {
            Vector3 origin = entity.GetFacade<IEntityTransformFacade>().Transform.position;
            return opponents.OrderBy(enemy => (enemy.GetFacade<IEntityTransformFacade>().Transform.position - origin).sqrMagnitude).First();
        }
    }
}
