using EmpireAtWar.Mvc;
using UnityEngine;

namespace EmpireAtWar.Entities.Planet
{
    [CreateAssetMenu(fileName = nameof(PlanetData), menuName = "Data/PlanetData")]
    public class PlanetData : Data, IPlanetData
    {
        [field: SerializeField] public float PlanetOrbitSpeed { get; private set; }
        [field: SerializeField] public float CloudOrbitSpeed { get; private set; }
    }
}