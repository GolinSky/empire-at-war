using System.Collections.Generic;
using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using UnityEngine;
using VContainer;
using EmpireAtWar.Lifecycle;

namespace EmpireAtWar.Services.SpawnBlocking
{
    /// <summary>Asteroid obstacles are neutral blockers: no team may spawn on or next to them.</summary>
    public sealed class AsteroidSpawnBlockers : IInitializable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly List<Transform> _registered = new List<Transform>();
        private readonly IBattleAreaWriter _areas;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly ReinforcementZoneData _data;

        public AsteroidSpawnBlockers(IBattleAreaWriter areas, INotifier<BattleMap> battleMap,
            ReinforcementZoneData data)
        {
            _areas = areas;
            _battleMap = battleMap;
            _data = data;
        }

        public void Initialize() => _battleMap.AddObserver(this);

        public void LateDispose()
        {
            _battleMap.RemoveObserver(this);
            UnregisterMap();
        }

        public void UpdateState(BattleMap battleMap)
        {
            UnregisterMap();
            foreach (MapObstacle obstacle in battleMap.Obstacles)
            {
                Vector3 extents = obstacle.WorldBounds.extents;
                float footprint = new Vector2(extents.x, extents.z).magnitude;
                Transform source = obstacle.transform;
                _registered.Add(source);
                _areas.Register(BattleAreaLayer.SpawnBlock, PlayerId.None, source,
                    footprint + _data.AsteroidSpawnBlockMargin);
            }
        }

        private void UnregisterMap()
        {
            foreach (Transform source in _registered)
                _areas.Unregister(BattleAreaLayer.SpawnBlock, source);
            _registered.Clear();
        }
    }
}
