using System.Collections.Generic;
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
        private readonly List<IObserver<BattleMap>> _observers = new List<IObserver<BattleMap>>();

        private BattleMap _battleMap;

        public BattleMapLoader(
            MapGenerationSettings settings,
            MapLayoutView mapLayoutView,
            IGameModelObserver gameModel,
            MapModel mapModel)
        {
            _settings = settings;
            _mapLayoutView = mapLayoutView;
            _gameModel = gameModel;
            _mapModel = mapModel;
        }

        public async Awaitable LoadAsync(CancellationToken cancellationToken)
        {
            MapSize mapSize = _gameModel.MapSize;
            IReadOnlyList<PlayerSlot> players = _gameModel.Players;
            // Prefab radii are read through the Unity API, so they are taken on the main thread.
            MapLayoutGenerator generator = new MapLayoutGenerator(_settings, _mapLayoutView.FeatureRadii);
            // DictionaryWrapper builds its dictionaries on first read; build them here so the worker only reads.
            _settings.GetSize(mapSize);
            _settings.GetStationRadius(players[0].Faction);
            _settings.GetRockLayer(AsteroidSize.Large);

            await Awaitable.BackgroundThreadAsync();
            MapLayout layout = generator.Generate(mapSize, players, new Random());
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
                obstacles.Add(new StationObstacle(
                    layout.GetStationPosition(player.Id),
                    _settings.GetStationRadius(player.Faction)));
            }

            return obstacles;
        }
    }
}
