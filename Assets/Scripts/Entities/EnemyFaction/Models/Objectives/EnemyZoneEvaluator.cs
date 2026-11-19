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
        /// <summary>Force over the zone defenders needed to commit: an even fight. Below it the force gathers.</summary>
        public const float COMMIT_ADVANTAGE = 1f;
        /// <summary>A force already sent keeps its zone down to this share of the commit line.</summary>
        public const float RETAIN_SCALE = 0.85f;

        private readonly ForceComposition _supported = new ForceComposition();

        public EnemyZoneAssessment Assess(EnemyZoneObjective zone, ForceComposition force, float commitAdvantage)
        {
            if (zone.IsDenied)
                return new EnemyZoneAssessment(EnemyZoneVerdict.Deny, 0f, float.PositiveInfinity, 0f, zone.DenyReason);
            // Own satellites and platforms in the zone fight beside the force.
            if (!zone.OwnSupport.IsEmpty)
            {
                _supported.CopyFrom(force);
                _supported.Add(zone.OwnSupport);
                force = _supported;
            }
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

        /// <summary>
        /// Selection order before score: lower tiers win. Go zones of the chosen tier are interchangeable. The centre
        /// is never excluded. Behind on zones it is the last target while it needs assembly, unless the economy
        /// already stands (an outer zone and the home mine held); a centre the force beats is taken like any other
        /// enemy-side zone.
        /// </summary>
        public static int GetTier(EnemyZoneObjective zone, EnemyZoneVerdict verdict, bool behindOnZones,
            bool strongEconomy = false)
        {
            bool go = verdict == EnemyZoneVerdict.Go;
            if (zone.IsThreatened && go) return -1;
            if (!behindOnZones) return go ? 0 : 1;
            if (zone.IsOwnSide && !zone.IsCentre) return go ? 0 : 1;
            if (zone.IsCentre && !go && !strongEconomy) return 4;
            return go ? 2 : 3;
        }
    }
}
