using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using UnityEngine;
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

        public MiniMapController(MiniMapModel model, IMapModelObserver mapModel, ICameraService cameraService, INavigationService navigationService) : base(model)
        {
            this.cameraService = cameraService;
            this.navigationService = navigationService;
            Model.MapRange = mapModel.SizeRange;            
            Model.AddMark(MarkType.PlayerBase, mapModel.GetStationPosition(PlayerType.Player));
            Model.AddMark(MarkType.EnemyBase, mapModel.GetStationPosition(PlayerType.Opponent));
        }

        public void MoveTo(Vector3 worldPoint)
        {
            cameraService.MoveTo(worldPoint);
        }

        public void Initialize()
        {
            navigationService.OnTypeChanged += UpdateSelectionType;
        }

        public void LateDispose()
        {
            navigationService.OnTypeChanged -= UpdateSelectionType;
        }
        
        private void UpdateSelectionType(SelectionType selectionType)
        {
            Model.IsInteractive = selectionType != SelectionType.Base;
        }
    }
}