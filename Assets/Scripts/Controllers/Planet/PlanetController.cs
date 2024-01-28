using EmpireAtWar.Models.Planet;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Planet
{
    public class PlanetController:Controller<PlanetModel>, ITickable
    {
        private Vector3 cloudRotation;
        private Vector3 planetRotation;
        
        public PlanetController(PlanetModel model) : base(model)
        {
        }

        public void Tick()
        {
            cloudRotation.z += Model.CloudOrbitSpeed * Time.deltaTime;
            Model.CloudRotation = cloudRotation;

            planetRotation.z += Model.PlanetOrbitSpeed * Time.deltaTime;
            Model.PlanetRotation = planetRotation;
        }
    }
}