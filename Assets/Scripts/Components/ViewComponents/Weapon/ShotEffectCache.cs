using System.Collections.Generic;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Services.Timing;
using Unity.Profiling;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Battle-owned storage shared by every mount using this shot prefab.</summary>
    public sealed class ShotEffectCache
    {
        private readonly ShotEffect _prefab;
        private readonly Stack<ShotEffect> _available = new Stack<ShotEffect>();
        private readonly HashSet<ShotEffect> _idle = new HashSet<ShotEffect>();
        private readonly HashSet<ShotEffect> _all = new HashSet<ShotEffect>();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ProfilerMarker _instantiateMarker;
        private readonly ProfilerMarker _prewarmMarker;
#endif
        private bool _released;

        public ShotEffectCache(ShotEffect prefab)
        {
            _prefab = prefab;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _instantiateMarker = new ProfilerMarker("Battle.Projectile.Instantiate." + prefab.name);
            _prewarmMarker = new ProfilerMarker("Battle.Projectile.Prewarm." + prefab.name);
#endif
        }

        public ShotEffect Acquire()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileGetOrCreate.Auto())
            {
#endif
            while (_available.Count > 0)
            {
                ShotEffect effect = _available.Pop();
                if (!_idle.Remove(effect)) continue;
                AttackSequenceDiagnostics.RecordPoolAvailable(-1);
                if (!effect) continue;
                AttackSequenceDiagnostics.RecordPoolReused();
                effect.enabled = true;
                return effect;
            }
            return Create(true);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
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

        public bool PrewarmOne(int target)
        {
            if (_released || _all.Count >= target) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_prewarmMarker.Auto())
            {
#endif
            Return(Create(false));
            return true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        private ShotEffect Create(bool onDemand)
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
            AttackSequenceDiagnostics.RecordPoolCreated(onDemand);
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
            foreach (ShotEffect effect in _all)
                if (effect) effect.RetireAfterCompletion();
        }

        public void DestroyAll()
        {
            _released = true;
            foreach (ShotEffect effect in new List<ShotEffect>(_all))
                if (effect) effect.CancelAndDestroy();
            _available.Clear();
        }
    }
}
