using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
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
    public class MiniMapController : Controller<MiniMapData>,
        IInitializable, ILateTickable, ILateDisposable,
        ISkirmishUiRoute
    {
        private readonly ICameraService _cameraService;
        private readonly IInputService _inputService;
        private readonly TimerPoolService _timerPoolService;
        private readonly IUiService _uiService;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly IPlayerOrderInputHandler _orderInput;
        private IMiniMapView _miniMapView;
        private CustomCoroutine _unblockCoroutine;
        
        public MiniMapController(
            MiniMapData model,
            IMapModelObserver mapModel,
            ICameraService cameraService,
            IInputService inputService,
            TimerPoolService timerPoolService,
            IUiService uiService,
            ISkirmishRouteNavigation routeNavigation,
            IPlayerOrderInputHandler orderInput,
            List<IMiniMapObstacleSource> obstacleSources,
            IPlayerRoster roster,
            ILocalPlayer localPlayer) : base(model)
        {
            _cameraService = cameraService;
            _inputService = inputService;
            _timerPoolService = timerPoolService;
            _uiService = uiService;
            _routeNavigation = routeNavigation;
            _orderInput = orderInput;
            Model.MapRange = mapModel.SizeRange;            
            Model.ClearBases();
            foreach (PlayerSlot player in roster.Players)
            {
                Model.AddBase(mapModel.GetStationPosition(player.Id), player.Id, localPlayer.IsHostile(player.Id));
            }
            // Obstacles are spawned and scaled this frame; auto sync is off, so collider bounds are stale until synced.
            Physics.SyncTransforms();
            foreach (IMiniMapObstacleSource obstacleSource in obstacleSources)
            {
                Bounds bounds = obstacleSource.WorldBounds;
                Model.AddObstacle(new MiniMapObstacle(
                    bounds.center.x,
                    bounds.center.z,
                    bounds.extents.x,
                    bounds.extents.z));
            }
        }

    
        public void Initialize()
        {
            _inputService.OnBlocked += UpdateBlockState;
            LateTick();
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.MiniMap,
                this);
        }
        
        public void LateTick()
        {
            Model.CameraMark.Clear();
            var footprint = _cameraService.GetGroundFootprint(Model.MapRange.Min, Model.MapRange.Max);
            for (int i = 0; i < footprint.Count; i++)
            {
                Vector3 point = footprint[i];
                Model.CameraMark.AddVertex(point.x, point.z);
            }
        }

        public void LateDispose()
        {
            _inputService.OnBlocked -= UpdateBlockState;
            if (_miniMapView != null)
            {
                _miniMapView.OnCameraMoveRequested -= MoveTo;
                _miniMapView.OnMoveOrderRequested -= OrderMove;
            }
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.MiniMap,
                this);
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_miniMapView == null)
            {
                BaseUi ui = _uiService.CreateUi(UiType.MiniMap, parentTransform);
                _miniMapView = ui as IMiniMapView
                    ?? throw new System.InvalidOperationException(
                        "The minimap prefab does not contain IMiniMapView.");
                _miniMapView.OnCameraMoveRequested += MoveTo;
                _miniMapView.OnMoveOrderRequested += OrderMove;
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
            }
        }
        
        private void MoveTo(Vector3 worldPoint)
        {
            _cameraService.MoveTo(worldPoint);
        }

        private void OrderMove(Vector3 worldPoint)
        {
            // Match world input: taps on an obstacle are not move targets.
            if (Model.IsObstacleAt(worldPoint)) return;
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
                    Model.IsInputBlocked = isBlocked;
                }, 1f);
            }
            else
            {
                Model.IsInputBlocked = isBlocked;
            }
        }
    }
}
