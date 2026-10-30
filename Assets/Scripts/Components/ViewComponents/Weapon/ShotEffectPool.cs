using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class ShotEffectPool
    {
        private const float MIN_IMPACT_SIZE = 0.75f;
        private const float STRIKECRAFT_IMPACT_SCALE = 0.3f;

        private readonly Transform _owner;
        private readonly WeaponProfile _profile;
        private readonly ImpactEffectPresenter _impactPresenter;
        private readonly BoltFlightBatch _boltFlights;
        private readonly ShotEffectCache _cache;
        private readonly bool _ownsCache;
        private readonly Dictionary<ShotEffect, int> _active = new Dictionary<ShotEffect, int>();
        private Action<int> _effectCompleted;
        private bool _released;

        public event Action<ShotEffectPool> Retired;

        public ShotEffectPool(WeaponProfile profile, Transform owner, Action<int> effectCompleted,
            ImpactEffectPresenter impactPresenter, BoltFlightBatch boltFlights)
            : this(profile, owner, effectCompleted, impactPresenter, boltFlights, new ShotEffectCache(profile.ShotPrefab))
        {
            _ownsCache = true;
        }

        internal ShotEffectPool(WeaponProfile profile, Transform owner, Action<int> effectCompleted,
            ImpactEffectPresenter impactPresenter, BoltFlightBatch boltFlights, ShotEffectCache cache)
        {
            _profile = profile;
            _owner = owner;
            _effectCompleted = effectCompleted;
            _impactPresenter = impactPresenter;
            _boltFlights = boltFlights;
            _cache = cache;
        }

        /// <returns>Seconds until the shot reaches the target.</returns>
        public float Play(AttackData attackData, IHardPointModel target, Vector3 aimOffset,
            int sequenceGeneration, bool isHit, out ShotEffect effect)
        {
            if (_released)
                throw new InvalidOperationException("Cannot play an effect from a released pool.");

            effect = Acquire();
            effect.PrepareImpact(_impactPresenter, attackData.TargetHealth, _profile.DamageType, _profile.Damage,
                GetImpactSize(attackData.TargetClass), isHit);
            float duration = effect.Fire(_owner, target.Transform, target.Pivot, aimOffset, _profile, _boltFlights);
            _active.Add(effect, sequenceGeneration);
            AttackSequenceDiagnostics.RecordPoolActivated();
            return duration;
        }

        private float GetImpactSize(ShipClass targetClass)
        {
            float size = Mathf.Max(_profile.Size.x, MIN_IMPACT_SIZE);
            return targetClass.IsStrikecraft() ? size * STRIKECRAFT_IMPACT_SCALE : size;
        }

        private ShotEffect Acquire()
        {
            ShotEffect effect = _cache.Acquire();
            effect.EffectCompleted += OnEffectCompleted;
            effect.EffectDestroyed += OnEffectDestroyed;
            return effect;
        }

        public void Release()
        {
            if (_released) return;
            _released = true;
            _effectCompleted = null;
            AttackSequenceDiagnostics.RecordOwnerlessActive(_active.Count);
            // Shared storage keeps idle shots and accepts these active leases after they finish.
            if (_ownsCache) _cache.Release();
            if (_active.Count == 0) NotifyRetired();
        }

        public void DestroyAll()
        {
            Release();
            foreach (ShotEffect effect in new List<ShotEffect>(_active.Keys))
                if (effect) effect.CancelAndDestroy();
        }

        private void OnEffectCompleted(ShotEffect effect, int leaseId)
        {
            if (effect.LeaseId != leaseId || !_active.TryGetValue(effect, out int sequenceGeneration))
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return;
            }
            _active.Remove(effect);
            Detach(effect);
            AttackSequenceDiagnostics.RecordPoolReturned();
            _cache.Return(effect);
            CompleteOwner(sequenceGeneration);
        }

        private void OnEffectDestroyed(ShotEffect effect, int leaseId)
        {
            if (!_active.TryGetValue(effect, out int sequenceGeneration)) return;
            _active.Remove(effect);
            Detach(effect);
            AttackSequenceDiagnostics.RecordPoolReturned();
            CompleteOwner(sequenceGeneration);
        }

        private void Detach(ShotEffect effect)
        {
            effect.EffectCompleted -= OnEffectCompleted;
            effect.EffectDestroyed -= OnEffectDestroyed;
        }

        private void CompleteOwner(int sequenceGeneration)
        {
            if (_released)
            {
                AttackSequenceDiagnostics.RecordOwnerlessActive(-1);
                if (_active.Count == 0) NotifyRetired();
            }
            else
            {
                _effectCompleted(sequenceGeneration);
            }
        }

        private void NotifyRetired()
        {
            if (Retired != null) Retired(this);
        }

        public bool PrewarmOne(int target) => !_released && _cache.PrewarmOne(target);

        public void Prewarm(int count)
        {
            if (_released) throw new InvalidOperationException("Cannot prewarm a released pool.");
            while (_cache.PrewarmOne(count)) { }
        }
    }
}
