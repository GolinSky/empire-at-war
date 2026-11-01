using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Ship.Health;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    public sealed class EnemyTaskForceSelectionModel
    {
        private readonly ForceComposition _selectedForce = new ForceComposition();
        private readonly ForceComposition _candidateForce = new ForceComposition();

        public bool Select(IReadOnlyList<ForceComposition> candidates, ForceComposition defenders,
            ShipClass targetClass, float targetHull, float targetShields, float requiredAdvantage,
            IReadOnlyList<int> retained, int maximumCount, List<int> selected)
        {
            selected.Clear();
            _selectedForce.Clear();
            foreach (int index in retained)
            {
                if (selected.Count >= maximumCount) break;
                if (index < 0 || index >= candidates.Count || selected.Contains(index) ||
                    !CanContribute(candidates[index], defenders, targetClass, targetShields)) continue;
                selected.Add(index);
                _selectedForce.Add(candidates[index]);
            }

            // Keep a viable force committed; rebuild an inadequate composition against changed defenders.
            if (!IsReady(_selectedForce, defenders, targetClass, targetHull, targetShields, requiredAdvantage))
            {
                selected.Clear();
                _selectedForce.Clear();
            }

            while (selected.Count < maximumCount)
            {
                // Strategy already chose this commitment. Keep its useful firepower in the attack,
                // rather than leaving the rest guarding a minimum-sized strike force.
                int best = -1;
                float bestScore = float.MinValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    if (selected.Contains(i) || !CanContribute(candidates[i], defenders, targetClass, targetShields)) continue;
                    _candidateForce.CopyFrom(_selectedForce);
                    _candidateForce.Add(candidates[i]);
                    float clearTime = TargetTime(_candidateForce, targetClass, targetHull, targetShields) +
                                      CombatMatchup.TimeToDestroy(_candidateForce, defenders);
                    float score = Math.Min(requiredAdvantage, CombatMatchup.Advantage(_candidateForce, defenders)) /
                                  Math.Max(float.Epsilon, clearTime);
                    if (score <= bestScore) continue;
                    best = i;
                    bestScore = score;
                }
                if (best < 0) break;
                selected.Add(best);
                _selectedForce.Add(candidates[best]);
            }

            return IsReady(_selectedForce, defenders, targetClass, targetHull, targetShields, requiredAdvantage);
        }

        public static bool IsReady(ForceComposition force, ForceComposition defenders,
            ShipClass targetClass, float targetHull, float targetShields, float requiredAdvantage) =>
            !force.IsEmpty && TargetTime(force, targetClass, targetHull, targetShields) < CombatMatchup.MAX_TIME &&
            CombatMatchup.Advantage(force, defenders) >= requiredAdvantage;

        private static bool CanContribute(ForceComposition force, ForceComposition defenders,
            ShipClass targetClass, float targetShields) =>
            force.HullDps(targetClass) + force.PiercingDps(targetClass) > 0f &&
            (targetShields <= 0f || force.ShieldDps(targetClass) + force.PiercingDps(targetClass) > 0f) ||
            !defenders.IsEmpty && CombatMatchup.TimeToDestroy(force, defenders) < CombatMatchup.MAX_TIME;

        private static float TargetTime(ForceComposition force, ShipClass targetClass, float hull, float shields) =>
            CombatMatchup.TimeToDestroyClass(hull, shields, force.HullDps(targetClass),
                force.ShieldDps(targetClass), force.PiercingDps(targetClass));
    }
}
