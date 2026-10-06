using System.Collections.Generic;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.CinematicCamera.Model;
using EmpireAtWar.Models.Health;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;
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
        /// <summary>Reference pixels between neighbouring marker centres at which markers keep their full size.</summary>
        private const float FULL_SIZE_SPACING = 36f;
        private const float MIN_MARKER_SCALE = 0.35f;

        private readonly IHardPointOverlayView _view;
        private readonly ISelectionQuery _selectionQuery;
        private readonly IPointerInput _pointerInput;
        private readonly ICameraService _cameraService;
        private readonly IUnitOrderService _unitOrderService;
        private readonly ICinematicCameraModelObserver _cinematicCamera;
        private readonly ILocalPlayer _localPlayer;

        private readonly HardPointOverlayModel _model;
        private readonly HardPointOverlayData _data;

        private readonly List<Vector2> _markerPositions = new List<Vector2>();
        private readonly List<float> _markerSpacings = new List<float>();

        public HardPointOverlayPresenter(
            IHardPointOverlayView view,
            ISelectionQuery selectionQuery,
            IPointerInput pointerInput,
            ICameraService cameraService,
            IUnitOrderService unitOrderService,
            ICinematicCameraModelObserver cinematicCamera,
            ILocalPlayer localPlayer,
            HardPointOverlayModel model,
            HardPointOverlayData data)
        {
            _view = view;
            _model = model;
            _data = data;
            _selectionQuery = selectionQuery;
            _pointerInput = pointerInput;
            _cameraService = cameraService;
            _unitOrderService = unitOrderService;
            _cinematicCamera = cinematicCamera;
            _localPlayer = localPlayer;
        }

        public void Initialize()
        {
            _unitOrderService.OrderIssued += HandleOrderIssued;
        }

        public void LateDispose()
        {
            _unitOrderService.OrderIssued -= HandleOrderIssued;
        }

        public void Tick()
        {
            if (_model.TargetedShip != null && !IsTargetStillValid())
            {
                _model.ClearTarget();
            }

            if (_cinematicCamera.IsActive)
            {
                _model.Inspect(null, UnitOrderModel.NO_HARD_POINT, false);
                _view.HideMarkersFrom(0);
                return;
            }

            UpdateInspection(_pointerInput.Position);
            Render();
        }

        private void UpdateInspection(Vector2 cursor)
        {
            IEntity ship = _model.InspectedShip;
            int hovered = UnitOrderModel.NO_HARD_POINT;

            // Markers can reach beyond the hull, so the inspected ship stays while the cursor is on one of them.
            if (ship != null && IsVisible(ship))
            {
                hovered = FindMarkerAt(ship, cursor);
            }

            if (hovered == UnitOrderModel.NO_HARD_POINT)
            {
                ship = GetShipUnderCursor(cursor);
                if (ship != null)
                {
                    hovered = FindMarkerAt(ship, cursor);
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
                float scale = GetMarkerScale(inspected);
                for (int id = 0; id < hardPoints.Count; id++)
                {
                    if (hardPoints[id].IsInstalled && TryShowMarker(slot, inspected, hardPoints[id], scale))
                    {
                        slot++;
                    }
                }
            }

            IEntity targeted = _model.TargetedShip;
            if (targeted != null && (inspected == null || targeted.Id != inspected.Id) && IsVisible(targeted) &&
                TryShowMarker(slot, targeted, GetHardPoints(targeted)[_model.TargetedHardPointId],
                    GetMarkerScale(targeted)))
            {
                slot++;
            }

            _view.HideMarkersFrom(slot);
        }

        private bool TryShowMarker(int slot, IEntity ship, IHardPointStatus hardPoint, float scale)
        {
            if (!TryGetScreenPosition(hardPoint, out Vector2 screenPosition))
            {
                return false;
            }

            bool isHovered = _model.InspectedShip != null && _model.InspectedShip.Id == ship.Id &&
                             _model.HoveredHardPointId == hardPoint.Id;
            _view.ShowMarker(slot, new HardPointMarkerData(
                screenPosition: screenPosition,
                icon: _data.Get(hardPoint.HardPointType).Icon,
                healthPercentage: hardPoint.HealthPercentage,
                isHovered: isHovered,
                isTargeted: _model.IsTargeted(ship, hardPoint.Id),
                isDestroyed: hardPoint.IsDestroyed,
                scale: scale));
            return true;
        }

        /// <summary>
        /// Shrinks one ship's markers together when its hardpoints crowd on screen (small hull, zoomed out or
        /// many hardpoints), using the median distance from each marker to its nearest neighbour.
        /// </summary>
        private float GetMarkerScale(IEntity ship)
        {
            _markerPositions.Clear();
            foreach (IHardPointStatus hardPoint in GetHardPoints(ship))
            {
                if (hardPoint.IsInstalled && TryGetScreenPosition(hardPoint, out Vector2 position))
                {
                    _markerPositions.Add(position);
                }
            }

            if (_markerPositions.Count < 2)
            {
                return 1f;
            }

            _markerSpacings.Clear();
            for (int i = 0; i < _markerPositions.Count; i++)
            {
                float nearest = float.MaxValue;
                for (int j = 0; j < _markerPositions.Count; j++)
                {
                    if (i != j)
                    {
                        nearest = Mathf.Min(nearest, (_markerPositions[i] - _markerPositions[j]).sqrMagnitude);
                    }
                }

                _markerSpacings.Add(nearest);
            }

            _markerSpacings.Sort();
            float spacing = Mathf.Sqrt(_markerSpacings[_markerSpacings.Count / 2]) / _view.ScaleFactor;
            return Mathf.Clamp(spacing / FULL_SIZE_SPACING, MIN_MARKER_SCALE, 1f);
        }

        private int FindMarkerAt(IEntity ship, Vector2 cursor)
        {
            IReadOnlyList<IHardPointStatus> hardPoints = GetHardPoints(ship);
            float hitHalfSize = HIT_HALF_SIZE * _view.ScaleFactor * GetMarkerScale(ship);

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
                if (!hardPoint.IsInstalled ||
                    !TryGetScreenPosition(hardPoint, out Vector2 position) ||
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
                   !ship.IsHiddenByFog();
        }

        private bool TryGetScreenPosition(IHardPointModel hardPoint, out Vector2 screenPosition)
        {
            Vector3 worldPosition = hardPoint.Position;
            screenPosition = _cameraService.WorldToScreenPoint(worldPosition);
            return _cameraService.WorldToViewportPoint(worldPosition).z > 0f;
        }

        private void HandleOrderIssued(UnitOrder order)
        {
            if (!_localPlayer.IsLocal(order.Issuer))
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
    }
}
