using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.SpaceStation
{
    public class SpaceStationController : Controller<SpaceStationModel>, IMovable
    {
        public SpaceStationController(SpaceStationModel model, Vector3 startPosition) : base(model)
        {
            Model.StartPosition = startPosition;
        }

        public bool CanMove => false;
        public void MoveToPosition(Vector2 screenPosition)
        {
            
        }
    }
}