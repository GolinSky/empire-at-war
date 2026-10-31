using EmpireAtWar.Components.Radar;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Extensions;
using UnityEngine;
using VContainer;

namespace EmpireAtWar.Services.Enemy
{
    public sealed class ShipOrderTelemetrySource
    {
        private readonly Transform _viewTransform;
        private readonly IRadarModelObserver _radar;

        public long EntityId { get; }
        public PlayerId Owner { get; }
        public Vector3 Forward => _viewTransform.forward;
        public float RadarRange => _radar.Range;

        public ShipOrderTelemetrySource(long entityId, PlayerId owner,
            [Key(EntityBindType.ViewTransform)] Transform viewTransform, IRadarModelObserver radar)
        {
            EntityId = entityId;
            Owner = owner;
            _viewTransform = viewTransform;
            _radar = radar;
        }
    }
}
