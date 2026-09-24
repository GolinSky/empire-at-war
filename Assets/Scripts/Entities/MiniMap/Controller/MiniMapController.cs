using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
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
    public interface IMiniMapCommand : ICommand
    {
        void MoveTo(Vector3 worldPoint);
        bool TryOrderMove(Vector3 worldPoint);
    }

    public class MiniMapController : Controller<MiniMapData>, IMiniMapCommand,
        IInitializable, ILateTickable, ILateDisposable,
        ISkirmishUiRoute
    {
        private readonly ICameraService _cameraService;
        private readonly IInputService _inputService;
        private readonly TimerPoolService _timerPoolService;
        private readonly IUiService _uiService;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private readonly IPlayerOrderInputHandler _orderInput;
        private MiniMapUi _miniMapUi;
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
            [Inject(Id = PlayerType.Player)] FactionType playerFactionType,
            [Inject(Id = PlayerType.Opponent)] FactionType opponentFactionType) : base(model)
        {
            _cameraService = cameraService;
            _inputService = inputService;
            _timerPoolService = timerPoolService;
            _uiService = uiService;
            _routeNavigation = routeNavigation;
            _orderInput = orderInput;
            Model.MapRange = mapModel.SizeRange;            
            Model.AddMark(MarkType.PlayerBase, mapModel.GetStationPosition(playerFactionType));
            Model.AddMark(MarkType.EnemyBase, mapModel.GetStationPosition(opponentFactionType));
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
            _routeNavigation.UnregisterRoute(
                SkirmishUiRoutePosition.MiniMap,
                this);
        }

        public void Activate(bool isActive, Transform parentTransform)
        {
            if (_miniMapUi == null)
            {
                BaseUi ui = _uiService.CreateUi(UiType.MiniMap, parentTransform);
                _miniMapUi = ui as MiniMapUi
                    ?? throw new System.InvalidOperationException(
                        "The minimap prefab does not contain MiniMapUi.");
            }
            else
            {
                _miniMapUi.SetParent(parentTransform);
            }

            if (isActive)
            {
                _miniMapUi.Show();
            }
            else
            {
                _miniMapUi.Hide();
            }
        }
        
        public void MoveTo(Vector3 worldPoint)
        {
            _cameraService.MoveTo(worldPoint);
        }

        public bool TryOrderMove(Vector3 worldPoint)
        {
            return _orderInput.TryIssueMove(worldPoint);
        }
        
        private void UpdateBlockState(bool isBlocked)
        {
            if (_unblockCoroutine != null)
            {
                _unblockCoroutine.Release();
            }
            if (!isBlocked)
            {
                _unblockCoroutine = _timerPoolService.Invoke(() => { Model.IsInputBlocked = isBlocked; }, 1f);
            }
            else
            {
                Model.IsInputBlocked = isBlocked;
            }
        }
    }
}
