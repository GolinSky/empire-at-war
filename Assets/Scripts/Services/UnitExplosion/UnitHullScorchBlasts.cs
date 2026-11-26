using System;
using EmpireAtWar.Components.AttackComponent;
using EmpireAtWar.Components.Ship.Health;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Extensions;
using EmpireAtWar.Lifecycle;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Services.UnitExplosion
{
    /// <summary>
    /// A short blast on hull hits, near the hit hardpoint with a random offset across the hull. One unit waits
    /// <see cref="IHullScorchBlastPool.UnitInterval"/> between blasts; the pool skips hidden, off-camera and
    /// over-budget ones. Precision strikes (super weapons, Lucky Shot) show their own effects and play none.
    /// </summary>
    public sealed class UnitHullScorchBlasts : IInitializable, ILateDisposable
    {
        // Offset radius per world unit of hardpoint VFX scale: about two thirds of a 7.5-wide blast.
        private const float OFFSET_RADIUS_PER_SCALE = 5f;
        // Largest lift off the surface, as a share of the offset radius.
        private const float MAX_LIFT_SHARE = 0.25f;
        private const int SPOT_ATTEMPTS = 8;

        private readonly IHullBlastSpots _spots;
        private readonly IHullScorchBlastPool _pool;
        private readonly IFogVisibilityFacade _fogVisibility;
        private readonly Transform _hull;
        private readonly System.Random _random = new System.Random();

        private float _readyAt;

        public UnitHullScorchBlasts(IHullBlastSpots spots, IHullScorchBlastPool pool,
            IFogVisibilityFacade fogVisibility, [Key(EntityBindType.ViewTransform)] Transform hull)
        {
            _spots = spots;
            _pool = pool;
            _fogVisibility = fogVisibility;
            _hull = hull;
        }

        public void Initialize() => _spots.HullDamaged += HandleHullDamaged;

        public void LateDispose() => _spots.HullDamaged -= HandleHullDamaged;

        private void HandleHullDamaged(int spotIndex, DamageType damageType)
        {
            if (damageType == DamageType.PrecisionStrike || Time.time < _readyAt) return;
            if (!TryPickSpot(spotIndex, out Transform anchor, out float scale)) return;
            if (_pool.TryPlay(anchor, RandomOffset(anchor.position, scale), _fogVisibility, scale))
                _readyAt = Time.time + _pool.UnitInterval;
        }

        // The hit spot, else a random one: hits on spots without VFX or under the hull still show on top.
        private bool TryPickSpot(int index, out Transform anchor, out float scale)
        {
            int count = _spots.SpotCount;
            if (index >= 0 && index < count && IsUsable(index, out anchor, out scale)) return true;
            for (int attempt = 0; count > 0 && attempt < SPOT_ATTEMPTS; attempt++)
            {
                if (IsUsable(_random.Next(count), out anchor, out scale)) return true;
            }

            anchor = null;
            scale = 0f;
            return false;
        }

        private bool IsUsable(int index, out Transform anchor, out float scale) =>
            _spots.TryGetSpot(index, out anchor, out scale) &&
            Vector3.Dot(anchor.position - _hull.position, _hull.up) >= 0f;

        // Across the hull's deck plane, always towards the hull's middle so edge spots stay over the hull, and lifted
        // off the surface, never pushed below it.
        private Vector3 RandomOffset(Vector3 anchor, float scale)
        {
            float radius = scale * OFFSET_RADIUS_PER_SCALE;
            float angle = (float)(_random.NextDouble() * 2.0 * Math.PI);
            Vector3 direction = _hull.right * Mathf.Cos(angle) + _hull.forward * Mathf.Sin(angle);
            if (Vector3.Dot(direction, _hull.position - anchor) < 0f) direction = -direction;
            float distance = radius * Mathf.Sqrt((float)_random.NextDouble());
            float lift = radius * MAX_LIFT_SHARE * (float)_random.NextDouble();
            return direction * distance + _hull.up * lift;
        }
    }
}
