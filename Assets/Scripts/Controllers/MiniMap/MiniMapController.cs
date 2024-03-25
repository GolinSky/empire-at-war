using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Map;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Camera;
using LightWeightFramework.Command;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.MiniMap
{
    public interface IMiniMapCommand : ICommand
    {
        void MoveTo(Vector3 worldPoint);
    }

    public class MiniMapController : Controller<MiniMapModel>, IMiniMapCommand
    {
        private readonly ICameraService cameraService;

        public MiniMapController(MiniMapModel model, IMapModelObserver mapModel, ICameraService cameraService) : base(model)
        {
            this.cameraService = cameraService;
            Model.MapRange = mapModel.SizeRange;            
            Model.AddMark(MarkType.PlayerBase, mapModel.GetStationPosition(PlayerType.Player));
            Model.AddMark(MarkType.EnemyBase, mapModel.GetStationPosition(PlayerType.Opponent));
        }

        public void MoveTo(Vector3 worldPoint)
        {
            // worldPoint.y = Camera.main.transform.position.y;
            // Camera.main.transform.position = worldPoint; 
            cameraService.MoveTo(worldPoint);
        }
    }
}