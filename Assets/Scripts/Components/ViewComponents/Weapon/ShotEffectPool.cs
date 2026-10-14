using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class ShotEffectPool
    {
        private const float MIN_IMPACT_SIZE = 0.75f;
        private const float STRIKECRAFT_IMPACT_SCALE = 0.3f;

        private readonly ShotEffect _prefab;
        private readonly Transform _owner;
        private readonly WeaponProfile _profile;
        private readonly ImpactEffectPresenter _impactPresenter;
        private readonly BoltFlightBatch _boltFlights;
        private readonly Stack<ShotEffect> _available = new Stack<ShotEffect>();
        private readonly HashSet<ShotEffect> _availableMembers = new HashSet<ShotEffect>();
        private readonly Dictionary<ShotEffect, int> _active = new Dictionary<ShotEffect, int>();
        private Action<int> _effectCompleted;

        private readonly int _maxIdle;
        // Most shots this pool has had in flight at once. Returned shots are kept up to it: dropping them while
        // the next salvo is already due made the pool instantiate and destroy shots every salvo.
        private int _peakActive;

        private bool _released;
        public event Action<ShotEffectPool> Retired;

        public ShotEffectPool(WeaponProfile profile, Transform owner, Action<int> effectCompleted, ImpactEffectPresenter impactPresenter,
            BoltFlightBatch boltFlights, int maxIdle)
        {
            _boltFlights = boltFlights;
            _prefab = profile.ShotPrefab;
            _owner = owner;
            _profile = profile;
            _effectCompleted = effectCompleted;
            _maxIdle = maxIdle;
            _impactPresenter = impactPresenter;
        }

        /// <returns>Seconds until the shot reaches the target.</returns>
        public float Play(AttackData attackData, IHardPointModel target, Vector3 aimOffset,
            int sequenceGeneration, bool isHit, out ShotEffect effect)
        {
            if (_released)
            {
                throw new InvalidOperationException("Cannot play an effect from a released pool.");
            }

            Transform targetTransform = target.Transform;
            effect = Acquire();
            effect.PrepareImpact(_impactPresenter, attackData.TargetHealth, _profile.DamageType, _profile.Damage,
                GetImpactSize(attackData.TargetClass), isHit);
            float duration = effect.Fire(_owner, targetTransform, target.Pivot, aimOffset, _profile, _boltFlights);
            _active.Add(effect, sequenceGeneration);
            if (_active.Count > _peakActive) _peakActive = _active.Count;
            AttackSequenceDiagnostics.RecordPoolActivated();
            return duration;
        }

        // Impacts on strikecraft shrink so a flash never dwarfs the fighter it hits.
        private float GetImpactSize(ShipClass targetClass)
        {
            float size = Mathf.Max(_profile.Size.x, MIN_IMPACT_SIZE);
            return targetClass.IsStrikecraft()
                ? size * STRIKECRAFT_IMPACT_SCALE
                : size;
        }

        private ShotEffect Acquire()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileGetOrCreate.Auto())
            {
#endif
            while (_available.Count > 0)
            {
                ShotEffect effect = _available.Pop();
                if (!_availableMembers.Remove(effect))
                {
                    continue;
                }

                AttackSequenceDiagnostics.RecordPoolAvailable(-1);
                if (!effect)
                {
                    AttackSequenceDiagnostics.RecordPoolRetired();
                    continue;
                }

                AttackSequenceDiagnostics.RecordPoolReused();
                effect.enabled = true;
                return effect;
            }

            ShotEffect created;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileInstantiate.Auto())
            {
#endif
            created = UnityEngine.Object.Instantiate(_prefab);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
            created.EffectCompleted += OnEffectCompleted;
            created.EffectDestroyed += OnEffectDestroyed;
            AttackSequenceDiagnostics.RecordPoolCreated(true);
            return created;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            }
