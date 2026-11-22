using System;
using System.Collections.Generic;
using System.Globalization;
using EmpireAtWar.ViewComponents.Weapon;
using UnityEngine;

namespace EmpireAtWar.Services.Networking.Diagnostics
{
    /// <summary>
    /// Projectile pool columns for one network status window: totals, ships vs fighters, and one
    /// <see cref="ShotPoolSample"/> per pool with a salvo waiting (overlay lines and the pools CSV).
    /// Max wait = longest admission wait finished in this window.
    /// </summary>
    public sealed class ShotPoolStats
    {
        private const int POOL_LINES = 6;

        private static readonly Comparison<ShotPoolSample> BY_WAITING = (left, right) =>
            right.Waiting.CompareTo(left.Waiting);

        private readonly List<ShotPoolSample> _waitingPools = new List<ShotPoolSample>();
        private int _lastCreated;
        private int _lastBlocked;

        /// <summary>Pools with a salvo waiting this window, most waiting shots first.</summary>
        public IReadOnlyList<ShotPoolSample> WaitingPools => _waitingPools;

        public void Collect(IEnumerable<ShotEffectCache> caches, float seconds, NetworkStatsRow row, List<string> details)
        {
            int waiting = 0, shipsWaiting = 0, fightersWaiting = 0, inUse = 0, total = 0, created = 0, blocked = 0;
            float oldestWait = 0f, maxWait = 0f;
            _waitingPools.Clear();
            foreach (ShotEffectCache cache in caches)
            {
                float windowMaxWait = cache.TakeWindowMaxAdmissionWaitSeconds();
                waiting += cache.WaitingCount;
                if (cache.IsStrikecraftGroup) fightersWaiting += cache.WaitingCount;
                else shipsWaiting += cache.WaitingCount;
                inUse += cache.InUseCount;
                total += cache.TotalCount;
                created += cache.RuntimeCreated;
                blocked += cache.AdmissionWaits;
                oldestWait = Mathf.Max(oldestWait, cache.OldestPendingWaitSeconds);
                maxWait = Mathf.Max(maxWait, windowMaxWait);
                if (cache.WaitingCount > 0)
                    _waitingPools.Add(new ShotPoolSample(cache.PrefabName, cache.IsStrikecraftGroup, cache.WaitingCount,
                        cache.InUseCount, cache.TotalCount, cache.RuntimeCreated, cache.OldestPendingWaitSeconds,
                        windowMaxWait));
            }

            // A battle restart clears the caches; never report negative growth.
            row.Set("pool_waiting_shots", waiting);
            row.Set("pool_ships_waiting", shipsWaiting);
            row.Set("pool_fighters_waiting", fightersWaiting);
            row.Set("pool_starving", _waitingPools.Count);
            row.Set("pool_oldest_wait_ms", oldestWait * 1000f);
            row.Set("pool_in_use", inUse);
            row.Set("pool_total", total);
            row.Set("pool_created_s", Math.Max(0, created - _lastCreated) / seconds);
            row.Set("pool_blocked_attempts_s", Math.Max(0, blocked - _lastBlocked) / seconds);
            row.Set("pool_max_wait_ms", maxWait * 1000f);
            _lastCreated = created;
            _lastBlocked = blocked;

            if (_waitingPools.Count == 0)
            {
                details.Add("Pools: no salvo waiting for a projectile.");
                return;
            }
            _waitingPools.Sort(BY_WAITING);
            int count = Math.Min(POOL_LINES, _waitingPools.Count);
            for (int i = 0; i < count; i++)
            {
                ShotPoolSample pool = _waitingPools[i];
                details.Add(string.Format(CultureInfo.InvariantCulture,
                    "Pool {0} ({1}): waiting {2}, in use {3}/{4}, created at runtime {5}, oldest wait {6:0.00} s, max wait {7:0.00} s",
                    pool.Prefab, pool.IsFighters ? "fighters" : "ships", pool.Waiting, pool.InUse, pool.Total,
                    pool.RuntimeCreated, pool.OldestWaitSeconds, pool.WindowMaxWaitSeconds));
            }
        }
    }
}
