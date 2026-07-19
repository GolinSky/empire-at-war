using System.Collections.Generic;
using EmpireAtWar.Components.Selection.Marquee;
using EmpireAtWar.Components.Ship.Selection;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityCommands;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Ship;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    public interface ISelectionQuery
    {
        bool TryFindAt(Vector2 screenPosition, out SelectionEntry selection);
        void CollectSameShipType(SelectionEntry selected, ICollection<SelectionEntry> results);
        void CollectInside(MarqueeRectangle rectangle, ICollection<SelectionEntry> results);
    }

    public sealed class SelectionQuery : ISelectionQuery
    {
        private readonly ICameraService _cameraService;
        private readonly IEntityLocator _entityLocator;
        private readonly List<MarqueeCandidate> _marqueeCandidates = new List<MarqueeCandidate>();
        private readonly List<MarqueeCandidate> _marqueeResults = new List<MarqueeCandidate>();

        public SelectionQuery(ICameraService cameraService, IEntityLocator entityLocator)
        {
            _cameraService = cameraService;
            _entityLocator = entityLocator;
        }

        public bool TryFindAt(Vector2 screenPosition, out SelectionEntry selection)
        {
            RaycastHit raycastHit = _cameraService.ScreenPointToRay(screenPosition);
            if (raycastHit.collider != null &&
                _entityLocator.TryGetEntity(raycastHit, out IEntity entity) &&
                entity.TryGetCommand(out IEntitySelectionCommand command))
            {
                selection = new SelectionEntry(entity, command);
                return true;
            }

            selection = default;
            return false;
        }

        public void CollectSameShipType(SelectionEntry selected, ICollection<SelectionEntry> results)
        {
            if (!(selected.Entity.Model is IShipModelObserver selectedShip))
            {
                return;
            }

            results.Add(selected);
            foreach (IEntity entity in _entityLocator.Entities)
            {
                if (entity.Id == selected.Entity.Id ||
                    entity.PlayerType != selected.Entity.PlayerType ||
                    !(entity.Model is IShipModelObserver ship) ||
                    ship.ShipType != selectedShip.ShipType ||
                    !entity.TryGetCommand(out IEntitySelectionCommand command))
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
                if (entity.PlayerType != PlayerType.Player ||
                    !entity.TryGetCommand(out IEntitySelectionCommand command) ||
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

            for (int i = 0; i < _marqueeResults.Count; i++)
            {
                results.Add(_marqueeResults[i].Entry);
            }
        }

        private readonly struct MarqueeCandidate
        {
            public MarqueeCandidate(SelectionEntry entry, MarqueePoint screenPoint)
            {
                Entry = entry;
                ScreenPoint = screenPoint;
            }

            public SelectionEntry Entry { get; }
            public MarqueePoint ScreenPoint { get; }
        }
    }
}
