using EmpireAtWar.Components.Ui.Tooltip;
using EmpireAtWar.Entities.Tooltip;
using EmpireAtWar.Services.Tooltip;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.Selection;
using EmpireAtWar.Services.UiRouting;
using EmpireAtWar.Services.UnitOrders;
using EmpireAtWar.Ui.Base;
using EmpireAtWar.Mvc;
using EmpireAtWar.Views.MiniMap;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Controllers.MiniMap
{
    public class MiniMapController : UiController,
        IInitializable, ILateTickable, ILateDisposable,
        ISkirmishUiRoute, IObserver<BattleMap>
    {
        private readonly ICameraService _cameraService;
        private readonly IInputLock _inputLock;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly IPlayerOrderInputHandler _orderInput;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly IPlayerRoster _playerRoster;
        private readonly ILocalPlayer _localPlayer;
        private IMiniMapView _miniMapView;

        private readonly MiniMapData _model;
        private readonly TimerPoolService _timerPoolService;
        private CustomCoroutine _unblockCoroutine;
        private readonly TooltipRequests _tooltips;
        private TooltipHoverSubscription _tooltipHover;

        private bool _hasMap;

        public MiniMapController(
            INotifier<BattleMap> battleMap,
            ICameraService cameraService,
            IInputLock inputLock,
            IUiService uiService,
            IUiCancelRouter cancelRouter,
            ISkirmishRouteNavigation routeNavigation,
            IPlayerOrderInputHandler orderInput,
            IPlayerRoster playerRoster,
            ILocalPlayer localPlayer,
            ITooltipService tooltipService,
            MiniMapData model,
            TimerPoolService timerPoolService) : base(uiService, cancelRouter)
        {
            _battleMap = battleMap;
            _playerRoster = playerRoster;
            _localPlayer = localPlayer;
            _cameraService = cameraService;
            _tooltips = new TooltipRequests(tooltipService);
            _model = model;
            _inputLock = inputLock;
            _timerPoolService = timerPoolService;
            _routeNavigation = routeNavigation;
            _orderInput = orderInput;
        }

        public void Initialize()
        {
            _inputLock.LockChanged += UpdateBlockState;
            _battleMap.AddObserver(this);
        }

        public void LateDispose()
        {
            _inputLock.LockChanged -= UpdateBlockState;
            _battleMap.RemoveObserver(this);
            if (_miniMapView != null)
            {
                _tooltipHover.Dispose();
                _miniMapView.OnCameraMoveRequested -= MoveTo;
                _miniMapView.OnMoveOrderRequested -= OrderMove;
            }
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.MiniMap,
                this);
        }

        public void UpdateState(BattleMap battleMap)
        {
            MapLayout layout = battleMap.Layout;
            _model.MapRange = layout.SizeRange;
            _model.ClearBases();
            foreach (PlayerSlot player in _playerRoster.Players)
            {
                _model.AddBase(layout.GetStationPosition(player.Id), player.Id, _localPlayer.IsHostile(player.Id));
            }
            // Obstacles are spawned and scaled this frame; auto sync is off, so collider bounds are stale until synced.
            Physics.SyncTransforms();
            foreach (IMiniMapObstacleSource obstacleSource in battleMap.Obstacles)
            {
                Bounds bounds = obstacleSource.WorldBounds;
                _model.AddObstacle(new MiniMapObstacle(
                    bounds.center.x,
                    bounds.center.z,
                    bounds.extents.x,
                    bounds.extents.z));
            }

            _hasMap = true;
            LateTick();
            // The view reads the map once when created, so the route opens only now.
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.MiniMap,
                this);
        }

        public void LateTick()
        {
            // The camera mark is clipped to the map, which exists once the battle map loads.
            if (!_hasMap)
            {
                return;
            }

            _model.CameraMark.Clear();
            var footprint = _cameraService.GetGroundFootprint(_model.MapRange.Min, _model.MapRange.Max);
            for (int i = 0; i < footprint.Count; i++)
            {
                Vector3 point = footprint[i];
                _model.CameraMark.AddVertex(point.x, point.z);
            }
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_miniMapView == null)
            {
                BaseUi ui = UiService.CreateUi(UiType.MiniMap, parentTransform);
                _miniMapView = ui as IMiniMapView
                    ?? throw new System.InvalidOperationException(
                        "The minimap prefab does not contain IMiniMapView.");
                _miniMapView.OnCameraMoveRequested += MoveTo;
                _miniMapView.OnMoveOrderRequested += OrderMove;
                _tooltipHover = new TooltipHoverSubscription(
                    ((ITooltipHoverView)_miniMapView).TooltipHover, HandleTooltipHover, _tooltips);
            }
            else
            {
                _miniMapView.SetParent(parentTransform);
            }

            if (isActive)
            {
                _miniMapView.Show();
            }
            else
            {
                _miniMapView.Hide();
                _tooltips.HideAll();
            }
        }

        private void HandleTooltipHover(object key, TooltipAnchor anchor, object source)
        {
            if (key is MiniMapMarker marker)
            {
                _tooltips.Show(source, key, anchor, () => !_model.IsInputBlocked && marker.Visible,
                    () => new TooltipContent(title: marker.MarkType.ToString(),
                        description: marker.MarkType == MarkType.ReinforcementZone
                            ? "Deploy reinforcements inside a friendly zone."
                            : marker.MarkType == MarkType.CaptureSite
                                ? "Move units into the ring to capture this construction site."
                                : "Click to move the camera here.",
                        status: $"Owner: {marker.Owner}"));
                return;
            }
            _tooltips.Show(source, key, anchor, () => !_model.IsInputBlocked,
                () => new TooltipContent(title: key.ToString() == "Station" ? "Station" : "Minimap",
                    description: "Click or drag to move the camera. Right-click to issue a move order. Obstacles block move targets."));
        }

        private void MoveTo(Vector3 worldPoint)
        {
            _cameraService.MoveTo(worldPoint);
        }

        private void OrderMove(Vector3 worldPoint)
        {
            // Match world input: taps on an obstacle are not move targets.
            if (_model.IsObstacleAt(worldPoint)) return;
            if (_orderInput.TryIssueMove(worldPoint))
            {
                _miniMapView.PlayMoveTarget(worldPoint);
            }
        }

        private void UpdateBlockState(bool isBlocked)
        {
            if (_unblockCoroutine != null)
            {
                _unblockCoroutine.Release();
                _unblockCoroutine = null;
            }
            if (!isBlocked)
            {
                // A finished timer returns to the shared pool; drop the handle so it is never released twice.
                _unblockCoroutine = _timerPoolService.Invoke(() =>
                {
                    _unblockCoroutine = null;
                    _model.IsInputBlocked = isBlocked;
                }, 1f);
            }
            else
            {
                _model.IsInputBlocked = isBlocked;
            }
        }
    }
}
