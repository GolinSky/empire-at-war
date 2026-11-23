using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.EnemyFaction.Models.Combat;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>
    /// Rates every zone for one force with a single strength measure (<see cref="CombatMatchup.Advantage"/> against
    /// the zone defenders) and always returns a plan: the cheapest Go zone, otherwise the cheapest Assemble zone. A
    /// priced force goes when the attack is expected to gain (W67): hostile value destroyed plus the zone's
    /// <see cref="EnemyZoneObjective.Value"/> minus own value lost, at the advantage times the zone's trade correction.
    /// A force without prices goes at the commit advantage.
    /// </summary>
    public sealed class EnemyZoneEvaluator
    {
        /// <summary>Cost of each missing force-equivalent, in travel-distance units: about one zone hop.</summary>
        public const float MISSING_FORCE_COST = 1000f;
        /// <summary>
        /// Force over the zone defenders needed to commit when the force has no prices (no expected gain to rate): an
        /// even fight. Below it the force gathers. Also the base of the retain and defend lines.
        /// </summary>
        public const float COMMIT_ADVANTAGE = 1f;
        /// <summary>Search range of <see cref="EnemyZoneAssessment.RequiredScale"/> for the expected-gain rule.</summary>
        private const float MIN_REQUIRED_SCALE = 0.01f;
        private const float MAX_REQUIRED_SCALE = CombatMatchup.MAX_ADVANTAGE;
        private const int REQUIRED_SCALE_STEPS = 16;
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
                force.Value > 0f ? GetRequiredScale(zone, force.Value, advantage * zone.TradeCorrection) :
                commitAdvantage / Math.Max(advantage, CombatMatchup.MIN_ADVANTAGE);
            bool go = zone.Defenders.IsEmpty || (force.Value > 0f
                ? Gain(zone, force.Value, advantage * zone.TradeCorrection, 1f) > 0f
                : advantage >= commitAdvantage);
            float score = zone.Priority - Math.Max(0f, requiredScale - 1f) * MISSING_FORCE_COST;
            return new EnemyZoneAssessment(go ? EnemyZoneVerdict.Go : EnemyZoneVerdict.Assemble, advantage,
                requiredScale, score, go ? "attack gains against the known defenders" : "force too small; assemble");
        }

        /// <summary>
        /// Smallest multiple of the force with a positive expected gain: the force and its value scale together, and
        /// the advantage with them (a force ratio). Bisection; <see cref="MAX_REQUIRED_SCALE"/> when none gains.
        /// </summary>
        private static float GetRequiredScale(EnemyZoneObjective zone, float ownValue, float advantage)
        {
            bool gains = Gain(zone, ownValue, advantage, 1f) > 0f;
            float low = gains ? MIN_REQUIRED_SCALE : 1f;
            float high = gains ? 1f : MAX_REQUIRED_SCALE;
            if (gains && Gain(zone, ownValue, advantage, low) > 0f) return low;
            if (!gains && Gain(zone, ownValue, advantage, high) <= 0f) return high;
            for (int step = 0; step < REQUIRED_SCALE_STEPS; step++)
            {
                float middle = 0.5f * (low + high);
                if (Gain(zone, ownValue, advantage, middle) > 0f) high = middle;
                else low = middle;
            }
            return high;
        }

        private static float Gain(EnemyZoneObjective zone, float ownValue, float advantage, float scale) =>
            CombatMatchup.ExpectedGain(advantage * scale, ownValue * scale, zone.Defenders.Value, zone.Value);

        /// <summary>
        /// Fills <paramref name="results"/> with one assessment per zone (same order) and returns the chosen index,
        /// or -1 when every zone is denied. Behind on zones: own-side zones come first and the centre is only an
        /// assembly target until nothing else remains or the economy stands (<paramref name="strongEconomy"/>, A7: the
        /// same tiers as the allocator).
        /// </summary>
        public int Select(IReadOnlyList<EnemyZoneObjective> zones, ForceComposition force, float commitAdvantage,
            bool behindOnZones, List<EnemyZoneAssessment> results, bool strongEconomy = false)
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
                int tier = GetTier(zones[i], assessment.Verdict, behindOnZones, strongEconomy);
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
        /// enemy-side zone. A held zone under attack is the top tier whatever the verdict: its satellites fight beside
        /// the force sent.
        /// </summary>
        public static int GetTier(EnemyZoneObjective zone, EnemyZoneVerdict verdict, bool behindOnZones,
            bool strongEconomy = false)
        {
            bool go = verdict == EnemyZoneVerdict.Go;
            if (zone.IsHeldUnderAttack || zone.IsThreatened && go) return -1;
            if (!behindOnZones) return go ? 0 : 1;
            if (zone.IsOwnSide && !zone.IsCentre) return go ? 0 : 1;
            if (zone.IsCentre && !go && !strongEconomy) return 4;
            return go ? 2 : 3;
        }
    }
}
