using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Picks the unit that improves the matchup against the hostile force the most per credit.
    /// Counters fall out of the damage matrix: a unit that kills what threatens the fleet, or survives it,
    /// raises the advantage more than one that does not.
    /// </summary>
    public sealed class EnemyCounterProductionModel : PureModel
    {
        /// <summary>Smallest log-advantage gain worth a purchase; below it the force is already saturated.</summary>
        private const double MINIMUM_GAIN = 0.001;

        private readonly ForceComposition _candidateForce = new ForceComposition();

        public bool TrySelect(
            ForceComposition own,
            ForceComposition hostile,
            IReadOnlyList<ProductionCandidate> candidates,
            out int selectedIndex)
        {
            selectedIndex = -1;
            if (hostile.IsEmpty)
            {
                return false;
            }

            double baseline = Math.Log(CombatMatchup.Advantage(own, hostile));
            double bestScore = 0.0;
            for (int i = 0; i < candidates.Count; i++)
            {
                _candidateForce.CopyFrom(own);
                _candidateForce.AddNew(candidates[i].Profile, true);
                double gain = Math.Log(CombatMatchup.Advantage(_candidateForce, hostile)) - baseline;
                if (gain < MINIMUM_GAIN)
                {
                    continue;
                }

                double score = gain / Math.Max(1, candidates[i].Price);
                if (score > bestScore)
                {
                    bestScore = score;
                    selectedIndex = i;
                }
            }

            return selectedIndex >= 0;
        }
    }
}
