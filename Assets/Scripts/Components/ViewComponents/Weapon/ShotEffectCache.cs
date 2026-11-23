using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Services.Timing;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// Battle-owned storage shared by every mount using this shot prefab. A shot that finds no idle visual is not
    /// drawn; it counts as demand, and <see cref="ShotPoolPrewarmer"/> grows the storage for later shots.
    /// </summary>
    public sealed class ShotEffectCache
    {
        // Demand counted per missed shot is capped, so one burst cannot queue unbounded growth.
        private const int MAX_PENDING_DEMAND = 256;

        private readonly ShotEffect _prefab;
        private readonly Stack<ShotEffect> _available = new Stack<ShotEffect>();
        private readonly HashSet<ShotEffect> _idle = new HashSet<ShotEffect>();
        private readonly HashSet<ShotEffect> _all = new HashSet<ShotEffect>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ProfilerMarker _instantiateMarker;
        private readonly ProfilerMarker _prewarmMarker;
#endif
        private bool _released;
        private int _windowSkippedVisuals;

        public int TotalCount => _all.Count;
        public int InUseCount => _all.Count - _idle.Count;
        /// <summary>Visuals still wanted by shots that fired undrawn.</summary>
        public int PendingDemand { get; private set; }
        public int RequiredCount => InUseCount + PendingDemand;
        public event Action<ShotEffectCache> DemandChanged;
        public string PrefabName => _prefab.name;
        public int PeakInUse { get; private set; }
        public int LoadingCreated { get; private set; }
        public int RuntimeCreated { get; private set; }
        /// <summary>Shots fired without a visual because every visual was in use.</summary>
        public int SkippedVisuals { get; private set; }

        /// <summary>Fighters and ships use separate caches of the same prefab so neither starves the other.</summary>
        public bool IsStrikecraftGroup { get; }

        public ShotEffectCache(ShotEffect prefab, bool strikecraftGroup = false)
        {
            _prefab = prefab;
            IsStrikecraftGroup = strikecraftGroup;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _instantiateMarker = new ProfilerMarker("Battle.Projectile.Instantiate." + prefab.name);
            _prewarmMarker = new ProfilerMarker("Battle.Projectile.Prewarm." + prefab.name);
#endif
        }

        /// <summary>Leases an idle visual; never instantiates while firing.</summary>
        internal bool TryAcquire(out ShotEffect effect)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileGetOrCreate.Auto())
            {
#endif
            while (_available.Count > 0)
            {
                effect = _available.Pop();
                if (!_idle.Remove(effect)) continue;
                AttackSequenceDiagnostics.RecordPoolAvailable(-1);
                if (!effect) continue;
                AttackSequenceDiagnostics.RecordPoolReused();
                PeakInUse = Math.Max(PeakInUse, InUseCount);
                effect.enabled = true;
                return true;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            effect = null;
            SkippedVisuals++;
            _windowSkippedVisuals++;
            AttackSequenceDiagnostics.RecordSkippedVisual();
            if (_released) return false;
            PendingDemand = Math.Min(PendingDemand + 1, MAX_PENDING_DEMAND);
            if (DemandChanged != null) DemandChanged(this);
            return false;
        }

        /// <summary>Shots fired without a visual since the previous call (network status window), then resets.</summary>
        public int TakeWindowSkippedVisuals()
        {
            int skipped = _windowSkippedVisuals;
            _windowSkippedVisuals = 0;
            return skipped;
        }

        public void Return(ShotEffect effect)
        {
            if (_released)
            {
                effect.RetireAfterCompletion();
                return;
            }
            effect.ResetForPool();
            _available.Push(effect);
            _idle.Add(effect);
            AttackSequenceDiagnostics.RecordPoolAvailable(1);
        }

        public bool PrewarmOne(int target, bool loading = false)
        {
            if (_released || _all.Count >= target) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_prewarmMarker.Auto())
#endif
            Return(Create());
            if (PendingDemand > 0) PendingDemand--;
            if (loading) LoadingCreated++;
            else RuntimeCreated++;
            return true;
        }

        private ShotEffect Create()
        {
            ShotEffect effect;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileInstantiate.Auto())
            using (_instantiateMarker.Auto())
            {
#endif
            effect = Object.Instantiate(_prefab);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            _all.Add(effect);
            effect.EffectDestroyed += OnDestroyed;
            AttackSequenceDiagnostics.RecordPoolCreated(false);
            return effect;
        }

        private void OnDestroyed(ShotEffect effect, int leaseId)
        {
            effect.EffectDestroyed -= OnDestroyed;
            if (!_all.Remove(effect)) return;
            if (_idle.Remove(effect)) AttackSequenceDiagnostics.RecordPoolAvailable(-1);
            AttackSequenceDiagnostics.RecordPoolRetired();
        }

        public void Release()
        {
            _released = true;
            PendingDemand = 0;
            foreach (ShotEffect effect in _all)
                if (effect) effect.RetireAfterCompletion();
        }

        public void DestroyAll()
        {
            _released = true;
            PendingDemand = 0;
            foreach (ShotEffect effect in new List<ShotEffect>(_all))
                if (effect) effect.CancelAndDestroy();
            _available.Clear();
        }
    }
}
