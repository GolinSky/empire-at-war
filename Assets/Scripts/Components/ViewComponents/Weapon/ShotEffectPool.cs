using System;
using System.Collections.Generic;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Weapon;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.ViewComponents.Weapon
{
    /// <summary>
    /// One mount's leases on pooled shot visuals (missiles, beams, ion pulses). With no idle visual the shot is not
    /// drawn and the shared cache grows for the next one; combat never waits for a visual.
    /// </summary>
    public sealed class ShotEffectPool : IShotVisuals
    {
        private const float MIN_IMPACT_SIZE = 0.75f;
        private const float STRIKECRAFT_IMPACT_SCALE = 0.3f;

        private readonly Transform _owner;
        private readonly WeaponProfile _profile;
        private readonly ImpactEffectPresenter _impactPresenter;
        private readonly ShotEffectCache _cache;
        private readonly HashSet<ShotEffect> _active;
        private readonly Action<ShotEffect, int> _onCompleted;
        private readonly Action<ShotEffect, int> _onDestroyed;
        private bool _released;

        public event Action<ShotEffectPool> Retired;

        internal int ActiveCount => _active.Count;

        internal ShotEffectPool(WeaponProfile profile, Transform owner, ImpactEffectPresenter impactPresenter,
            ShotEffectCache cache, int capacity = 0)
        {
            _profile = profile;
            _owner = owner;
            _impactPresenter = impactPresenter;
            _cache = cache;
            _active = new HashSet<ShotEffect>(capacity);
            _onCompleted = OnEffectCompleted;
            _onDestroyed = OnEffectDestroyed;
        }

        public void Play(AttackData attackData, IHardPointModel target, Vector3 aimOffset, bool isHit,
            IncomingMissile missile, float lateSeconds)
        {
            if (_released)
                throw new InvalidOperationException("Cannot play an effect from a released pool.");
            if (!_cache.TryAcquire(out ShotEffect effect)) return;

            effect.EffectCompleted += _onCompleted;
            effect.EffectDestroyed += _onDestroyed;
            effect.PrepareImpact(_impactPresenter, attackData.TargetHealth, _profile.DamageType, _profile.Damage,
                GetImpactSize(_profile, attackData.TargetClass), isHit);
            effect.Fire(_owner, target.Transform, target.Pivot, aimOffset, _profile, lateSeconds);
            if (missile != null) effect.TrackInterception(missile);
            _active.Add(effect);
            AttackSequenceDiagnostics.RecordPoolActivated();
        }

        internal static float GetImpactSize(WeaponProfile profile, ShipClass targetClass)
        {
            float size = Mathf.Max(profile.Size.x, MIN_IMPACT_SIZE);
            return targetClass.IsStrikecraft() ? size * STRIKECRAFT_IMPACT_SCALE : size;
        }

        public void Release()
        {
            if (_released) return;
            _released = true;
            AttackSequenceDiagnostics.RecordOwnerlessActive(_active.Count);
            if (_active.Count == 0) NotifyRetired();
        }

        public void DestroyAll()
        {
            Release();
            foreach (ShotEffect effect in new List<ShotEffect>(_active))
                if (effect) effect.CancelAndDestroy();
        }

        private void OnEffectCompleted(ShotEffect effect, int leaseId)
        {
            if (effect.LeaseId != leaseId || !_active.Remove(effect))
            {
                AttackSequenceDiagnostics.RecordUnmatchedCompletion();
                return;
            }
            Detach(effect);
            AttackSequenceDiagnostics.RecordPoolReturned();
            _cache.Return(effect);
            CompleteLease();
        }

        private void OnEffectDestroyed(ShotEffect effect, int leaseId)
        {
            if (!_active.Remove(effect)) return;
            Detach(effect);
            AttackSequenceDiagnostics.RecordPoolReturned();
            CompleteLease();
        }

        private void Detach(ShotEffect effect)
        {
            effect.EffectCompleted -= _onCompleted;
            effect.EffectDestroyed -= _onDestroyed;
        }

        private void CompleteLease()
        {
            if (!_released) return;
            AttackSequenceDiagnostics.RecordOwnerlessActive(-1);
            if (_active.Count == 0) NotifyRetired();
        }

        private void NotifyRetired()
        {
            if (Retired != null) Retired(this);
        }
    }
}
