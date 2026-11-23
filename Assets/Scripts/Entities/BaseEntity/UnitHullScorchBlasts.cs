using System;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Components.TeamColor;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.UnitExplosion;
using UnityEngine;

namespace EmpireAtWar.Entities.BaseEntity
{
    /// <summary>
    /// Hull scorches (ShipHullDamage.hlsl) switch on all at once per damage level. When the level rises, a few short
    /// blasts play at random hardpoint surface points so the new marks do not just pop in. Repairs, loaded saves and
    /// the unit's death play none.
    /// </summary>
    public sealed class UnitHullScorchBlasts : IInitializable, ILateDisposable
    {
        private const int MAX_BLASTS_PER_RISE = 3;
        private const int SPOT_ATTEMPTS = 8;

        private readonly IHealthModelObserver _health;
        private readonly IHullBlastSpots _spots;
        private readonly IHullScorchBlastPool _pool;
        private readonly IFogVisibilityFacade _fogVisibility;
        private readonly System.Random _random = new System.Random();

        private int _level;

        public UnitHullScorchBlasts(IHealthComponent healthComponent, IHullBlastSpots spots,
            IHullScorchBlastPool pool, IFogVisibilityFacade fogVisibility)
        {
            _health = healthComponent.HealthModelObserver;
            _spots = spots;
            _pool = pool;
            _fogVisibility = fogVisibility;
        }

        private int CurrentLevel => UnitRenderStateComponent.GetHullDamageLevel(_health.HullPercentage);

        public void Initialize()
        {
            _level = CurrentLevel;
            _health.OnValueChanged += HandleHealthChanged;
            _spots.Restored += HandleRestored;
        }

        public void LateDispose()
        {
            _health.OnValueChanged -= HandleHealthChanged;
            _spots.Restored -= HandleRestored;
        }

        private void HandleRestored() => _level = CurrentLevel;

        private void HandleHealthChanged()
        {
            int level = CurrentLevel;
            int rise = level - _level;
            _level = level;
            if (rise <= 0 || _health.IsDestroyed) return;

            int blasts = Math.Min(rise, MAX_BLASTS_PER_RISE);
            for (int i = 0; i < blasts; i++) PlayAtRandomSpot();
        }

        private void PlayAtRandomSpot()
        {
            int count = _spots.SpotCount;
            if (count == 0) return;
            for (int attempt = 0; attempt < SPOT_ATTEMPTS; attempt++)
            {
                if (!_spots.TryGetSpot(_random.Next(count), out Transform anchor, out float scale)) continue;
                _pool.Play(anchor, _fogVisibility, scale);
                return;
            }
        }
    }
}
