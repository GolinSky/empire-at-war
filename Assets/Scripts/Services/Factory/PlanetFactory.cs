using System;
using System.Collections.Generic;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Planet;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Services.VContainer;
using UnityEngine;
using Object = UnityEngine.Object;
using LifetimeScope = global::VContainer.Unity.LifetimeScope;

namespace EmpireAtWar.Services.Factory
{
    [LifecycleOrder(Dispose = LifecycleOrders.PLANETS_DISPOSE)]
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
            PlanetScope planet = null;
            try
            {
                planet = Object.Instantiate(prefab, inactiveParent.transform, false);
                planet.gameObject.SetActive(false);
                planet.transform.SetParent(_owner.transform, false);
                planet.autoRun = false;
                planet.parentReference.Object = _owner;
                planet.transform.SetPositionAndRotation(
                    position + Vector3.up * prefab.transform.position.y * scale, prefab.transform.rotation);
                planet.transform.localScale = prefab.transform.localScale * scale;
                planet.Build();
                planet.gameObject.SetActive(true);
                _planets.Add(planet);
                return planet.View;
            }
            catch (Exception createFailure)
            {
                if (planet == null) throw;
                var failures = new CleanupFailures();
                failures.Add(createFailure);
                failures.DisposeScope(planet);
                failures.ThrowIfAny();
                throw;
            }
            finally
            {
                if (Application.isPlaying) Object.Destroy(inactiveParent);
                else Object.DestroyImmediate(inactiveParent);
            }
        }

        public void Dispose()
        {
            var failures = new CleanupFailures();
            for (int index = _planets.Count - 1; index >= 0; index--)
                failures.DisposeScope(_planets[index]);
            _planets.Clear();
            failures.ThrowIfAny();
        }
    }
}
