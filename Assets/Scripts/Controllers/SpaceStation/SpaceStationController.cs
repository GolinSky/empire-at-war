using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.SpaceStation
{
    public class SpaceStationController:Controller<SpaceStationModel>
    {

        public SpaceStationController(SpaceStationModel model) : base(model)
        {
        }
        
    }
}