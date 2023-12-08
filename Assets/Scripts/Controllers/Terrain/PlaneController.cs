using EmpireAtWar.Models.Terrain;
using EmpireAtWar.Services.NavigationService;
using LightWeightFramework.Controller;
using UnityEngine;

namespace EmpireAtWar.Controllers.Terrain
{
    public class TerrainController:Controller<TerrainModel>, ISelectable
    {
        public bool CanMove => false;

        public TerrainController(TerrainModel model) : base(model)
        {
        }

        public void MoveToPosition(Vector2 screenPosition) //solid IS failed here
        {
            
        }

        public void SetActive(bool isActive)
        {
        }
    }
}