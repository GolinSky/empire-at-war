using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Picks the unit that improves the matchup against the hostile force the most per credit.
    /// Counters fall out of the damage matrix: a unit that kills what threatens the fleet, or survives it,
    /// raises the advantage more than one that does not. The designer matchup table and a repeat penalty
    /// then shape the pick so the fleet stays varied.
    /// </summary>
    public sealed class EnemyCounterProductionModel : PureModel
    {
        /// <summary>Smallest log-advantage gain worth a purchase; below it the force is already saturated.</summary>
        private const double MINIMUM_GAIN = 0.001;

        private readonly IShipClassMatchups _matchups;
        private readonly ForceComposition _candidateForce = new ForceComposition();

        public EnemyCounterProductionModel(IShipClassMatchups matchups)
        {
            _matchups = matchups;
        }

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
                ProductionCandidate candidate = candidates[i];
                _candidateForce.CopyFrom(own);
                _candidateForce.AddNew(candidate.Profile, candidate.IncludeHangar);
                double gain = Math.Log(CombatMatchup.Advantage(_candidateForce, hostile)) - baseline;
                if (gain < MINIMUM_GAIN)
                {
                    continue;
                }

                double score = gain / Math.Max(1, candidate.Price) *
                               _matchups.GetPreference(candidate.Profile.ShipClass, hostile) /
                               (1 + candidate.OwnedCount);
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
