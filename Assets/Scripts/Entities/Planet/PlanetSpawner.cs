using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Entities.Planet
{
    /// <summary>Spawns the planet chosen in the skirmish setup at its generated map position.</summary>
    public sealed class PlanetSpawner : IInitializable
    {
        private readonly IGameModelObserver _gameModel;

        private readonly DiContainer _container;
        private readonly MapGenerationSettings _settings;
        private readonly MapLayout _layout;

        public PlanetSpawner(
            IGameModelObserver gameModel,
            DiContainer container,
            MapGenerationSettings settings,
            MapLayout layout)
        {
            _container = container;
            _settings = settings;
            _layout = layout;
            _gameModel = gameModel;
        }

        public void Initialize()
        {
            GameObject prefab = _settings.GetPlanetPrefab(_gameModel.PlanetType);
            float scale = _settings.GetSize(_gameModel.MapSize).PlanetScale;
            // A scaled-up planet sinks by the same factor so it keeps its authored view from above.
            Vector3 position = _layout.PlanetPosition + Vector3.up * prefab.transform.position.y * scale;
            // The planet's GameObjectContext needs container instantiation to run its installers.
            GameObject planet = _container.InstantiatePrefab(prefab, position, prefab.transform.rotation, null);
            planet.transform.localScale = prefab.transform.localScale * scale;
        }
    }
}
