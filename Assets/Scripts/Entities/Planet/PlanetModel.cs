using LightWeightFramework.Model;
using UnityEngine;

namespace EmpireAtWar.Entities.Planet
{
    public interface IPlanetModelObserver : IModelObserver
    {
        Vector3 PlanetRotation { get; }
        Vector3 CloudRotation { get; }
    }

    [CreateAssetMenu(fileName = "PlanetModel", menuName = "Model/PlanetModel")]
    public class PlanetModel : Model, IPlanetModelObserver
    {
        [field: SerializeField] public float PlanetOrbitSpeed { get; private set; }
        [field: SerializeField] public float CloudOrbitSpeed { get; private set; }

        public Vector3 PlanetRotation { get; set; }
        public Vector3 CloudRotation { get; set; }
    }
}