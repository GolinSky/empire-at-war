using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.ViewComponents.Weapon;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Projectile pool columns for one network status window: totals, ships vs fighters, and one
    /// <see cref="ShotPoolSample"/> per pool that fired shots undrawn or still grows for them (overlay lines and the
    /// pools CSV). Shots never wait for a visual, so these measure missing visuals, not firing delay.
    /// </summary>
    public sealed class ShotPoolStats
    {
        private const int POOL_LINES = 6;

        private static readonly Comparison<ShotPoolSample> BY_SKIPPED = (left, right) =>
            right.Skipped != left.Skipped ? right.Skipped.CompareTo(left.Skipped) : right.Pending.CompareTo(left.Pending);

        private readonly List<ShotPoolSample> _starvingPools = new List<ShotPoolSample>();
        private int _lastCreated;

        /// <summary>Pools short of visuals this window, most skipped shots first.</summary>
        public IReadOnlyList<ShotPoolSample> StarvingPools => _starvingPools;

        public void Collect(IEnumerable<ShotEffectCache> caches, float seconds, NetworkStatsRow row, List<string> details)
        {
            int pending = 0, shipsPending = 0, fightersPending = 0, skipped = 0, inUse = 0, total = 0, created = 0;
            _starvingPools.Clear();
            foreach (ShotEffectCache cache in caches)
            {
                int windowSkipped = cache.TakeWindowSkippedVisuals();
                pending += cache.PendingDemand;
                if (cache.IsStrikecraftGroup) fightersPending += cache.PendingDemand;
                else shipsPending += cache.PendingDemand;
                skipped += windowSkipped;
                inUse += cache.InUseCount;
                total += cache.TotalCount;
                created += cache.RuntimeCreated;
                if (cache.PendingDemand > 0 || windowSkipped > 0)
                    _starvingPools.Add(new ShotPoolSample(cache.PrefabName, cache.IsStrikecraftGroup, cache.PendingDemand,
                        windowSkipped, cache.InUseCount, cache.TotalCount, cache.RuntimeCreated));
            }

            // A battle restart clears the caches; never report negative growth.
            row.Set("pool_pending_visuals", pending);
            row.Set("pool_ships_pending", shipsPending);
            row.Set("pool_fighters_pending", fightersPending);
            row.Set("pool_starving", _starvingPools.Count);
            row.Set("pool_skipped_visuals_s", skipped / seconds);
            row.Set("pool_in_use", inUse);
            row.Set("pool_total", total);
            row.Set("pool_created_s", Math.Max(0, created - _lastCreated) / seconds);
            _lastCreated = created;

            if (_starvingPools.Count == 0)
            {
                details.Add("Pools: every shot drawn.");
                return;
            }
            _starvingPools.Sort(BY_SKIPPED);
            int count = Math.Min(POOL_LINES, _starvingPools.Count);
            for (int i = 0; i < count; i++)
            {
                ShotPoolSample pool = _starvingPools[i];
                details.Add(string.Format(CultureInfo.InvariantCulture,
                    "Pool {0} ({1}): {2} shots undrawn, growing {3}, in use {4}/{5}, created at runtime {6}",
                    pool.Prefab, pool.IsFighters ? "fighters" : "ships", pool.Skipped, pool.Pending, pool.InUse,
                    pool.Total, pool.RuntimeCreated));
            }
        }
    }
}
