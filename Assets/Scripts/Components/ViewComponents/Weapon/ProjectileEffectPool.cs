using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Timing;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    public sealed class ProjectileEffectPool
    {
        private readonly BaseTurretView _prefab;
        private readonly Transform _owner;
        private readonly ProjectileData _projectileData;
        private readonly float _attackDistance;
        private readonly int _maxIdle;
        private readonly Stack<BaseTurretView> _available = new Stack<BaseTurretView>();
        private readonly HashSet<BaseTurretView> _availableMembers = new HashSet<BaseTurretView>();
        private readonly Dictionary<BaseTurretView, int> _active = new Dictionary<BaseTurretView, int>();
        private Action<int> _effectCompleted;
        private bool _released;

        public ProjectileEffectPool(BaseTurretView prefab, Transform owner, ProjectileData projectileData,
            float attackDistance, int maxIdle, Action<int> effectCompleted)
        {
            _prefab = prefab;
            _owner = owner;
            _projectileData = projectileData;
            _effectCompleted = effectCompleted;
            _attackDistance = attackDistance;
            _maxIdle = maxIdle;
        }

        public float Play(IHardPointModel target, int sequenceGeneration)
        {
            if (_released)
            {
                throw new InvalidOperationException("Cannot play an effect from a released pool.");
            }

            BaseTurretView effect = Acquire();
            effect.SetData(_projectileData, _attackDistance);
            effect.SetParent(_owner);
            effect.Attack(target, out float duration);
            effect.ResetParent();
            _active.Add(effect, sequenceGeneration);
            AttackSequenceDiagnostics.RecordPoolActivated();
            return duration;
        }

        private BaseTurretView Acquire()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileGetOrCreate.Auto())
            {
#endif
            while (_available.Count > 0)
            {
                BaseTurretView effect = _available.Pop();
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

            BaseTurretView created;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using (BattleProfilerMarkers.ProjectileInstantiate.Auto())
            {
#endif
            created = UnityEngine.Object.Instantiate(_prefab, _owner);
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
                BaseTurretView effect = _available.Pop();
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

            foreach (BaseTurretView effect in _active.Keys)
            {
                effect.transform.SetParent(null, true);
                effect.RetireAfterCompletion();
            }
        }

        private void OnEffectCompleted(BaseTurretView effect, int leaseId)
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

        private void OnEffectDestroyed(BaseTurretView effect, int leaseId)
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
                BaseTurretView effect = UnityEngine.Object.Instantiate(_prefab, _owner);
                effect.EffectCompleted += OnEffectCompleted;
                effect.EffectDestroyed += OnEffectDestroyed;
                effect.SetData(_projectileData, _attackDistance);
                effect.gameObject.SetActive(false);
                _available.Push(effect);
                _availableMembers.Add(effect);
                AttackSequenceDiagnostics.RecordPoolCreated(false);
                AttackSequenceDiagnostics.RecordPoolAvailable(1);
            }
        }
    }
}
