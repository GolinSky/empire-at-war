using EmpireAtWar.Models.SpaceStation;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.SpaceStation
{
    public class SpaceStationController : Controller<SpaceStationModel>
    {
        public SpaceStationController(SpaceStationModel model, Vector3 startPosition) : base(model)
        {
            Model.StartPosition = startPosition;
        }
    }
}