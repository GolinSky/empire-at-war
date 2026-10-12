using System;
using System.Collections.Generic;

namespace EmpireAtWar.Entities.BaseEntity.Orders
{
    /// <summary>
    /// Shared by every unit of one attack-move order. Pools what the group detects into one
    /// engagement set and counts attackers per enemy so the group spreads across the enemy formation
    /// instead of focusing a single target.
    /// </summary>
    /// <remarks>
    /// Both counts are kept as members report and claim: every member queries the set every tick, and scanning
    /// each member's contacts and claims made a 250-ship attack-move cost tens of ms per frame (seconds when the
    /// whole group re-selected at once).
    /// </remarks>
    public sealed class AttackMoveEngagement
    {
        private readonly Dictionary<int, List<IEntity>> _contacts = new Dictionary<int, List<IEntity>>();
        private readonly Dictionary<int, IEntity> _claims = new Dictionary<int, IEntity>();
        // Enemy → how many members currently report it; the keys are the engagement set.
        private readonly Dictionary<IEntity, int> _reporters = new Dictionary<IEntity, int>();
        // Enemy → how many members claim it.
        private readonly Dictionary<IEntity, int> _attackers = new Dictionary<IEntity, int>();

        private int _nextMemberId;

        public int Join()
        {
            int member = _nextMemberId++;
            _contacts.Add(member, new List<IEntity>());
            return member;
        }

        public void Leave(int member)
        {
            ClearContacts(_contacts[member]);
            _contacts.Remove(member);
            DropClaim(member);
        }

        // IList, not IEnumerable: the radar's ObservableList boxes its enumerator.
        public void Report(int member, IList<IEntity> detected)
        {
            List<IEntity> contacts = _contacts[member];
            ClearContacts(contacts);
            for (int i = 0; i < detected.Count; i++)
            {
                IEntity enemy = detected[i];
                if (!IsAlive(enemy)) continue;
                contacts.Add(enemy);
                Increment(_reporters, enemy);
            }
        }

        public bool Contains(IEntity enemy) => IsAlive(enemy) && _reporters.ContainsKey(enemy);

        /// <summary>
        /// Claims the live enemy with the fewest other attackers, nearest first on ties.
        /// Returns null and drops the member's claim when the engagement set is empty.
        /// </summary>
        public IEntity SelectTarget(int member, Func<IEntity, float> distance)
        {
            DropClaim(member);
            IEntity best = null;
            int fewestAttackers = int.MaxValue;
            float nearest = float.PositiveInfinity;
            foreach (IEntity enemy in _reporters.Keys)
            {
                if (!IsAlive(enemy)) continue;
                _attackers.TryGetValue(enemy, out int attackers);
                if (attackers > fewestAttackers) continue;
                float range = distance(enemy);
                if (attackers == fewestAttackers && range >= nearest) continue;
                best = enemy;
                fewestAttackers = attackers;
                nearest = range;
            }

            if (best != null)
            {
                _claims[member] = best;
                Increment(_attackers, best);
            }

            return best;
        }

        private void ClearContacts(List<IEntity> contacts)
        {
            for (int i = 0; i < contacts.Count; i++) Decrement(_reporters, contacts[i]);
            contacts.Clear();
        }

        private void DropClaim(int member)
        {
            if (!_claims.TryGetValue(member, out IEntity claimed)) return;
            _claims.Remove(member);
            Decrement(_attackers, claimed);
        }

        private static void Increment(Dictionary<IEntity, int> counts, IEntity enemy)
        {
            counts.TryGetValue(enemy, out int count);
            counts[enemy] = count + 1;
        }

        private static void Decrement(Dictionary<IEntity, int> counts, IEntity enemy)
        {
            int count = counts[enemy] - 1;
            if (count == 0) counts.Remove(enemy);
            else counts[enemy] = count;
        }

        private static bool IsAlive(IEntity enemy) =>
            !enemy.HealthModel.IsDestroyed && enemy.HealthModel.HasUnits && !enemy.IsCloaked();
    }
}
