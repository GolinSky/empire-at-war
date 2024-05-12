using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.NavigationService;
using EmpireAtWar.Services.TimerPoolWrapperService;
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

    public class MiniMapController : Controller<MiniMapModel>, IMiniMapCommand, IInitializable, ILateDisposable
    {
        private readonly ICameraService cameraService;
        private readonly INavigationService navigationService;
        private readonly IInputService inputService;
        private readonly ITimerPoolWrapperService timerPoolWrapperService;
        private CustomCoroutine unblockCoroutine;
        
        public MiniMapController(
            MiniMapModel model,
            IMapModelObserver mapModel,
            ICameraService cameraService,
            INavigationService navigationService,
            IInputService inputService,
            ITimerPoolWrapperService timerPoolWrapperService) : base(model)
        {
            this.cameraService = cameraService;
            this.navigationService = navigationService;
            this.inputService = inputService;
            this.timerPoolWrapperService = timerPoolWrapperService;
            Model.MapRange = mapModel.SizeRange;            
            Model.AddMark(MarkType.PlayerBase, mapModel.GetStationPosition(PlayerType.Player));
            Model.AddMark(MarkType.EnemyBase, mapModel.GetStationPosition(PlayerType.Opponent));
        }

    
        public void Initialize()
        {
            navigationService.OnTypeChanged += UpdateSelectionType;
            inputService.OnBlocked += UpdateBlockState;
            Model.AddMark(MarkType.Camera, cameraService.CameraTransform);
        }
        
        public void LateDispose()
        {
            navigationService.OnTypeChanged -= UpdateSelectionType;
            inputService.OnBlocked -= UpdateBlockState;
        }
        
        public void MoveTo(Vector3 worldPoint)
        {
            cameraService.MoveTo(worldPoint);
        }

        
        private void UpdateBlockState(bool isBlocked)
        {
            if (unblockCoroutine != null)
            {
                unblockCoroutine.Release();
            }
            if (!isBlocked)
            {
                unblockCoroutine = timerPoolWrapperService.Invoke(() => { Model.IsInputBlocked = isBlocked; }, 1f);
            }
            else
            {
                Model.IsInputBlocked = isBlocked;
            }
        }
        
        private void UpdateSelectionType(SelectionType selectionType)
        {
            Model.IsInteractive = selectionType != SelectionType.Base;
        }
    }
}