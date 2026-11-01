using System;
using System.Collections.Generic;
using EmpireAtWar.Mvc;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Combat
{
    /// <summary>
    /// Picks the unit that improves the matchup against the hostile force the most per credit.
    /// Counters fall out of the damage matrix: a unit that kills what threatens the fleet, or survives it,
    /// raises the advantage more than one that does not. The designer matchup table and a repeat penalty
    /// then shape the pick so the fleet stays varied. The penalty grows with the square root of the owned count:
    /// a linear one pushed the fleet into weak types it did not own yet.
    /// </summary>
    public sealed class EnemyCounterProductionModel : Model
    {
        /// <summary>Smallest log-advantage gain worth a purchase; below it the force is already saturated.</summary>
        private const double MINIMUM_GAIN = 0.001;

        private readonly IShipClassMatchups _matchups;
        private readonly ForceComposition _candidateForce = new ForceComposition();

        public double SelectedScore { get; private set; }

        public EnemyCounterProductionModel(IShipClassMatchups matchups)
        {
            _matchups = matchups;
        }

        public bool TrySelect(
            ForceComposition own,
            ForceComposition hostile,
            IReadOnlyList<ProductionCandidate> candidates,
            out int selectedIndex,
            float availableCredits = 1f,
            int availableCapacity = int.MaxValue,
            bool isStationCritical = false,
            float timeToLossSeconds = float.PositiveInfinity)
        {
            selectedIndex = -1;
            SelectedScore = 0.0;
            if (hostile.IsEmpty && !isStationCritical)
            {
                return false;
            }

            double baseline = hostile.IsEmpty ? 0.0 : Math.Log(CombatMatchup.Advantage(own, hostile));
            double bestScore = 0.0;
            bool hasTimelyCandidate = false;
            for (int i = 0; i < candidates.Count; i++)
            {
                ProductionCandidate candidate = candidates[i];
                _candidateForce.CopyFrom(own);
                _candidateForce.AddNew(candidate.Profile, candidate.IncludeHangar);
                double gain = hostile.IsEmpty
                    ? candidate.Profile.GetCombatValue()
                    : Math.Log(CombatMatchup.Advantage(_candidateForce, hostile)) - baseline;
                if (gain < MINIMUM_GAIN)
                {
                    continue;
                }

                // Price scarce credits and scarce fleet slots together. A rich, nearly full fleet
                // should replace losses with effective combat power per slot, not cheap hulls.
                double resourceCost = Math.Max(
                    Math.Max(1, candidate.Price) / (double)Math.Max(1f, availableCredits),
                    candidate.UnitCapacity / (double)Math.Max(1, availableCapacity));
                float preference = hostile.IsEmpty ? 1f : _matchups.GetPreference(candidate.Profile.ShipClass, hostile);
                double score = gain / resourceCost * preference /
                               Math.Sqrt(1 + candidate.OwnedCount);
                if (isStationCritical)
                {
                    bool isTimely = candidate.ArrivalSeconds <= timeToLossSeconds;
                    if (hasTimelyCandidate && !isTimely) continue;
                    if (isTimely && !hasTimelyCandidate)
                    {
                        bestScore = 0.0;
                        hasTimelyCandidate = true;
                    }
                    score = gain * preference /
                            Math.Max(1f, candidate.ArrivalSeconds);
                }
                if (score > bestScore)
                {
                    bestScore = score;
                    SelectedScore = score;
                    selectedIndex = i;
                }
            }

            return selectedIndex >= 0;
        }
    }
}
