using EmpireAtWar.Models.SpaceStation;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.SpaceStation
{
    public class SpaceStationController:Controller<SpaceStationModel>, ISelectable
    {
        public bool CanMove => false;

        public SpaceStationController(SpaceStationModel model) : base(model)
        {
        }

        public void MoveToPosition(Vector2 screenPosition)
        {
            
        }

        public void SetActive(bool isActive)
        {
            //Model.IsSelected = isActive;
        }
    }
}