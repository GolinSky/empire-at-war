using System;
using System.Collections.Generic;

namespace EmpireAtWar.Entities.EnemyFaction.Models.Objectives
{
    /// <summary>
    /// Per-zone memory of one AI player (W47, decision D4): failed captures, retakes and own losses at each zone, and
    /// the zones marked "not now". <see cref="GetLevel"/> turns them with the zone's situation into a caution level and
    /// <see cref="SelectPlan"/> picks the capture plan from it. Everything fades after <see cref="MEMORY_SECONDS"/>.
    /// </summary>
    public sealed class EnemyZoneCaution
    {
        /// <summary>Failures and losses older than this no longer count: "within a few minutes".</summary>
        public const float MEMORY_SECONDS = 300f;
        /// <summary>An important zone failed this often goes together with an ally.</summary>
        public const int COMBINED_FAILURES = 2;
        /// <summary>Any zone failed this often is skipped.</summary>
        public const int SKIP_FAILURES = 3;
        /// <summary>How long a skipped zone stays "not now".</summary>
        public const float SKIP_SECONDS = 180f;
        /// <summary>Caution added per failed capture or retake.</summary>
        public const float FAILURE_WEIGHT = 1f;
        /// <summary>Caution added per own fleet value lost at the zone (loss share 1 = the whole fleet).</summary>
        public const float LOSS_WEIGHT = 2f;
        /// <summary>Caution added when hostiles can drop reinforcements near the zone.</summary>
        public const float SPAWN_ACCESS_WEIGHT = 0.5f;
        /// <summary>Caution added by a hostile armada at the zone, fading to 0 at <see cref="ARMADA_NEAR_DISTANCE"/>.</summary>
        public const float ARMADA_WEIGHT = 1f;
        public const float ARMADA_NEAR_DISTANCE = 3000f;
        /// <summary>Caution added per missing share of an even reach rating at the zone (rating 0.5 → 0.5 × weight).</summary>
        public const float FLEET_DEFICIT_WEIGHT = 1f;
        /// <summary>A retaken zone with hostile spawn access at this level is held from outside.</summary>
        public const float HOLD_BACK_LEVEL = 1.5f;
        /// <summary>At this level a zone is skipped whatever its failures.</summary>
        public const float SKIP_LEVEL = 4f;
        /// <summary>A zone worth this share of the team income (or the centre) is important enough for an ally.</summary>
        public const float IMPORTANT_WORTH_SHARE = 0.25f;

        private readonly Dictionary<string, Record> _records = new Dictionary<string, Record>();

        /// <summary>
        /// Notes the zone's state at <paramref name="now"/>: owned → not owned is a retake; own ships that stood in the
        /// zone while it was not owned and left before it was owned are a failed capture.
        /// </summary>
        public void Observe(string zoneId, bool owned, bool ownShipsPresent, float now)
        {
            Record record = GetRecord(zoneId);
            if (record.Observed && record.Owned && !owned) record.Retakes.Add(now);
            if (owned && (!record.Observed || !record.Owned)) record.OwnedSince = now;
            if (owned) record.Attempting = false;
            else if (ownShipsPresent) record.Attempting = true;
            else if (record.Attempting)
            {
                record.Attempting = false;
                record.FailedCaptures.Add(now);
            }
            record.Owned = owned;
            record.Observed = true;
        }

        /// <summary>An own unit worth <paramref name="value"/> credits was lost at the zone.</summary>
        public void RecordLoss(string zoneId, float value, float now)
        {
            Record record = GetRecord(zoneId);
            record.Losses = GetLosses(record, now) + value;
            record.LossTime = now;
        }

        /// <summary>Seconds the team has owned the zone without a break; 0 while it does not.</summary>
        public float GetHeldSeconds(string zoneId, float now) =>
            _records.TryGetValue(zoneId, out Record record) && record.Owned ? Math.Max(0f, now - record.OwnedSince) : 0f;

        /// <summary>Failed captures plus retakes within <see cref="MEMORY_SECONDS"/>.</summary>
        public int CountFailures(string zoneId, float now) =>
            _records.TryGetValue(zoneId, out Record record) ? Count(record.FailedCaptures, now) + Count(record.Retakes, now) : 0;

