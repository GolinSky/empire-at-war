using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.VContainer;
using UnityEngine;
using LifetimeScope = global::VContainer.Unity.LifetimeScope;

namespace EmpireAtWar.Services.Factory
{
    [LifecycleOrder(Dispose = 200)]
    public sealed class PlanetFactory : IOrderedDisposable
    {
        private readonly LifetimeScope _owner;
        private readonly MapGenerationSettings _settings;
        private readonly List<PlanetScope> _planets = new List<PlanetScope>();

        public PlanetFactory(LifetimeScope owner, MapGenerationSettings settings)
        {
            _owner = owner;
            _settings = settings;
        }

        public PlanetView Create(PlanetType type, MapSize size, Vector3 position)
        {
            PlanetScope prefab = _settings.GetPlanetPrefab(type);
            float scale = _settings.GetSize(size).PlanetScale;
            GameObject inactiveParent = new GameObject("PlanetCreation");
            inactiveParent.SetActive(false);
            PlanetScope planet;
            try
            {
                planet = Object.Instantiate(prefab, inactiveParent.transform, false);
                planet.gameObject.SetActive(false);
                planet.transform.SetParent(_owner.transform, false);
            }
            finally
            {
                if (Application.isPlaying) Object.Destroy(inactiveParent);
                else Object.DestroyImmediate(inactiveParent);
            }

            planet.autoRun = false;
            planet.parentReference.Object = _owner;
            planet.transform.SetPositionAndRotation(
                position + Vector3.up * prefab.transform.position.y * scale, prefab.transform.rotation);
            planet.transform.localScale = prefab.transform.localScale * scale;
            _planets.Add(planet);
            planet.Build();
            planet.gameObject.SetActive(true);
            return planet.View;
        }

        public void Dispose()
        {
            for (int index = _planets.Count - 1; index >= 0; index--)
            {
                PlanetScope planet = _planets[index];
                try
                {
                    planet.DisposeCore();
                }
                finally
                {
                    if (Application.isPlaying) Object.Destroy(planet.gameObject);
                    else Object.DestroyImmediate(planet.gameObject);
                }
            }
            _planets.Clear();
        }
    }
}