#endif
        }

        public void Release()
        {
            if (_released)
            {
                return;
            }

            _released = true;
            _effectCompleted = null;
            AttackSequenceDiagnostics.RecordOwnerlessActive(_active.Count);

            while (_available.Count > 0)
            {
                ShotEffect effect = _available.Pop();
                if (!_availableMembers.Remove(effect))
                {
                    continue;
                }

                AttackSequenceDiagnostics.RecordPoolAvailable(-1);
                if (!effect)
                {
                    AttackSequenceDiagnostics.RecordPoolRetired();
                    continue;
                }

                effect.EffectCompleted -= OnEffectCompleted;
                effect.EffectDestroyed -= OnEffectDestroyed;
                effect.RetireAfterCompletion();
                AttackSequenceDiagnostics.RecordPoolRetired();
            }

            foreach (ShotEffect effect in _active.Keys)
            {
                effect.RetireAfterCompletion();
            }
            if (_active.Count == 0) Retired?.Invoke(this);
        }

        public void DestroyAll()
        {
            Release();
            foreach (ShotEffect effect in new List<ShotEffect>(_active.Keys))
                if (effect != null) effect.CancelAndDestroy();
        }

        private void OnEffectCompleted(ShotEffect effect, int leaseId)
        {
            if (effect.LeaseId != leaseId || !_active.TryGetValue(effect, out int sequenceGeneration))
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return;
            }

            _active.Remove(effect);
            AttackSequenceDiagnostics.RecordPoolReturned();
            if (_released)
            {
                AttackSequenceDiagnostics.RecordOwnerlessActive(-1);
            }

            if (_released || _availableMembers.Count >= Mathf.Max(_maxIdle, _peakActive))
            {
                effect.EffectCompleted -= OnEffectCompleted;
                effect.EffectDestroyed -= OnEffectDestroyed;
                effect.RetireAfterCompletion();
                AttackSequenceDiagnostics.RecordPoolRetired();
            }
            else
            {
                effect.ResetForPool();
                _available.Push(effect);
                _availableMembers.Add(effect);
                AttackSequenceDiagnostics.RecordPoolAvailable(1);
            }

            if (!_released)
            {
                _effectCompleted(sequenceGeneration);
            }
            else if (_active.Count == 0) Retired?.Invoke(this);
        }

        private void OnEffectDestroyed(ShotEffect effect, int leaseId)
        {
            if (_availableMembers.Remove(effect))
            {
                AttackSequenceDiagnostics.RecordPoolAvailable(-1);
                AttackSequenceDiagnostics.RecordPoolRetired();
                return;
            }

            if (!_active.TryGetValue(effect, out int sequenceGeneration))
            {
                return;
            }

            _active.Remove(effect);
            AttackSequenceDiagnostics.RecordPoolReturned();
            AttackSequenceDiagnostics.RecordPoolRetired();
            if (_released)
            {
                AttackSequenceDiagnostics.RecordOwnerlessActive(-1);
            }

            if (!_released)
            {
                _effectCompleted(sequenceGeneration);
            }
            else if (_active.Count == 0) Retired?.Invoke(this);
        }

        /// <summary>Adds one idle shot while the pool holds fewer than <paramref name="target"/> shots.</summary>
        /// <returns>False once the pool is full or released.</returns>
        public bool PrewarmOne(int target)
        {
            if (_released || _availableMembers.Count + _active.Count >= target) return false;
            AddIdle();
            return true;
        }

        public void Prewarm(int count)
        {
            if (_released)
            {
                throw new InvalidOperationException("Cannot prewarm a released pool.");
            }

            int desired = Mathf.Min(count, _maxIdle);
            while (_availableMembers.Count < desired) AddIdle();
        }

        private void AddIdle()
        {
            ShotEffect effect = UnityEngine.Object.Instantiate(_prefab);
            effect.EffectCompleted += OnEffectCompleted;
            effect.EffectDestroyed += OnEffectDestroyed;
            effect.ResetForPool();
            _available.Push(effect);
            _availableMembers.Add(effect);
            AttackSequenceDiagnostics.RecordPoolCreated(false);
            AttackSequenceDiagnostics.RecordPoolAvailable(1);
        }
    }
}
