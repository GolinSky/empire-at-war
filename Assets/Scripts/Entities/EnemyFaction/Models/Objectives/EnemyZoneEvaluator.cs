using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>
    /// Rates every zone for one force with a single strength measure (<see cref="CombatMatchup.Advantage"/> against
    /// the zone defenders) and always returns a plan: the cheapest Go zone, otherwise the cheapest Assemble zone.
    /// </summary>
    public sealed class EnemyZoneEvaluator
    {
        /// <summary>Cost of each missing force-equivalent, in travel-distance units: about one zone hop.</summary>
        public const float MISSING_FORCE_COST = 1000f;

        public EnemyZoneAssessment Assess(EnemyZoneObjective zone, ForceComposition force, float commitAdvantage)
        {
            if (zone.IsDenied)
                return new EnemyZoneAssessment(EnemyZoneVerdict.Deny, 0f, float.PositiveInfinity, 0f, zone.DenyReason);
            float advantage = CombatMatchup.Advantage(force, zone.Defenders);
            float requiredScale = zone.Defenders.IsEmpty ? 0f :
                commitAdvantage / Math.Max(advantage, CombatMatchup.MIN_ADVANTAGE);
            bool go = zone.Defenders.IsEmpty || advantage >= commitAdvantage;
            float score = zone.Priority - Math.Max(0f, requiredScale - 1f) * MISSING_FORCE_COST;
            return new EnemyZoneAssessment(go ? EnemyZoneVerdict.Go : EnemyZoneVerdict.Assemble, advantage,
                requiredScale, score, go ? "force beats the known defenders" : "force too small; assemble");
        }

        /// <summary>
        /// Fills <paramref name="results"/> with one assessment per zone (same order) and returns the chosen index,
        /// or -1 when every zone is denied. Behind on zones: own-side zones come first and the centre is only an
        /// assembly target until nothing else remains.
        /// </summary>
        public int Select(IReadOnlyList<EnemyZoneObjective> zones, ForceComposition force, float commitAdvantage,
            bool behindOnZones, List<EnemyZoneAssessment> results)
        {
            results.Clear();
            int best = -1;
            int bestTier = int.MaxValue;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < zones.Count; i++)
            {
                EnemyZoneAssessment assessment = Assess(zones[i], force, commitAdvantage);
                results.Add(assessment);
                if (assessment.Verdict == EnemyZoneVerdict.Deny) continue;
                int tier = GetTier(zones[i], assessment.Verdict, behindOnZones);
                if (tier > bestTier || tier == bestTier && assessment.Score <= bestScore) continue;
                best = i;
                bestTier = tier;
                bestScore = assessment.Score;
            }
            return best;
        }

        /// <summary>Selection order before score: lower tiers win. Go zones of the chosen tier are interchangeable.</summary>
        public static int GetTier(EnemyZoneObjective zone, EnemyZoneVerdict verdict, bool behindOnZones)
        {
            bool go = verdict == EnemyZoneVerdict.Go;
            if (!behindOnZones) return go ? 0 : 1;
            if (zone.IsOwnSide && !zone.IsCentre) return go ? 0 : 1;
            if (zone.IsCentre) return 4;
            return go ? 2 : 3;
        }
    }
}
