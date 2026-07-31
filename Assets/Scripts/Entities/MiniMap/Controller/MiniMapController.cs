using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Services.UiRouting;
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
    }

    public class MiniMapController : Controller<MiniMapModel>, IMiniMapCommand,
        IInitializable, ILateDisposable, IObserver<ISelectionSubject>,
        ISkirmishUiRoute
    {
        private readonly ICameraService _cameraService;
        private readonly IInputService _inputService;
        private readonly ITimerPoolWrapperService _timerPoolWrapperService;
        private readonly IUiService _uiService;
        private readonly ISelectionService _selectionService;
        private readonly ISkirmishRouteNavigation _routeNavigation;
        private MiniMapUi _miniMapUi;
        private CustomCoroutine _unblockCoroutine;
        
        public MiniMapController(
            MiniMapModel model,
            IMapModelObserver mapModel,
            ICameraService cameraService,
            IInputService inputService,
            ITimerPoolWrapperService timerPoolWrapperService,
            IUiService uiService,
            ISelectionService selectionService,
            ISkirmishRouteNavigation routeNavigation,
            [Inject(Id = PlayerType.Player)] FactionType playerFactionType,
            [Inject(Id = PlayerType.Opponent)] FactionType opponentFactionType) : base(model)
        {
            _cameraService = cameraService;
            _inputService = inputService;
            _timerPoolWrapperService = timerPoolWrapperService;
            _uiService = uiService;
            _selectionService = selectionService;
            _routeNavigation = routeNavigation;
            Model.MapRange = mapModel.SizeRange;            
            Model.AddMark(MarkType.PlayerBase, mapModel.GetStationPosition(playerFactionType));
            Model.AddMark(MarkType.EnemyBase, mapModel.GetStationPosition(opponentFactionType));
        }

    
        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _inputService.OnBlocked += UpdateBlockState;
            Model.AddMark(MarkType.Camera, _cameraService.CameraTransform);
            _routeNavigation.RegisterRoute(
                SkirmishUiRoutePosition.MiniMap,
                this);
        }
        
        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
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
        
        private void UpdateBlockState(bool isBlocked)
        {
            if (_unblockCoroutine != null)
            {
                _unblockCoroutine.Release();
            }
            if (!isBlocked)
            {
                _unblockCoroutine = _timerPoolWrapperService.Invoke(() => { Model.IsInputBlocked = isBlocked; }, 1f);
            }
            else
            {
                Model.IsInputBlocked = isBlocked;
            }
        }
        
        public void UpdateState(ISelectionSubject subject)
        {
            if (subject.UpdatedType == PlayerType.Player)
            {
                Model.IsInteractive = subject.PlayerSelectionContext.SelectionType != SelectionType.Base;
            }
        }
    }
}
