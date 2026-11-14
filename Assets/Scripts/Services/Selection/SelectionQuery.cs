using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.SpaceStation;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Selection;
using Unity.Profiling;
using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionQuery
    {
        bool TryFindAt(Vector2 screenPosition, out SelectionEntry selection);

        bool TryFindMarkerAt(Vector2 screenPosition, out SelectionEntry selection);

        bool IsMarkerAvailable(SelectionEntry selection);

        void CollectAt(Vector2 screenPosition, IList<SelectionEntry> results);

        void CollectSameShipType(SelectionEntry selected, ICollection<SelectionEntry> results);

        void CollectAllPlayerUnits(ICollection<SelectionEntry> results);

        void CollectVisiblePlayerUnits(ICollection<SelectionEntry> results);

        void CollectInside(MarqueeRectangle rectangle, ICollection<SelectionEntry> results);
    }

    public sealed class SelectionQuery : ISelectionQuery
    {
        private const int INITIAL_RAYCAST_CAPACITY = 32;

        private static readonly Comparison<PointCandidate> COMPARE_POINT_CANDIDATES = ComparePointCandidates;
        private static readonly Comparison<MarkerCandidate> COMPARE_MARKER_CANDIDATES = CompareMarkerCandidates;
        private static readonly ProfilerMarker MARKER_HIT_QUERY = new ProfilerMarker("Battle.Markers.HitQuery");

        private readonly ICameraService _cameraService;
        private readonly IEntityLocator _entityLocator;
        private readonly ILocalPlayer _localPlayer;
        private readonly IBattleMarkerLayer _markerLayer;

        private readonly List<SelectionEntry> _marqueeResults = new List<SelectionEntry>();
        private readonly HashSet<long> _marqueeMarkerIds = new HashSet<long>();
        private RaycastHit[] _raycastHits = new RaycastHit[INITIAL_RAYCAST_CAPACITY];
        private readonly List<PointCandidate> _pointCandidates = new List<PointCandidate>();
        private readonly List<MarkerCandidate> _markerCandidates = new List<MarkerCandidate>();
        private readonly List<SelectionEntry> _pointResults = new List<SelectionEntry>();

        public SelectionQuery(ICameraService cameraService, IEntityLocator entityLocator, ILocalPlayer localPlayer,
            IBattleMarkerLayer markerLayer)
        {
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _entityLocator = entityLocator;
            _markerLayer = markerLayer;
        }

        public bool TryFindAt(Vector2 screenPosition, out SelectionEntry selection)
        {
            if (TryFindMarkerAt(screenPosition, out selection))
                return true;

            _pointResults.Clear();
            CollectHullsAt(screenPosition, _pointResults);
            selection = _pointResults.Count > 0 ? _pointResults[0] : default;
            return _pointResults.Count > 0;
        }

        public bool TryFindMarkerAt(Vector2 screenPosition, out SelectionEntry selection)
        {
            CollectMarkerCandidates(screenPosition);
            selection = _markerCandidates.Count > 0 ? _markerCandidates[0].Entry : default;
            return _markerCandidates.Count > 0;
        }

        public bool IsMarkerAvailable(SelectionEntry selection)
        {
            using ProfilerMarker.AutoScope sample = MARKER_HIT_QUERY.Auto();
            IReadOnlyList<IBattleMarker> markers = _markerLayer.Markers;
            for (int i = 0; i < markers.Count; i++)
            {
                IBattleMarker marker = markers[i];
                if (marker.IsShown && marker.EntityId == selection.Entity.Id &&
                    marker.DrawOrder == selection.MarkerDrawOrder)
                    return TryResolveMarker(marker, out SelectionEntry current) &&
                           ReferenceEquals(current.Entity, selection.Entity);
            }

            return false;
        }

        public void CollectAt(Vector2 screenPosition, IList<SelectionEntry> results)
        {
            CollectMarkerCandidates(screenPosition);
            for (int i = 0; i < _markerCandidates.Count; i++)
            {
                SelectionEntry entry = _markerCandidates[i].Entry;
                if (!Contains(results, entry.Entity))
                    results.Add(entry);
            }

            CollectHullsAt(screenPosition, results);
        }

        private void CollectMarkerCandidates(Vector2 screenPosition)
        {
            using ProfilerMarker.AutoScope sample = MARKER_HIT_QUERY.Auto();
            _markerCandidates.Clear();
            IReadOnlyList<IBattleMarker> markers = _markerLayer.Markers;
            for (int i = 0; i < markers.Count; i++)
            {
                IBattleMarker marker = markers[i];
                if (!marker.IsShown || !marker.ContainsScreenPoint(screenPosition) ||
                    !TryResolveMarker(marker, out SelectionEntry entry))
                    continue;

                float distance = (marker.ScreenRect.center - screenPosition).sqrMagnitude;
                _markerCandidates.Add(new MarkerCandidate(entry, marker.IsSquadron, distance));
            }

            _markerCandidates.Sort(COMPARE_MARKER_CANDIDATES);
        }

        private bool TryResolveMarker(IBattleMarker marker, out SelectionEntry entry)
        {
            if (_entityLocator.TryGetEntity(marker.EntityId, out IEntity entity) &&
                !entity.HealthModel.IsDestroyed && !entity.IsHiddenByFog() &&
                entity.TryGetFacade(out IEntitySelectionFacade selection))
            {
                entry = new SelectionEntry(entity, selection, marker.DrawOrder);
                return true;
            }

            entry = default;
            return false;
        }

        private static int CompareMarkerCandidates(MarkerCandidate a, MarkerCandidate b)
        {
            int comparison = a.IsSquadron.CompareTo(b.IsSquadron);
            if (comparison != 0) return comparison;
            comparison = Nullable.Compare(b.Entry.MarkerDrawOrder, a.Entry.MarkerDrawOrder);
            if (comparison != 0) return comparison;
            comparison = a.Distance.CompareTo(b.Distance);
            return comparison != 0 ? comparison : a.Entry.Entity.Id.CompareTo(b.Entry.Entity.Id);
        }

        private void CollectHullsAt(Vector2 screenPosition, IList<SelectionEntry> results)
        {
            // Units normally precede structures; deep hulls occluded by a station share its rank.
            _pointCandidates.Clear();
            int hitCount = _cameraService.ScreenPointRaycastAll(screenPosition, _raycastHits);
            // A full buffer may have dropped hits, including the hull under a squadron.
            while (hitCount == _raycastHits.Length)
            {
                _raycastHits = new RaycastHit[_raycastHits.Length * 2];
                hitCount = _cameraService.ScreenPointRaycastAll(screenPosition, _raycastHits);
            }
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _raycastHits[i];
                if (!_entityLocator.TryGetEntity(hit, out IEntity entity) ||
                    entity.HealthModel.IsDestroyed ||
                    entity.IsHiddenByFog() ||
                    !entity.TryGetFacade(out IEntitySelectionFacade command))
                {
                    continue;
                }

                AddPointCandidate(new PointCandidate(new SelectionEntry(entity, command), entity.IsUnit(), hit.distance, hit.point.y));
            }

            RankUnitsBelowStations();
            _pointCandidates.Sort(COMPARE_POINT_CANDIDATES);
            for (int i = 0; i < _pointCandidates.Count; i++)
            {
                if (!Contains(results, _pointCandidates[i].Entry.Entity))
                {
                    results.Add(_pointCandidates[i].Entry);
                }
            }
        }

        // An entity with several colliders keeps only its closest hit.
        private void AddPointCandidate(PointCandidate candidate)
        {
            for (int i = 0; i < _pointCandidates.Count; i++)
            {
                if (_pointCandidates[i].Entry.Entity.Id == candidate.Entry.Entity.Id)
                {
                    if (candidate.Distance < _pointCandidates[i].Distance)
                    {
                        _pointCandidates[i] = candidate;
                    }

                    return;
                }
            }

            _pointCandidates.Add(candidate);
        }

        private void RankUnitsBelowStations()
        {
            for (int i = 0; i < _pointCandidates.Count; i++)
            {
                PointCandidate unit = _pointCandidates[i];
                if (!unit.IsUnit)
                    continue;

                for (int j = 0; j < _pointCandidates.Count; j++)
                {
                    PointCandidate structure = _pointCandidates[j];
                    if (structure.Entry.Entity.IsUnit() || structure.Distance >= unit.Distance ||
                        !structure.Entry.Entity.TryGetFacade(out IStationHullFacade hull) ||
                        unit.HitY >= hull.HullBottom)
                        continue;

                    _pointCandidates[i] = new PointCandidate(unit.Entry, false, unit.Distance, unit.HitY);
                    break;
                }
            }
        }

        private static bool Contains(IList<SelectionEntry> entries, IEntity entity)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].Entity.Id == entity.Id)
                {
                    return true;
                }
            }

            return false;
        }

        private static int ComparePointCandidates(PointCandidate a, PointCandidate b)
        {
            if (a.IsUnit != b.IsUnit)
            {
                return a.IsUnit ? -1 : 1;
            }

            return a.Distance.CompareTo(b.Distance);
        }

        public void CollectSameShipType(SelectionEntry selected, ICollection<SelectionEntry> results)
        {
            if (!selected.Entity.IsUnit())
            {
                return;
            }

            results.Add(selected);
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Id == selected.Entity.Id ||
                    entity.HealthModel.IsDestroyed ||
                    entity.Owner != selected.Entity.Owner ||
                    !selected.Entity.IsSameUnitType(entity) ||
                    !entity.TryGetFacade(out IEntitySelectionFacade command) ||
                    !IsOnScreen(command))
                {
                    continue;
                }

                results.Add(new SelectionEntry(entity, command));
            }
        }

        public void CollectAllPlayerUnits(ICollection<SelectionEntry> results)
        {
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (!_localPlayer.IsLocal(entity.Owner) ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.IsUnit() ||
                    !entity.TryGetFacade(out IEntitySelectionFacade command))
                {
                    continue;
                }

                results.Add(new SelectionEntry(entity, command));
            }
        }

        public void CollectVisiblePlayerUnits(ICollection<SelectionEntry> results)
        {
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (!_localPlayer.IsLocal(entity.Owner) ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.IsUnit() ||
                    !entity.TryGetFacade(out IEntitySelectionFacade command) ||
                    !IsOnScreen(command))
                {
                    continue;
                }

                results.Add(new SelectionEntry(entity, command));
            }
        }

        private bool IsOnScreen(IEntitySelectionFacade command)
        {
            if (!(command is ISelectionPositionProvider positionProvider))
            {
                return false;
            }

            Vector3 viewportPoint = _cameraService.WorldToViewportPoint(positionProvider.WorldPosition);
            return viewportPoint.z > 0f &&
                viewportPoint.x >= 0f &&
                viewportPoint.x <= 1f &&
                viewportPoint.y >= 0f &&
                viewportPoint.y <= 1f;
        }

        public void CollectInside(MarqueeRectangle rectangle, ICollection<SelectionEntry> results)
        {
            _marqueeResults.Clear();
            using (MARKER_HIT_QUERY.Auto())
            {
                _marqueeMarkerIds.Clear();
                IReadOnlyList<IBattleMarker> markers = _markerLayer.Markers;
                for (int i = 0; i < markers.Count; i++)
                {
                    IBattleMarker marker = markers[i];
                    if (marker.IsShown && marker.OverlapsMarquee(rectangle))
                        _marqueeMarkerIds.Add(marker.EntityId);
                }
            }

            bool hasUnits = false;
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (!_localPlayer.IsLocal(entity.Owner) || entity.HealthModel.IsDestroyed || entity.IsHiddenByFog() ||
                    !entity.TryGetFacade(out IEntitySelectionFacade command) ||
                    (!_marqueeMarkerIds.Contains(entity.Id) && !IsInsideMarquee(entity, command, rectangle)))
                {
                    continue;
                }

                _marqueeResults.Add(new SelectionEntry(entity, command));
                hasUnits |= entity.IsUnit();
            }

            // Like the select-all shortcuts, a box that catches units selects only the units;
            // structures are box-selected only when nothing else is inside.
            for (int i = 0; i < _marqueeResults.Count; i++)
            {
                SelectionEntry entry = _marqueeResults[i];
                if (!hasUnits || entry.Entity.IsUnit())
                {
                    results.Add(entry);
                }
            }
        }

        private bool IsInsideMarquee(IEntity entity, IEntitySelectionFacade command, MarqueeRectangle rectangle)
        {
            if (command is ISelectionPositionProvider positionProvider)
            {
                Vector3 position = positionProvider.WorldPosition;
                if (_cameraService.WorldToViewportPoint(position).z > 0f)
                {
                    Vector2 screen = _cameraService.WorldToScreenPoint(position);
                    if (rectangle.Contains(new MarqueePoint(screen.x, screen.y)))
                        return true;
                }
            }

            return entity.TryGetFacade(out ShipMarqueeHull hull) &&
                   hull.Overlaps(rectangle, _cameraService.SceneCamera);
        }

        private readonly struct MarkerCandidate
        {
            public readonly SelectionEntry Entry;
            public readonly bool IsSquadron;
            public readonly float Distance;

            public MarkerCandidate(SelectionEntry entry, bool isSquadron, float distance)
            {
                Entry = entry;
                IsSquadron = isSquadron;
                Distance = distance;
            }
        }

        private readonly struct PointCandidate
        {
            public SelectionEntry Entry { get; }
            public bool IsUnit { get; }
            public float Distance { get; }
            public float HitY { get; }

            public PointCandidate(SelectionEntry entry, bool isUnit, float distance, float hitY)
            {
                Entry = entry;
                IsUnit = isUnit;
                Distance = distance;
                HitY = hitY;
            }
        }
    }
}
