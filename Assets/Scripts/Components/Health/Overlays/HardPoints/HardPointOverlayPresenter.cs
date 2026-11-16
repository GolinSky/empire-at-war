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
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Components.Ship.Health.HardPointOverlay
{
    /// <summary>
    /// Reveals the hardpoints of the ship under the cursor, resolves which marker is hovered
    /// and keeps the player's explicitly targeted hardpoint highlighted.
    /// </summary>
    public sealed class HardPointOverlayPresenter : IInitializable, ILateDisposable, ITickable, IHardPointOverlayLayout
    {
        // Includes the 55-unit targeted ring and a gap; independent of hover/target state.
        private const float MARKER_SPACING = 60f;
        /// <summary>Reference pixels per unit of marker scale when hardpoints are crowded.</summary>
        private const float FULL_SIZE_SPACING = 36f;
        private const float REFERENCE_SHIP_RADIUS = 40f;
        private const float REFERENCE_CAMERA_DEPTH = 600f;
        private const float MIN_MARKER_SCALE = 0.6f;
        private const float MAX_MARKER_SCALE = 1.4f;

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
        private readonly List<int> _markerIds = new List<int>();
        private Rect _markerBounds;

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
            bool keepInspection = false;

            // Keep the expanded layout reachable outside the hull, including gaps between its markers.
            if (ship != null && IsVisible(ship))
            {
                hovered = FindMarkerAt(ship, cursor);
                keepInspection = hovered != UnitOrderModel.NO_HARD_POINT ||
                                 _markerPositions.Count > 0 && _markerBounds.Contains(cursor);
            }

            if (hovered == UnitOrderModel.NO_HARD_POINT)
            {
                IEntity underCursor = GetShipUnderCursor(cursor);
                if (underCursor != null || !keepInspection)
                {
                    ship = underCursor;
                    if (ship != null) hovered = FindMarkerAt(ship, cursor);
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
                float scale = PrepareMarkerLayout(inspected);
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
                    PrepareMarkerLayout(targeted)))
            {
                slot++;
            }

            _view.HideMarkersFrom(slot);
        }

        public bool TryGetMarkerTop(IEntity ship, out float screenTop)
        {
            screenTop = float.MinValue;
            if (_cinematicCamera.IsActive || !ship.TryGetFacade(out IHardPointsFacade _) || !IsVisible(ship))
            {
                return false;
            }

            float halfSize = MARKER_SPACING * 0.5f * _view.ScaleFactor * PrepareMarkerLayout(ship);
            bool hasMarkers = false;
            foreach (IHardPointStatus hardPoint in GetHardPoints(ship))
            {
                if (hardPoint.IsInstalled && !hardPoint.IsDestroyed &&
                    TryGetMarkerPosition(hardPoint.Id, out Vector2 position))
                {
                    float markerTop = position.y + halfSize;
                    screenTop = Mathf.Max(screenTop, markerTop);
                    hasMarkers = true;
                }
            }

            return hasMarkers;
        }

        private bool TryShowMarker(int slot, IEntity ship, IHardPointStatus hardPoint, float scale)
        {
            if (hardPoint.IsDestroyed || !TryGetMarkerPosition(hardPoint.Id, out Vector2 screenPosition))
            {
                return false;
            }

            _view.ShowMarker(slot, new HardPointMarkerData(
                screenPosition: screenPosition,
                icon: _data.Get(hardPoint.HardPointType).Icon,
                healthPercentage: hardPoint.HealthPercentage,
                isHovered: GetHoveredMarker(ship) == hardPoint.Id,
                isTargeted: _model.IsTargeted(ship, hardPoint.Id),
                isDestroyed: hardPoint.IsDestroyed,
                scale: scale));
            return true;
        }

        /// <summary>
        /// Sizes markers by apparent hull size, then separates their shared render and hit positions.
        /// </summary>
        private float PrepareMarkerLayout(IEntity ship)
        {
            Vector3 position = ship.GetFacade<IEntityTransformFacade>().Transform.position;
            float depth = Mathf.Max(1f, _cameraService.WorldToViewportPoint(position).z);
            float radius = ship.TryGetFacade(out INavigationFootprintFacade footprint)
                ? footprint.NavigationRadius
                : REFERENCE_SHIP_RADIUS;
            float apparentSize = radius / REFERENCE_SHIP_RADIUS * (REFERENCE_CAMERA_DEPTH / depth);
            float scale = Mathf.Lerp(MIN_MARKER_SCALE, MAX_MARKER_SCALE, apparentSize / (1f + apparentSize));

            _markerPositions.Clear();
            _markerIds.Clear();
            foreach (IHardPointStatus hardPoint in GetHardPoints(ship))
            {
                if (hardPoint.IsInstalled && !hardPoint.IsDestroyed &&
                    TryGetScreenPosition(hardPoint, out Vector2 markerPosition))
                {
                    _markerPositions.Add(markerPosition);
                    _markerIds.Add(hardPoint.Id);
                }
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

            if (_markerPositions.Count > 1)
            {
                _markerSpacings.Sort();
                float spacing = Mathf.Sqrt(_markerSpacings[_markerSpacings.Count / 2]) / _view.ScaleFactor;
                scale = Mathf.Max(MIN_MARKER_SCALE, Mathf.Min(scale, spacing / FULL_SIZE_SPACING));
            }

            float separation = MARKER_SPACING * _view.ScaleFactor * scale;
            HardPointMarkerLayout.Separate(_markerPositions, separation);
            if (_markerPositions.Count > 0)
            {
                Vector2 min = _markerPositions[0];
                Vector2 max = min;
                foreach (Vector2 marker in _markerPositions)
                {
                    min = Vector2.Min(min, marker);
                    max = Vector2.Max(max, marker);
                }
                Vector2 padding = Vector2.one * separation * 0.5f;
                _markerBounds = Rect.MinMaxRect(min.x - padding.x, min.y - padding.y,
                    max.x + padding.x, max.y + padding.y);
            }
            return scale;
        }

        private int FindMarkerAt(IEntity ship, Vector2 cursor)
        {
            float halfSize = MARKER_SPACING * 0.5f * _view.ScaleFactor * PrepareMarkerLayout(ship);
            int index = HardPointMarkerLayout.FindClosest(_markerPositions, cursor, halfSize);
            return index < 0 ? UnitOrderModel.NO_HARD_POINT : _markerIds[index];
        }

        private bool TryGetMarkerPosition(int id, out Vector2 position)
        {
            int index = _markerIds.IndexOf(id);
            position = index < 0 ? default : _markerPositions[index];
            return index >= 0;
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
            return !ship.HealthModel.IsDestroyed &&
                   (!ship.TryGetFacade(out IConstructionFacade construction) || !construction.IsConstructing) &&
                   !GetHardPoints(ship)[_model.TargetedHardPointId].IsDestroyed;
        }

        private bool IsVisible(IEntity ship)
        {
            return !ship.HealthModel.IsDestroyed &&
                   !ship.IsHiddenByFog() &&
                   _cameraService.CameraPosition.y <= _data.MaxCameraHeight &&
                   (!ship.TryGetFacade(out IConstructionFacade construction) || !construction.IsConstructing);
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

        private int GetHoveredMarker(IEntity ship) =>
            _model.InspectedShip != null && _model.InspectedShip.Id == ship.Id
                ? _model.HoveredHardPointId
                : UnitOrderModel.NO_HARD_POINT;
    }
}
