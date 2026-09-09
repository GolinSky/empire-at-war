using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Planet
{
    public interface IPlanetModelObserver : IModelObserver
    {
        Vector3 PlanetRotation { get; }
        Vector3 CloudRotation { get; }
    }

    [CreateAssetMenu(fileName = nameof(PlanetData), menuName = "Data/PlanetData")]
    public class PlanetData : Data, IModel, IPlanetModelObserver
    {
        [field: SerializeField] public float PlanetOrbitSpeed { get; private set; }
        [field: SerializeField] public float CloudOrbitSpeed { get; private set; }

        public Vector3 PlanetRotation { get; set; }
        public Vector3 CloudRotation { get; set; }
    }
}