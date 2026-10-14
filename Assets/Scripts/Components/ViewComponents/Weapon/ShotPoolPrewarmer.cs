using System.Collections.Generic;
using System.Diagnostics;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Fills shot pools up to the shots a hardpoint has in flight at once, a few per frame inside a time budget,
    /// so the salvos after a hardpoint's first one reuse shots instead of instantiating them mid-battle.
    /// </summary>
    public sealed class ShotPoolPrewarmer : ITickable
    {
        private const double BUDGET_MILLISECONDS = 0.5;

        private readonly Queue<(ShotEffectPool Pool, int Target)> _pending = new Queue<(ShotEffectPool, int)>();
        private readonly HashSet<ShotEffectPool> _pools = new HashSet<ShotEffectPool>();

        public void Enqueue(ShotEffectPool pool, int target)
        {
            if (_pools.Add(pool)) pool.Retired += OnRetired;
            _pending.Enqueue((pool, target));
        }

        private void OnRetired(ShotEffectPool pool)
        {
            pool.Retired -= OnRetired;
            _pools.Remove(pool);
        }

        public void Clear()
        {
            _pending.Clear();
            foreach (ShotEffectPool pool in new List<ShotEffectPool>(_pools)) pool.DestroyAll();
        }

        public void Tick()
        {
            if (_pending.Count == 0) return;
            long start = Stopwatch.GetTimestamp();
            long budget = (long)(BUDGET_MILLISECONDS * Stopwatch.Frequency / 1000.0);
            while (_pending.Count > 0 && Stopwatch.GetTimestamp() - start < budget)
            {
                (ShotEffectPool pool, int target) = _pending.Peek();
                if (!pool.PrewarmOne(target)) _pending.Dequeue();
            }
        }
    }
}
