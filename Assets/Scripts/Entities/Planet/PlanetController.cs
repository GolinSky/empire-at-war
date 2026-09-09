using EmpireAtWar.Mvc;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Planet
{
    public class PlanetController:Controller<PlanetData>, ITickable
    {
        private Vector3 _cloudRotation;
        private Vector3 _planetRotation;
        
        public PlanetController(PlanetData model) : base(model)
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