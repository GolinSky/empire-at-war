using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Models.Players;
using UnityEngine;
using Random = System.Random;

namespace EmpireAtWar.Entities.Map
{
    /// <summary>Generates the layout on a worker thread, builds it frame-sliced and publishes the result.</summary>
    public sealed class BattleMapLoader : IBattleMapLoader, INotifier<BattleMap>
    {
        private readonly MapGenerationSettings _settings;
        private readonly MapLayoutView _mapLayoutView;
        private readonly IGameModelObserver _gameModel;
        private readonly MapModel _mapModel;
        private readonly BattleDefinition _battle;
        private readonly EmpireAtWar.Services.ShipSpawning.IShipSpawnClearance _spawnClearance;
        private readonly List<IObserver<BattleMap>> _observers = new List<IObserver<BattleMap>>();

        private BattleMap _battleMap;

        public BattleMapLoader(
            MapGenerationSettings settings,
            MapLayoutView mapLayoutView,
            IGameModelObserver gameModel,
            MapModel mapModel, BattleDefinition battle,
            EmpireAtWar.Services.ShipSpawning.IShipSpawnClearance spawnClearance)
        {
            _settings = settings;
            _mapLayoutView = mapLayoutView;
            _gameModel = gameModel;
            _mapModel = mapModel;
            _battle = battle;
            _spawnClearance = spawnClearance;
        }

        public async Awaitable LoadAsync(CancellationToken cancellationToken)
        {
            MapSize mapSize = _gameModel.MapSize;
            IReadOnlyList<PlayerSlot> players = _gameModel.Players;
            // Prefab radii are read through the Unity API, so they are taken on the main thread.
            MapLayoutGenerator generator = new MapLayoutGenerator(_settings, _mapLayoutView.FeatureRadii);
            SurvivalLayoutGenerator survivalGenerator = new SurvivalLayoutGenerator(_settings, _mapLayoutView.FeatureRadii);
            // DictionaryWrapper builds its dictionaries on first read; build them here so the worker only reads.
            _settings.GetSize(mapSize);
            _settings.GetStationRadius(players[0].Faction);
            _settings.GetRockLayer(AsteroidSize.Large);
            MapLayout simulationLayout = _battle.Map.Type == MapType.Empty
                ? EmptyLayoutGenerator.Generate(_battle.Simulation,
                    EmptyLayoutGenerator.GetSpacing(_battle.Simulation.FleetA, _spawnClearance),
                    EmptyLayoutGenerator.GetSpacing(_battle.Simulation.FleetB, _spawnClearance))
                : null;

            await Awaitable.BackgroundThreadAsync();
            MapLayout layout = _battle.Map.Type == MapType.Survival
                ? survivalGenerator.Generate(_battle)
                : _battle.Map.Type == MapType.Empty ? simulationLayout
                : generator.Generate(mapSize, players, new Random(_battle.Map.Seed));
            layout = new MapLayout(players.ToDictionary(player => player.Id, player => layout.GetStationPosition(player.Id)),
                layout.Zones, layout.Sites.Where(site => _battle.HasFeature(BattleFeatures.CaptureSites) &&
                    (site.FacilityType != EmpireAtWar.Entities.CaptureSites.SiteFacilityType.Mining ||
                     _battle.HasFeature(BattleFeatures.MiningSites))).ToArray(),
                layout.Lanes, layout.Fields, layout.SizeRange, layout.PlanetPosition);
            await Awaitable.MainThreadAsync();
            cancellationToken.ThrowIfCancellationRequested();

            await _mapLayoutView.BuildAsync(layout, cancellationToken);
            _mapModel.SetLayout(layout);
            Publish(new BattleMap(
                layout: layout,
                zoneViews: _mapLayoutView.ZoneViews,
                siteViews: _mapLayoutView.SiteViews,
                obstacles: _mapLayoutView.Obstacles,
                stationObstacles: CreateStationObstacles(layout, players)));
        }

        public void AddObserver(IObserver<BattleMap> observer)
        {
            if (_observers.Contains(observer))
            {
                return;
            }

            _observers.Add(observer);
            if (_battleMap != null)
            {
                observer.UpdateState(_battleMap);
            }
        }

        public void RemoveObserver(IObserver<BattleMap> observer)
        {
            _observers.Remove(observer);
        }

        private void Publish(BattleMap battleMap)
        {
            _battleMap = battleMap;
            foreach (IObserver<BattleMap> observer in _observers.ToArray())
            {
                observer.UpdateState(battleMap);
            }
        }

        private List<StationObstacle> CreateStationObstacles(MapLayout layout, IReadOnlyList<PlayerSlot> players)
        {
            List<StationObstacle> obstacles = new List<StationObstacle>(players.Count);
            foreach (PlayerSlot player in players)
            {
                if (!_battle.GetPlayer(player.Id).OwnsStation) continue;
                obstacles.Add(new StationObstacle(
                    layout.GetStationPosition(player.Id),
                    _settings.GetStationRadius(player.Faction)));
            }

            return obstacles;
        }
    }
}
