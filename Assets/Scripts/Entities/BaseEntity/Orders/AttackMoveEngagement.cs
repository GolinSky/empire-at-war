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
        // Member → its slot, its flight height and how far short of its slot it stopped (slot minus stop position).
        // Ships in other height layers pass over or under each other, so a stop pulls back only its own layer
        // (a deep-flying dreadnought stops at its long range without holding the group back).
        public const float SAME_LAYER_HEIGHT = 50f;
        private readonly Dictionary<int, PullBack> _pullBacks = new Dictionary<int, PullBack>();

        private int _nextMemberId;
        private string _groupId;

        // Only engagements included in a save need a serialized identity.
        public string GroupId => _groupId ??= Guid.NewGuid().ToString("N");

        public AttackMoveEngagement(string groupId = null) => _groupId = groupId;

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
            _pullBacks.Remove(member);
        }

        /// <summary>
        /// The member stopped short of its slot (an enemy came into its reach). Members whose slots lie behind
        /// this one along the shortfall shift their slots back by it, so rear rows do not fly into the stopped line.
        /// </summary>
        public void SetPullBack(int member, float slotX, float slotZ, float height, float pullX, float pullZ) =>
            _pullBacks[member] = new PullBack(slotX, slotZ, height, pullX, pullZ);

        public void ClearPullBack(int member) => _pullBacks.Remove(member);

        /// <summary>
        /// The largest shortfall of another member in this height layer whose slot is ahead of this slot along that
        /// shortfall; false when no such member stopped short.
        /// </summary>
        public bool TryGetPullBack(int member, float slotX, float slotZ, float height, out float pullX,
            out float pullZ)
        {
            pullX = 0f;
            pullZ = 0f;
            float best = 0f;
            foreach (KeyValuePair<int, PullBack> entry in _pullBacks)
            {
                if (entry.Key == member) continue;
                PullBack pull = entry.Value;
                if (Math.Abs(pull.Height - height) > SAME_LAYER_HEIGHT) continue;
                float length = (float)Math.Sqrt(pull.PullX * pull.PullX + pull.PullZ * pull.PullZ);
                if (length <= best) continue;
                // Ahead: the stopped member's slot is farther along its shortfall than this slot.
                float ahead = ((pull.SlotX - slotX) * pull.PullX + (pull.SlotZ - slotZ) * pull.PullZ) / length;
                if (ahead <= 0f) continue;
                best = length;
                pullX = pull.PullX;
                pullZ = pull.PullZ;
            }

            return best > 0f;
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

        public bool HasPreferred(Func<IEntity, bool> isPreferred)
        {
            foreach (IEntity enemy in _reporters.Keys)
                if (IsAlive(enemy) && isPreferred(enemy)) return true;
            return false;
        }

        /// <summary>
        /// Claims a live enemy: preferred enemies first, then the highest <paramref name="priority"/> (for ships:
        /// in weapon reach, then threat), then the fewest other attackers, nearest first on ties.
        /// Only enemies passing <paramref name="isEligible"/> (all when null) count; a null priority ranks all
        /// equal. Returns null and drops the member's claim when none qualifies.
        /// </summary>
        public IEntity SelectTarget(int member, Func<IEntity, float> distance, Func<IEntity, bool> isPreferred,
            Func<IEntity, bool> isEligible = null, Func<IEntity, int> priority = null)
        {
            DropClaim(member);
            IEntity best = null;
            bool bestPreferred = false;
            int bestPriority = int.MinValue;
            int fewestAttackers = int.MaxValue;
            float nearest = float.PositiveInfinity;
            foreach (IEntity enemy in _reporters.Keys)
            {
                if (!IsAlive(enemy) || (isEligible != null && !isEligible(enemy))) continue;
                bool preferred = isPreferred(enemy);
                if (bestPreferred && !preferred) continue;
                bool outranks = preferred && !bestPreferred;
                int rank = priority == null ? 0 : priority(enemy);
                if (!outranks && rank < bestPriority) continue;
                outranks |= rank > bestPriority;
                _attackers.TryGetValue(enemy, out int attackers);
                if (!outranks && attackers > fewestAttackers) continue;
                float range = distance(enemy);
                if (!outranks && attackers == fewestAttackers && range >= nearest) continue;
                best = enemy;
                bestPreferred = preferred;
                bestPriority = rank;
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

        private readonly struct PullBack
        {
            public readonly float SlotX;
            public readonly float SlotZ;
            public readonly float Height;
            public readonly float PullX;
            public readonly float PullZ;

            public PullBack(float slotX, float slotZ, float height, float pullX, float pullZ)
            {
                SlotX = slotX;
                SlotZ = slotZ;
                Height = height;
                PullX = pullX;
                PullZ = pullZ;
            }
        }
    }
}
