using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Battle;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.TimerPoolWrapperService;
using EmpireAtWar.Ui.Base;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using UnityEngine;
using Utilities.ScriptUtils.Time;
using Zenject;

namespace EmpireAtWar.Controllers.MiniMap
{
    public interface IMiniMapCommand : ICommand
    {
        void MoveTo(Vector3 worldPoint);
    }

    public class MiniMapController : Controller<MiniMapModel>, IMiniMapCommand, IInitializable, ILateDisposable, IObserver<ISelectionSubject>
    {
        private readonly ICameraService _cameraService;
        private readonly IInputService _inputService;
        private readonly ITimerPoolWrapperService _timerPoolWrapperService;
        private readonly IUiService _uiService;
        private readonly ISelectionService _selectionService;
        private CustomCoroutine _unblockCoroutine;
        
        public MiniMapController(
            MiniMapModel model,
            IMapModelObserver mapModel,
            ICameraService cameraService,
            IInputService inputService,
            ITimerPoolWrapperService timerPoolWrapperService,
            IUiService uiService,
            ISelectionService selectionService) : base(model)
        {
            _cameraService = cameraService;
            _inputService = inputService;
            _timerPoolWrapperService = timerPoolWrapperService;
            _uiService = uiService;
            _selectionService = selectionService;
            Model.MapRange = mapModel.SizeRange;            
            Model.AddMark(MarkType.PlayerBase, mapModel.GetStationPosition(PlayerType.Player));
            Model.AddMark(MarkType.EnemyBase, mapModel.GetStationPosition(PlayerType.Opponent));
        }

    
        public void Initialize()
        {
            _selectionService.AddObserver(this);
            _inputService.OnBlocked += UpdateBlockState;
            Model.AddMark(MarkType.Camera, _cameraService.CameraTransform);
            _uiService.CreateUi(UiType.MiniMap);

        }
        
        public void LateDispose()
        {
            _selectionService.RemoveObserver(this);
            _inputService.OnBlocked -= UpdateBlockState;
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