using System;
using System.Collections.Generic;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    /// <summary>
    /// Shared by every unit of one attack-move order. Pools what the group detects into one
    /// engagement set and counts attackers per enemy so the group spreads across the enemy formation
    /// instead of focusing a single target.
    /// </summary>
    public sealed class AttackMoveEngagement
    {
        private readonly Dictionary<int, List<IEntity>> _contacts = new Dictionary<int, List<IEntity>>();
        private readonly Dictionary<int, IEntity> _claims = new Dictionary<int, IEntity>();

        private int _nextMemberId;

        public int Join()
        {
            int member = _nextMemberId++;
            _contacts.Add(member, new List<IEntity>());
            return member;
        }

        public void Leave(int member)
        {
            _contacts.Remove(member);
            _claims.Remove(member);
        }

        public void Report(int member, IEnumerable<IEntity> detected)
        {
            List<IEntity> contacts = _contacts[member];
            contacts.Clear();
            foreach (IEntity enemy in detected)
                if (IsAlive(enemy)) contacts.Add(enemy);
        }

        public bool Contains(IEntity enemy)
        {
            if (!IsAlive(enemy)) return false;
            foreach (List<IEntity> contacts in _contacts.Values)
                if (contacts.Contains(enemy)) return true;
            return false;
        }

        /// <summary>
        /// Claims the live enemy with the fewest other attackers, nearest first on ties.
        /// Returns null and drops the member's claim when the engagement set is empty.
        /// </summary>
        public IEntity SelectTarget(int member, Func<IEntity, float> distance)
        {
            _claims.Remove(member);
            IEntity best = null;
            int fewestAttackers = int.MaxValue;
            float nearest = float.PositiveInfinity;
            foreach (List<IEntity> contacts in _contacts.Values)
            {
                foreach (IEntity enemy in contacts)
                {
                    if (!IsAlive(enemy)) continue;
                    int attackers = AttackersOf(enemy);
                    if (attackers > fewestAttackers) continue;
                    float range = distance(enemy);
                    if (attackers == fewestAttackers && range >= nearest) continue;
                    best = enemy;
                    fewestAttackers = attackers;
                    nearest = range;
                }
            }

            if (best != null) _claims[member] = best;
            return best;
        }

        private int AttackersOf(IEntity enemy)
        {
            int attackers = 0;
            foreach (IEntity claimed in _claims.Values)
                if (claimed == enemy) attackers++;
            return attackers;
        }

        private static bool IsAlive(IEntity enemy) =>
            !enemy.HealthModel.IsDestroyed && enemy.HealthModel.HasUnits;
    }
}
