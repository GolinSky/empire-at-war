using EmpireAtWar.Components.Ship.Health.HardPointOverlay;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Ui.Base;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    public sealed class UnitHoverService : IHoveredEntityObserver, IInitializable, ITickable, ILateDisposable
    {
        private readonly ISelectionQuery _query;
        private readonly IPointerInput _pointerInput;
        private readonly IUiHitTest _ui;
        private readonly IPointerGestures _gestures;
        private readonly IHudVisibilityObserver _hud;
        private readonly IHardPointHoverObserver _hardPointHover;
        private readonly IEntityLocator _entities;
        private IEntity _hovered;
        private int _hardPointId;
        private bool _hasHardPoint;
        private bool _dragging;

        public bool CanHover => _hud.IsHudVisible && !_dragging && !_ui.IsOverUi(_pointerInput.Position);

        public UnitHoverService(ISelectionQuery query, IPointerInput pointerInput, IUiHitTest ui,
            IPointerGestures gestures, IHudVisibilityObserver hud, IHardPointHoverObserver hardPointHover,
            IEntityLocator entities)
        {
            _query = query;
            _pointerInput = pointerInput;
            _ui = ui;
            _gestures = gestures;
            _hud = hud;
            _hardPointHover = hardPointHover;
            _entities = entities;
        }

        public void Initialize()
        {
            _gestures.DragStarted += StartDrag;
            _gestures.DragEnded += EndDrag;
            _gestures.DragCanceled += CancelDrag;
            _entities.EntityRemoved += OnEntityRemoved;
        }

        public void Tick()
        {
            _hasHardPoint = false;
            if (!CanHover)
            {
                SetHovered(null);
                return;
            }

            if (_hardPointHover.TryGetHovered(out IEntity owner, out int hardPointId) && IsVisible(owner))
            {
                _hasHardPoint = true;
                _hardPointId = hardPointId;
                SetHovered(owner);
            }
            else if (_query.TryFindAt(_pointerInput.Position, out SelectionEntry selection) && IsVisible(selection.Entity))
                SetHovered(selection.Entity);
            else
                SetHovered(null);
        }

        public bool TryGetHovered(out IEntity entity)
        {
            entity = _hovered;
            return entity != null && CanHover && IsVisible(entity);
        }

        public bool TryGetHardPoint(out IEntity entity, out int hardPointId)
        {
            hardPointId = _hardPointId;
            return TryGetHovered(out entity) && _hasHardPoint;
        }

        public void LateDispose()
        {
            _gestures.DragStarted -= StartDrag;
            _gestures.DragEnded -= EndDrag;
            _gestures.DragCanceled -= CancelDrag;
            _entities.EntityRemoved -= OnEntityRemoved;
            _hasHardPoint = false;
            SetHovered(null);
        }

        private bool IsVisible(IEntity entity) => !entity.HealthModel.IsDestroyed && !entity.IsHiddenByFog() &&
            _entities.TryGetEntity(entity.Id, out IEntity current) && ReferenceEquals(current, entity);

        private void SetHovered(IEntity entity)
        {
            if (ReferenceEquals(_hovered, entity)) return;
            if (_hovered != null && _hovered.TryGetFacade(out IEntityHighlightFacade previous))
                previous.SetHovered(false);
            _hovered = entity;
            if (_hovered != null && _hovered.TryGetFacade(out IEntityHighlightFacade current))
                current.SetHovered(true);
        }

        private void OnEntityRemoved(IEntity entity)
        {
            if (!ReferenceEquals(_hovered, entity)) return;
            _hasHardPoint = false;
            SetHovered(null);
        }

        private void StartDrag(Vector2 point)
        {
            _dragging = true;
            _hasHardPoint = false;
            SetHovered(null);
        }

        private void EndDrag(Vector2 point) => _dragging = false;
        private void CancelDrag() => _dragging = false;
    }
}