        public int CountRetakes(string zoneId, float now) =>
            _records.TryGetValue(zoneId, out Record record) ? Count(record.Retakes, now) : 0;

        /// <summary>Own value lost at the zone, fading with <see cref="MEMORY_SECONDS"/>.</summary>
        public float GetLosses(string zoneId, float now) =>
            _records.TryGetValue(zoneId, out Record record) ? GetLosses(record, now) : 0f;

        public bool IsSkipped(string zoneId, float now) =>
            _records.TryGetValue(zoneId, out Record record) && now < record.SkipUntil;

        /// <summary>Marks the zone "not now" for <see cref="SKIP_SECONDS"/>.</summary>
        public void Skip(string zoneId, float now) => GetRecord(zoneId).SkipUntil = now + SKIP_SECONDS;

        /// <summary>Caution level of a zone; 0 is a zone with no history in a fair fight.</summary>
        /// <param name="lossShare">Own value lost there over the own fleet value.</param>
        /// <param name="armadaDistance">Distance from the zone to the nearest hostile armada; infinity when none.</param>
        /// <param name="fleetRating">Reach rating of the own fleet at the zone (1 = even).</param>
        public static float GetLevel(int failures, float lossShare, bool hostileSpawnNear, float armadaDistance,
            float fleetRating)
        {
            float armada = armadaDistance >= ARMADA_NEAR_DISTANCE ? 0f : 1f - armadaDistance / ARMADA_NEAR_DISTANCE;
            return FAILURE_WEIGHT * failures + LOSS_WEIGHT * lossShare + (hostileSpawnNear ? SPAWN_ACCESS_WEIGHT : 0f) +
                ARMADA_WEIGHT * armada + FLEET_DEFICIT_WEIGHT * Math.Max(0f, 1f - fleetRating);
        }

        /// <summary>
        /// Plan for a zone. A zone the team holds is never skipped. Skip: <see cref="SKIP_FAILURES"/> failures, the
        /// <see cref="SKIP_LEVEL"/>, or a failed zone the fleet cannot take with gain even with the ally's help. An
        /// important zone failed <see cref="COMBINED_FAILURES"/> times goes with the ally; a retaken zone where hostiles
        /// drop is held from outside. Otherwise take and hold (the allocator turns it into raid-and-leave when the
        /// garrison costs more than the zone is worth).
        /// </summary>
        /// <param name="gainsWithAlly">The own fleet with the available allied main forces takes the zone with gain.</param>
        public static EnemyZonePlan SelectPlan(float level, int failures, int retakes, bool held, bool important,
            bool gainsWithAlly, bool hostileSpawnNear)
        {
            if (!held && (failures >= SKIP_FAILURES || level >= SKIP_LEVEL || failures > 0 && !gainsWithAlly))
                return EnemyZonePlan.Skip;
            if (failures >= COMBINED_FAILURES && important) return EnemyZonePlan.AllyCombined;
            if (retakes > 0 && hostileSpawnNear && level >= HOLD_BACK_LEVEL) return EnemyZonePlan.HoldBackAndCounter;
            return EnemyZonePlan.TakeAndHold;
        }

        private Record GetRecord(string zoneId)
        {
            if (!_records.TryGetValue(zoneId, out Record record))
            {
                record = new Record();
                _records.Add(zoneId, record);
            }
            return record;
        }

        private static int Count(List<float> times, float now)
        {
            int kept = 0;
            for (int i = 0; i < times.Count; i++)
                if (now - times[i] <= MEMORY_SECONDS) times[kept++] = times[i];
            times.RemoveRange(kept, times.Count - kept);
            return kept;
        }

        private static float GetLosses(Record record, float now) =>
            record.Losses * (float)Math.Exp(-Math.Max(0f, now - record.LossTime) / MEMORY_SECONDS);

        private sealed class Record
        {
            public readonly List<float> FailedCaptures = new List<float>();
            public readonly List<float> Retakes = new List<float>();
            public bool Observed;
            public bool Owned;
            public bool Attempting;
            public float OwnedSince;
            public float Losses;
            public float LossTime;
            public float SkipUntil = float.NegativeInfinity;
        }
    }
}
