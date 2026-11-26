using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Picks the strike ships of a hit-and-run cycle: group ships ranked by their damage against the target, added
    /// until the set is ready against the target and its support. No ship names and no type ids.
    /// </summary>
    public sealed class EnemyStrikeGroupRules
    {
        /// <summary>A strike group of one ship is a plain fight.</summary>
        public const int MINIMUM_STRIKERS = 2;

        private const UnitRole STRIKE_ROLES = UnitRole.AntiCapital | UnitRole.Artillery | UnitRole.Dps;

        private readonly ForceComposition _selectedForce = new ForceComposition();
        private readonly List<int> _ranked = new List<int>();
        private readonly List<float> _damage = new List<float>();
        private readonly List<UnitRole> _roles = new List<UnitRole>();
        private readonly Comparison<int> _byDamage;

        public EnemyStrikeGroupRules()
        {
            _byDamage = CompareByDamage;
        }

        /// <summary>
        /// Fills <paramref name="selected"/> with candidate indices. Members in <paramref name="retained"/> stay while
        /// they still damage the target; ships are then added by rank until
        /// <see cref="EnemyTaskForceSelectionModel.IsReady"/> passes or <paramref name="maximumCount"/> is reached.
        /// False, with an empty selection, below <see cref="MINIMUM_STRIKERS"/>.
        /// </summary>
        /// <param name="defenders">The target and its nearby support.</param>
        public bool Select(IReadOnlyList<ForceComposition> candidates, IReadOnlyList<UnitRole> roles,
            ForceComposition defenders, ShipClass targetClass, float targetHull, float targetShields,
            float requiredAdvantage, IReadOnlyList<int> retained, int maximumCount, List<int> selected)
        {
            selected.Clear();
            _selectedForce.Clear();
            _ranked.Clear();
            _damage.Clear();
            _roles.Clear();
            for (int i = 0; i < candidates.Count; i++)
            {
                _damage.Add(EffectiveDps(candidates[i], targetClass, targetHull, targetShields));
                _roles.Add(roles[i]);
                if (_damage[i] > 0f) _ranked.Add(i);
            }
            _ranked.Sort(_byDamage);
            foreach (int index in retained)
            {
                if (selected.Count >= maximumCount) break;
                if (index < 0 || index >= candidates.Count || _damage[index] <= 0f || selected.Contains(index)) continue;
                selected.Add(index);
                _selectedForce.Add(candidates[index]);
            }
            foreach (int index in _ranked)
            {
                if (selected.Count >= maximumCount || selected.Count >= MINIMUM_STRIKERS &&
                    EnemyTaskForceSelectionModel.IsReady(_selectedForce, defenders, targetClass, targetHull,
                        targetShields, requiredAdvantage)) break;
                if (selected.Contains(index)) continue;
                selected.Add(index);
                _selectedForce.Add(candidates[index]);
            }
            if (selected.Count >= MINIMUM_STRIKERS) return true;
            selected.Clear();
            return false;
        }

        /// <summary>Hit-and-run is for hostile big ships; smaller ships and strikecraft are fought normally.</summary>
        public static bool IsStrikeTargetClass(ShipClass shipClass) =>
            shipClass is ShipClass.HeavyCruiser or ShipClass.Capital or ShipClass.BattleCruiser or ShipClass.Dreadnought;

        /// <summary>Hull and shield points per second <paramref name="force"/> removes from the target; 0 when it cannot destroy it.</summary>
        public static float EffectiveDps(ForceComposition force, ShipClass targetClass, float hull, float shields)
        {
            float time = CombatMatchup.TimeToDestroyTarget(force, targetClass, hull, shields, null);
            return time >= CombatMatchup.MAX_TIME ? 0f : (hull + shields) / Math.Max(time, float.Epsilon);
        }

        // Most damage first; a strike role breaks a tie, then the earlier ship.
        private int CompareByDamage(int first, int second)
        {
            int damage = _damage[second].CompareTo(_damage[first]);
            if (damage != 0) return damage;
            int role = ((_roles[second] & STRIKE_ROLES) != 0).CompareTo((_roles[first] & STRIKE_ROLES) != 0);
            return role != 0 ? role : first.CompareTo(second);
        }
    }
}
