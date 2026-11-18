using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Services.Timing;
using Unity.Profiling;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>Battle-owned storage shared by every mount using this shot prefab.</summary>
    public sealed class ShotEffectCache
    {
        private readonly ShotEffect _prefab;
        private readonly Stack<ShotEffect> _available = new Stack<ShotEffect>();
        private readonly HashSet<ShotEffect> _idle = new HashSet<ShotEffect>();
        private readonly HashSet<ShotEffect> _all = new HashSet<ShotEffect>();
        private readonly LinkedList<ShotEffectPool> _waiting = new LinkedList<ShotEffectPool>();
        private int _reservedCount;
        private int _waitingCount;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private readonly ProfilerMarker _instantiateMarker;
        private readonly ProfilerMarker _prewarmMarker;
#endif
        private bool _released;
        private int _registeredCapacity;

        public int TotalCount => _all.Count;
        public int InUseCount => _all.Count - _idle.Count;
        public int ReservedCount => _reservedCount;
        public int WaitingCount => _waitingCount;
        public int RequiredCount => InUseCount + _reservedCount + _waitingCount;
        public int RegisteredCapacity => _registeredCapacity;
        public event Action<ShotEffectCache> DemandChanged;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public string PrefabName => _prefab.name;
        public int PeakInUse { get; private set; }
        public int LoadingCreated { get; private set; }
        public int RuntimeCreated { get; private set; }
        public int OnDemandExpansions { get; private set; }
        public int AdmissionWaits { get; private set; }
        public double TotalAdmissionWaitSeconds { get; private set; }
        public float MaxAdmissionWaitSeconds { get; private set; }
        public float OldestPendingWaitSeconds => _waiting.First == null ? 0f
            : Mathf.Max(0f, Time.time - _waiting.First.Value.AdmissionStartedAt);
#endif

        internal bool TryReserve(ShotEffectPool pool, int count)
        {
            if (pool.ReservedShots > 0) return pool.ReservedShots == count;
            if (pool.AdmissionNode.List == null)
            {
                pool.WaitingShots = count;
                _waitingCount += count;
                _waiting.AddLast(pool.AdmissionNode);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                pool.AdmissionStartedAt = Time.time;
#endif
            }
            else if (pool.WaitingShots != count)
            {
                _waitingCount += count - pool.WaitingShots;
                pool.WaitingShots = count;
            }
            // The front waiter keeps priority, but a smaller salvo behind it may go when idle shots cover both.
            int free = _idle.Count - _reservedCount;
            bool isFront = _waiting.First == pool.AdmissionNode;
            if (free >= count && (isFront || free >= count + _waiting.First.Value.WaitingShots))
            {
                _waiting.Remove(pool.AdmissionNode);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                RecordWait(pool);
#endif
                _waitingCount -= pool.WaitingShots;
                pool.WaitingShots = 0;
                pool.ReservedShots = count;
                _reservedCount += count;
                return true;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            AdmissionWaits++;
#endif
            if (DemandChanged != null) DemandChanged(this);
            return false;
        }

        internal void CancelReservation(ShotEffectPool pool)
        {
            if (pool.AdmissionNode.List != null)
            {
                _waiting.Remove(pool.AdmissionNode);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                RecordWait(pool);
#endif
                _waitingCount -= pool.WaitingShots;
                pool.WaitingShots = 0;
            }
            _reservedCount -= pool.ReservedShots;
            pool.ReservedShots = 0;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void RecordWait(ShotEffectPool pool)
        {
            float seconds = Mathf.Max(0f, Time.time - pool.AdmissionStartedAt);
            TotalAdmissionWaitSeconds += seconds;
            MaxAdmissionWaitSeconds = Mathf.Max(MaxAdmissionWaitSeconds, seconds);
        }
#endif

        public int RegisterCapacity(int capacity)
        {
            _registeredCapacity += capacity;
            return _registeredCapacity;
        }

        public void ReleaseCapacity(int capacity) => _registeredCapacity -= capacity;

        public ShotEffectCache(ShotEffect prefab)
        {
            _prefab = prefab;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _instantiateMarker = new ProfilerMarker("Battle.Projectile.Instantiate." + prefab.name);
            _prewarmMarker = new ProfilerMarker("Battle.Projectile.Prewarm." + prefab.name);
#endif
        }

        internal ShotEffect Acquire(ShotEffectPool pool)
        {
            if (pool.ReservedShots == 0)
                throw new InvalidOperationException("A projectile must belong to an admitted salvo before it is leased.");
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
                pool.ReservedShots--;
                _reservedCount--;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                PeakInUse = Math.Max(PeakInUse, InUseCount);
#endif
                effect.enabled = true;
                return effect;
            }
            throw new InvalidOperationException("An admitted salvo lost its reserved projectile storage.");
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

        public bool PrewarmOne(int target, bool loading = false)
        {
            if (_released || _all.Count >= target) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (_prewarmMarker.Auto())
            {
#endif
            Return(Create());
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (loading) LoadingCreated++;
            else RuntimeCreated++;
#endif
            return true;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
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
