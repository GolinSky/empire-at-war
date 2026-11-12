using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.CaptureSites;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Vision;

namespace EmpireAtWar.Services.TacticalMarkers
{
    /// <summary>
    /// Host: ends Capture when the team owns the site, and Defend once hostiles seen near it are gone.
    /// A Defend placed before any hostile arrives stays for its full lifetime.
    /// </summary>
    public sealed class MarkerResolutionWatcher : IInitializable, ITickable, ILateDisposable
    {
        private const float CHECK_INTERVAL = 0.5f;
        private readonly TacticalMarkerRegistry _markers;
        private readonly TacticalMarkerTargets _targets;
        private readonly TacticalMarkerSettings _settings;
        private readonly IPlayerRelations _relations;
        private readonly IEntityLocator _entities;
        private readonly IVisionService _vision;
        private readonly HashSet<long> _contested = new HashSet<long>();
        private float _nextCheck;

        public MarkerResolutionWatcher(TacticalMarkerRegistry markers, TacticalMarkerTargets targets,
            TacticalMarkerSettings settings, IPlayerRelations relations, IEntityLocator entities, IVisionService vision)
        {
            _markers = markers;
            _targets = targets;
            _settings = settings;
            _relations = relations;
            _entities = entities;
            _vision = vision;
        }

        public void Initialize() => _markers.Removed += HandleRemoved;

        public void LateDispose()
        {
            _markers.Removed -= HandleRemoved;
            _contested.Clear();
        }

        private void HandleRemoved(TacticalMarker marker) => _contested.Remove(marker.Id);

        public void Tick()
        {
            if (_markers.Time < _nextCheck - CHECK_INTERVAL) _nextCheck = _markers.Time;
            if (_markers.Time < _nextCheck) return;
            _nextCheck = _markers.Time + CHECK_INTERVAL;
            for (int i = _markers.Markers.Count - 1; i >= 0; i--)
            {
                TacticalMarker marker = _markers.Markers[i];
                if (!_vision.IsVisible(marker.Sender, marker.Point)) continue;
                ICaptureSite site = _targets.FindSite(marker.TargetId);
                if (marker.Type == TacticalMarkerType.Capture && site != null && _relations.IsAllied(marker.Sender, site.Owner))
                    _markers.Resolve(marker.Id);
                else if (marker.Type == TacticalMarkerType.Defend)
                {
                    if (HasHostiles(marker)) _contested.Add(marker.Id);
                    else if (_contested.Contains(marker.Id)) _markers.Resolve(marker.Id);
                }
            }
        }

        private bool HasHostiles(TacticalMarker marker)
        {
            foreach (IEntity entity in _entities.Entities)
                if (_relations.IsHostile(marker.Sender, entity.Owner) && !entity.HealthModel.IsDestroyed &&
                    entity.TryGetFacade(out IEntityTransformFacade transform) &&
                    (transform.Transform.position - marker.Point).sqrMagnitude <= _settings.ObjectiveRadius * _settings.ObjectiveRadius &&
                    _vision.IsVisible(marker.Sender, transform.Transform.position)) return true;
            return false;
        }
    }
}
