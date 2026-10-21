using System;
using System.Collections.Generic;
using EmpireAtWar.Components.Radar;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Mvc;
using VContainer;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Services.Stations;

namespace EmpireAtWar.Services.ShipNavigation
{
    public interface IMapObstacleContactSource
    {
        RadarContact Contact { get; }
    }

    public interface IMapObstacleContactProvider : IService
    {
        void CopyContacts(List<RadarContact> destination);
    }

    public sealed class MapObstacleContactProvider : Service,
        IMapObstacleContactProvider, IInitializable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly INotifier<BattleMap> _battleMap;
        private readonly IStationRegistry _stations;
        private readonly List<IMapObstacleContactSource> _sources = new List<IMapObstacleContactSource>();

        public MapObstacleContactProvider(INotifier<BattleMap> battleMap, IStationRegistry stations)
        {
            _battleMap = battleMap;
            _stations = stations;
        }

        public void Initialize()
        {
            _battleMap.AddObserver(this);
        }

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
        }

        public void UpdateState(BattleMap battleMap)
        {
            _sources.Clear();
            _sources.AddRange(battleMap.Obstacles);
            _sources.AddRange(battleMap.StationObstacles);
        }

        public void CopyContacts(List<RadarContact> destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            destination.Clear();
            for (int i = 0; i < _sources.Count; i++)
            {
                IMapObstacleContactSource source = _sources[i];
                if (source == null)
                {
                    throw new InvalidOperationException(
                        "A registered map obstacle contact source is missing.");
                }

                if (source is StationObstacle station &&
                    !_stations.TryGetLivingStation(station.Owner, out _)) continue;
                destination.Add(source.Contact);
            }
        }
    }
}
