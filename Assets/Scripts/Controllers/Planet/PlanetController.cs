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
            Model.CloudRotation = Vector3.forward *  Model.CloudOrbitSpeed * Time.deltaTime;;
            Model.PlanetRotation = Vector3.forward * Model.PlanetOrbitSpeed * Time.deltaTime;
        }
    }
}