using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using UnityEngine;

namespace EmpireAtWar.Entities.SpaceStation
{
    /// <summary>Keeps squadron icon grids off the station hull box so the station stays clickable.</summary>
    public sealed class StationHullObstacle : IBattleHull, IInitializable, ILateDisposable
    {
        private readonly IBattleMarkerLayer _layer;
        private readonly IHealthModelObserver _health;
        private readonly IFogVisibilityFacade _fog;
        private readonly BoxCollider _hull;
        private bool _registered;

        public StationHullObstacle(IBattleMarkerLayer layer, IHealthModelObserver health, IFogVisibilityFacade fog,
            BoxCollider hull)
        {
            _layer = layer;
            _health = health;
            _fog = fog;
            _hull = hull;
        }

        public Transform Transform => _hull.transform;
        public Bounds LocalBounds => new Bounds(_hull.center, _hull.size);
        public bool IsActive => !_health.IsDestroyed && !_fog.IsHiddenByFog;

        public void Initialize()
        {
            _registered = true;
            _layer.RegisterHull(this);
        }

        public void LateDispose()
        {
            if (!_registered) return;
            _registered = false;
            _layer.UnregisterHull(this);
        }
    }
}
