using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Extensions;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.ShipSpawning;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Components.Ship.Selection
{
    /// <summary>Tells the battle marker layer where this hull is on screen so squadron grids keep off it.</summary>
    [LifecycleOrder(Initialize = LifecycleOrders.HUD_PRESENTERS_INITIALIZE)]
    public sealed class ShipHullObstacle : IBattleHull, IInitializable, ILateDisposable, IRespawnable, IDespawnable
    {
        private readonly IBattleMarkerLayer _layer;
        private readonly ShipSpawnClearance _clearance;
        private readonly IHealthModelObserver _health;
        private readonly IFogVisibilityFacade _fog;
        private readonly ShipType _shipType;
        private bool _registered;

        public ShipHullObstacle(IBattleMarkerLayer layer, ShipSpawnClearance clearance,
            [Key(EntityBindType.ViewTransform)] Transform transform, IHealthModelObserver health,
            IFogVisibilityFacade fog, ShipType shipType)
        {
            _layer = layer;
            _clearance = clearance;
            Transform = transform;
            _health = health;
            _fog = fog;
            _shipType = shipType;
        }

        public Transform Transform { get; }
        public Bounds LocalBounds { get; private set; }
        public bool IsActive => !_health.IsDestroyed && !_fog.IsHiddenByFog;

        public void Initialize()
        {
            if (_registered || _health.IsDestroyed) return;
            LocalBounds = _clearance.GetLocalHullBounds(_shipType);
            _registered = true;
            _layer.RegisterHull(this);
        }

        public void Respawn() => Initialize();
        public void Despawn() => LateDispose();

        public void LateDispose()
        {
            if (!_registered) return;
            _registered = false;
            _layer.UnregisterHull(this);
        }
    }
}
