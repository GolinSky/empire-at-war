using System.Collections.Generic;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;
using ViewComponents;
using Zenject;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>
    /// Reveals the hardpoints of the ship under the cursor, resolves which marker is hovered
    /// and keeps the player's explicitly targeted hardpoint highlighted.
    /// </summary>
    public sealed class HardPointOverlayPresenter : IInitializable, ILateDisposable, ITickable
    {
        private const float HIT_HALF_SIZE = 20f;

        private readonly IHardPointOverlayView _view;
        private readonly HardPointOverlayModel _model;
        private readonly HardPointOverlayData _data;
        private readonly ISelectionQuery _selectionQuery;
        private readonly IInputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly IUnitOrderService _orderService;
        private readonly ICinematicCameraModelObserver _cinematicCamera;
        private readonly FogOfWarSystem _fogOfWarSystem;

        public HardPointOverlayPresenter(
            IHardPointOverlayView view,
            HardPointOverlayModel model,
            HardPointOverlayData data,
            ISelectionQuery selectionQuery,
            IInputService inputService,
            ICameraService cameraService,
            IUnitOrderService orderService,
            ICinematicCameraModelObserver cinematicCamera,
            FogOfWarSystem fogOfWarSystem)
        {
            _view = view;
            _model = model;
            _data = data;
            _selectionQuery = selectionQuery;
            _inputService = inputService;
            _cameraService = cameraService;
            _orderService = orderService;
            _cinematicCamera = cinematicCamera;
            _fogOfWarSystem = fogOfWarSystem;
        }

        public void Initialize()
        {
            _orderService.OrderIssued += HandleOrderIssued;
        }

        public void LateDispose()
        {
            _orderService.OrderIssued -= HandleOrderIssued;
        }

        public void Tick()
        {
            if (_model.TargetedShip != null && !IsTargetStillValid())
            {
                _model.ClearTarget();
            }

            if (!_inputService.SupportsHover || _cinematicCamera.IsActive)
            {
                _model.Inspect(null, UnitOrderModel.NO_HARD_POINT, false);
                _view.HideMarkersFrom(0);
                _view.HideTooltip();
                return;
            }

            UpdateInspection(_inputService.TouchPosition);
            Render();
        }

        private void UpdateInspection(Vector2 cursor)
        {
            float hitHalfSize = HIT_HALF_SIZE * _view.ScaleFactor;
            IEntity ship = _model.InspectedShip;
            int hovered = UnitOrderModel.NO_HARD_POINT;

            // Markers can reach beyond the hull, so the inspected ship stays while the cursor is on one of them.
            if (ship != null && IsVisible(ship))
            {
                hovered = FindMarkerAt(ship, cursor, hitHalfSize);
            }

            if (hovered == UnitOrderModel.NO_HARD_POINT)
            {
                ship = GetShipUnderCursor(cursor);
                if (ship != null)
                {
                    hovered = FindMarkerAt(ship, cursor, hitHalfSize);
                }
            }

            bool isTargetable = hovered != UnitOrderModel.NO_HARD_POINT &&
                                !ship.HealthModel.IsDestroyed &&
                                !GetHardPoints(ship)[hovered].IsDestroyed;
            _model.Inspect(ship, hovered, isTargetable);
        }

        private void Render()
        {
            int slot = 0;
            IEntity inspected = _model.InspectedShip;
            if (inspected != null)
            {
                IReadOnlyList<IHardPointStatus> hardPoints = GetHardPoints(inspected);
                for (int id = 0; id < hardPoints.Count; id++)
                {
                    if (TryShowMarker(slot, inspected, hardPoints[id]))
                    {
                        slot++;
                    }
                }
            }

            IEntity targeted = _model.TargetedShip;
            if (targeted != null && (inspected == null || targeted.Id != inspected.Id) && IsVisible(targeted) &&
                TryShowMarker(slot, targeted, GetHardPoints(targeted)[_model.TargetedHardPointId]))
            {
                slot++;
            }

            _view.HideMarkersFrom(slot);
            RenderTooltip();
        }

        private bool TryShowMarker(int slot, IEntity ship, IHardPointStatus hardPoint)
        {
            if (!TryGetScreenPosition(hardPoint, out Vector2 screenPosition))
            {
                return false;
            }

            bool isHovered = _model.InspectedShip != null && _model.InspectedShip.Id == ship.Id &&
                             _model.HoveredHardPointId == hardPoint.Id;
            _view.ShowMarker(slot, new HardPointMarkerData(
                screenPosition,
                _data.Get(hardPoint.HardPointType).Icon,
                hardPoint.HealthPercentage,
                isHovered,
                _model.IsTargeted(ship, hardPoint.Id),
                hardPoint.IsDestroyed));
            return true;
        }

        private void RenderTooltip()
        {
            IEntity ship = _model.InspectedShip;
            if (ship == null || _model.HoveredHardPointId == UnitOrderModel.NO_HARD_POINT)
            {
                _view.HideTooltip();
                return;
            }

            IHardPointsFacade hardPoints = ship.GetFacade<IHardPointsFacade>();
            IHardPointStatus hardPoint = hardPoints.HardPoints[_model.HoveredHardPointId];
            HardPointOverlayEntry entry = _data.Get(hardPoint.HardPointType);
            TryGetScreenPosition(hardPoint, out Vector2 anchor);

            string title = $"{entry.DisplayName} {GetOrdinal(hardPoints.HardPoints, hardPoint):00}";
            string status = hardPoint.IsDestroyed ? "Destroyed" : "Operational";
            string body = $"{status} · HP: {hardPoint.Health:N0} / {hardPoint.MaxHealth:N0}";
            if (hardPoints.MaxShields > 0f)
            {
                body += $"\nShip shields: {ship.HealthModel.Shields:N0} / {hardPoints.MaxShields:N0} — shared";
            }

            body += $"\n{entry.Description}";
            _view.ShowTooltip(anchor, title, body);
        }

        private int FindMarkerAt(IEntity ship, Vector2 cursor, float hitHalfSize)
        {
            IReadOnlyList<IHardPointStatus> hardPoints = GetHardPoints(ship);

            // Keep the current marker while the cursor stays on it so overlapping markers do not flicker.
            int hovered = _model.InspectedShip != null && _model.InspectedShip.Id == ship.Id
                ? _model.HoveredHardPointId
                : UnitOrderModel.NO_HARD_POINT;
            if (hovered != UnitOrderModel.NO_HARD_POINT &&
                TryGetScreenPosition(hardPoints[hovered], out Vector2 hoveredPosition) &&
                IsInside(hoveredPosition, cursor, hitHalfSize))
            {
                return hovered;
            }

            int closest = UnitOrderModel.NO_HARD_POINT;
            float closestDistance = float.MaxValue;
            foreach (IHardPointStatus hardPoint in hardPoints)
            {
                if (!TryGetScreenPosition(hardPoint, out Vector2 position) ||
                    !IsInside(position, cursor, hitHalfSize))
                {
                    continue;
                }

                float distance = (position - cursor).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = hardPoint.Id;
                }
            }

            return closest;
        }

        private IEntity GetShipUnderCursor(Vector2 cursor)
        {
            if (!_selectionQuery.TryFindAt(cursor, out SelectionEntry selection))
            {
                return null;
            }

            IEntity entity = selection.Entity;
            return entity.TryGetFacade(out IHardPointsFacade _) && IsVisible(entity) ? entity : null;
        }

        private bool IsTargetStillValid()
        {
            IEntity ship = _model.TargetedShip;
            return !ship.HealthModel.IsDestroyed && !GetHardPoints(ship)[_model.TargetedHardPointId].IsDestroyed;
        }

        private bool IsVisible(IEntity ship)
        {
            return !ship.HealthModel.IsDestroyed &&
                   (ship.PlayerType == PlayerType.Player ||
                    !_fogOfWarSystem.IsHidden(ship.GetFacade<IEntityTransformFacade>().Transform.position));
        }

        private bool TryGetScreenPosition(IHardPointModel hardPoint, out Vector2 screenPosition)
        {
            Vector3 worldPosition = hardPoint.Position;
            screenPosition = _cameraService.WorldToScreenPoint(worldPosition);
            return _cameraService.WorldToViewportPoint(worldPosition).z > 0f;
        }

        private void HandleOrderIssued(UnitOrder order)
        {
            if (order.Issuer != PlayerType.Player)
            {
                return;
            }

            // Any new player order replaces the previous one, so the highlight follows the latest order.
            if (order.TargetHardPointId == UnitOrderModel.NO_HARD_POINT)
            {
                _model.ClearTarget();
                return;
            }

            _model.SetTarget(order.Target, order.TargetHardPointId);
        }

        private static IReadOnlyList<IHardPointStatus> GetHardPoints(IEntity ship) =>
            ship.GetFacade<IHardPointsFacade>().HardPoints;

        private static bool IsInside(Vector2 position, Vector2 cursor, float halfSize) =>
            Mathf.Abs(position.x - cursor.x) <= halfSize && Mathf.Abs(position.y - cursor.y) <= halfSize;

        private static int GetOrdinal(IReadOnlyList<IHardPointStatus> hardPoints, IHardPointStatus hardPoint)
        {
            int ordinal = 0;
            for (int id = 0; id <= hardPoint.Id; id++)
            {
                if (hardPoints[id].HardPointType == hardPoint.HardPointType)
                {
                    ordinal++;
                }
            }

            return ordinal;
        }
    }
}
