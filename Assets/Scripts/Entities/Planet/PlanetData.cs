using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Planet
{
    public interface IPlanetModelObserver : IModelObserver
    {
        float PlanetOrbitSpeed { get; }
        float CloudOrbitSpeed { get; }
    }

    [CreateAssetMenu(fileName = nameof(PlanetData), menuName = "Data/PlanetData")]
    public class PlanetData : Data, IModel, IPlanetModelObserver
    {
        [field: SerializeField] public float PlanetOrbitSpeed { get; private set; }
        [field: SerializeField] public float CloudOrbitSpeed { get; private set; }
    }
}