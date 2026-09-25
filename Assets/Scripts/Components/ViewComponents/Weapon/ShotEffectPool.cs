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
        private readonly int _maxIdle;
        private readonly ImpactEffectPresenter _impactPresenter;
        private readonly Stack<ShotEffect> _available = new Stack<ShotEffect>();
        private readonly HashSet<ShotEffect> _availableMembers = new HashSet<ShotEffect>();
        private readonly Dictionary<ShotEffect, int> _active = new Dictionary<ShotEffect, int>();
        private Action<int> _effectCompleted;
        private bool _released;

        public ShotEffectPool(WeaponProfile profile, Transform owner, int maxIdle, Action<int> effectCompleted,
            ImpactEffectPresenter impactPresenter)
        {
            _prefab = profile.ShotPrefab;
            _owner = owner;
            _profile = profile;
            _effectCompleted = effectCompleted;
            _maxIdle = maxIdle;
            _impactPresenter = impactPresenter;
        }

        /// <returns>Seconds until the shot reaches the target.</returns>
        public float Play(AttackData attackData, IHardPointModel target, Vector3 aimOffset,
            int sequenceGeneration, bool isHit)
        {
            if (_released)
            {
                throw new InvalidOperationException("Cannot play an effect from a released pool.");
            }

            ShotEffect effect = Acquire();
            effect.PrepareImpact(_impactPresenter, attackData.TargetHealth, _profile.DamageType,
                GetImpactSize(attackData.TargetClass), isHit);
            float duration = effect.Fire(_owner, target.Transform, aimOffset, _profile);
            _active.Add(effect, sequenceGeneration);
            AttackSequenceDiagnostics.RecordPoolActivated();
            return duration;
        }

        // Impacts on strikecraft shrink so a flash never dwarfs the fighter it hits.
        private float GetImpactSize(ShipClass targetClass)
        {
            float size = Mathf.Max(_profile.Size.x, MIN_IMPACT_SIZE);
            return targetClass == ShipClass.Fighter || targetClass == ShipClass.Bomber
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
                effect.gameObject.SetActive(true);
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
                effect.transform.SetParent(null, true);
                effect.RetireAfterCompletion();
            }
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

            if (_released || _availableMembers.Count >= _maxIdle)
            {
                effect.EffectCompleted -= OnEffectCompleted;
                effect.EffectDestroyed -= OnEffectDestroyed;
                effect.RetireAfterCompletion();
                AttackSequenceDiagnostics.RecordPoolRetired();
            }
            else
            {
                effect.gameObject.SetActive(false);
                _available.Push(effect);
                _availableMembers.Add(effect);
                AttackSequenceDiagnostics.RecordPoolAvailable(1);
            }

            if (!_released)
            {
                _effectCompleted(sequenceGeneration);
            }
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
        }

        public void Prewarm(int count)
        {
            if (_released)
            {
                throw new InvalidOperationException("Cannot prewarm a released pool.");
            }

            int desired = Mathf.Min(count, _maxIdle);
            while (_availableMembers.Count < desired)
            {
                ShotEffect effect = UnityEngine.Object.Instantiate(_prefab);
                effect.EffectCompleted += OnEffectCompleted;
                effect.EffectDestroyed += OnEffectDestroyed;
                effect.gameObject.SetActive(false);
                _available.Push(effect);
                _availableMembers.Add(effect);
                AttackSequenceDiagnostics.RecordPoolCreated(false);
                AttackSequenceDiagnostics.RecordPoolAvailable(1);
            }
        }
    }
}
