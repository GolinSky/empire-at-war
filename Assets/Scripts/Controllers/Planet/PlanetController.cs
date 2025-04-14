using EmpireAtWar.Models.Planet;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Controllers.Planet
{
    public class PlanetController:Controller<PlanetModel>, ITickable
    {
        private Vector3 _cloudRotation;
        private Vector3 _planetRotation;
        
        public PlanetController(PlanetModel model) : base(model)
        {
        }

        public void Tick()
        {
            //todo: try to use ref for struct as in ecs
            Model.CloudRotation = Vector3.forward *  Model.CloudOrbitSpeed * Time.deltaTime;
            Model.PlanetRotation = Vector3.forward * Model.PlanetOrbitSpeed * Time.deltaTime;
        }
    }
}