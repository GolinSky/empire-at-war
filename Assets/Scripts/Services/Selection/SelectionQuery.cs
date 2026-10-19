using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Components.Squadrons.Icon;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Services.Camera;
using UnityEngine;
using EmpireAtWar.Entities.Units;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionQuery
    {
        bool TryFindAt(Vector2 screenPosition, out SelectionEntry selection);

        void CollectAt(Vector2 screenPosition, IList<SelectionEntry> results);

        void CollectSameShipType(SelectionEntry selected, ICollection<SelectionEntry> results);

        void CollectAllPlayerUnits(ICollection<SelectionEntry> results);

        void CollectVisiblePlayerUnits(ICollection<SelectionEntry> results);

        void CollectInside(MarqueeRectangle rectangle, ICollection<SelectionEntry> results);
    }

    public sealed class SelectionQuery : ISelectionQuery
    {
        private const int MAX_RAYCAST_HITS = 32;

        private static readonly Comparison<PointCandidate> COMPARE_POINT_CANDIDATES = ComparePointCandidates;

        private readonly ICameraService _cameraService;
        private readonly IEntityLocator _entityLocator;
        private readonly ILocalPlayer _localPlayer;

        private readonly List<MarqueeCandidate> _marqueeCandidates = new List<MarqueeCandidate>();
        private readonly List<MarqueeCandidate> _marqueeResults = new List<MarqueeCandidate>();
        private readonly RaycastHit[] _raycastHits = new RaycastHit[MAX_RAYCAST_HITS];
        private readonly List<PointCandidate> _pointCandidates = new List<PointCandidate>();
        private readonly List<SelectionEntry> _pointResults = new List<SelectionEntry>();

        public SelectionQuery(ICameraService cameraService, IEntityLocator entityLocator, ILocalPlayer localPlayer)
        {
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _entityLocator = entityLocator;
        }

        public bool TryFindAt(Vector2 screenPosition, out SelectionEntry selection)
        {
            _pointResults.Clear();
            CollectAt(screenPosition, _pointResults);
            selection = _pointResults.Count > 0 ? _pointResults[0] : default;
            return _pointResults.Count > 0;
        }

        public void CollectAt(Vector2 screenPosition, IList<SelectionEntry> results)
        {
            // Squadron icons are drawn over the scene, so they come before whatever the ray hits behind them.
            foreach (IEntity iconEntity in _entityLocator.Entities)
            {
                if (!iconEntity.HealthModel.IsDestroyed &&
                    iconEntity.TryGetFacade(out ISquadronIconCommand icon) &&
                    icon.ContainsScreenPoint(screenPosition) &&
                    iconEntity.TryGetFacade(out IEntitySelectionFacade iconSelection))
                {
                    results.Add(new SelectionEntry(iconEntity, iconSelection));
                }
            }

            // Structures are large and would hide ships under or behind them, so units anywhere
            // along the ray come before structures; within the same rank the closest hit comes first.
            _pointCandidates.Clear();
            int hitCount = _cameraService.ScreenPointRaycastAll(screenPosition, _raycastHits);
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

                AddPointCandidate(new PointCandidate(new SelectionEntry(entity, command), entity.IsUnit(), hit.distance));
            }

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
                    !entity.TryGetFacade(out IEntitySelectionFacade command))
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
                    !(command is ISelectionPositionProvider positionProvider))
                {
                    continue;
                }

                Vector3 viewportPoint = _cameraService.WorldToViewportPoint(positionProvider.WorldPosition);
                if (viewportPoint.z <= 0f ||
                    viewportPoint.x < 0f ||
                    viewportPoint.x > 1f ||
                    viewportPoint.y < 0f ||
                    viewportPoint.y > 1f)
                {
                    continue;
                }

                results.Add(new SelectionEntry(entity, command));
            }
        }

        public void CollectInside(MarqueeRectangle rectangle, ICollection<SelectionEntry> results)
        {
            _marqueeCandidates.Clear();
            _marqueeResults.Clear();

            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (!_localPlayer.IsLocal(entity.Owner) ||
                    entity.HealthModel.IsDestroyed ||
                    !entity.TryGetFacade(out IEntitySelectionFacade command) ||
                    !(command is ISelectionPositionProvider positionProvider))
                {
                    continue;
                }

                Vector3 viewportPoint = _cameraService.WorldToViewportPoint(positionProvider.WorldPosition);
                if (viewportPoint.z <= 0f)
                {
                    continue;
                }

                Vector2 screenPoint = _cameraService.WorldToScreenPoint(positionProvider.WorldPosition);
                _marqueeCandidates.Add(new MarqueeCandidate(
                    new SelectionEntry(entity, command),
                    new MarqueePoint(screenPoint.x, screenPoint.y)));
            }

            MarqueeSelectionUtility.CollectInside(
                _marqueeCandidates,
                rectangle,
                candidate => candidate.ScreenPoint,
                _marqueeResults);

            // Like the select-all shortcuts, a box that catches units selects only the units;
            // structures are box-selected only when nothing else is inside.
            bool hasUnits = false;
            for (int i = 0; i < _marqueeResults.Count; i++)
            {
                hasUnits |= _marqueeResults[i].Entry.Entity.IsUnit();
            }

            for (int i = 0; i < _marqueeResults.Count; i++)
            {
                SelectionEntry entry = _marqueeResults[i].Entry;
                if (!hasUnits || entry.Entity.IsUnit())
                {
                    results.Add(entry);
                }
            }
        }

        private readonly struct PointCandidate
        {
            public SelectionEntry Entry { get; }
            public bool IsUnit { get; }
            public float Distance { get; }

            public PointCandidate(SelectionEntry entry, bool isUnit, float distance)
            {
                Entry = entry;
                IsUnit = isUnit;
                Distance = distance;
            }
        }

        private readonly struct MarqueeCandidate
        {
            public SelectionEntry Entry { get; }
            public MarqueePoint ScreenPoint { get; }

            public MarqueeCandidate(SelectionEntry entry, MarqueePoint screenPoint)
            {
                Entry = entry;
                ScreenPoint = screenPoint;
            }
        }
    }
}
