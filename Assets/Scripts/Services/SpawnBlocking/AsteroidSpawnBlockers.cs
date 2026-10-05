using EmpireAtWar.Components.Obstacles;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.ReinforcementZones;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.SpawnBlocking
{
    /// <summary>Asteroid obstacles are neutral blockers: no team may spawn on or next to them.</summary>
    public sealed class AsteroidSpawnBlockers : IInitializable, ILateDisposable, IObserver<BattleMap>
    {
        private readonly ISpawnBlockerService _spawnBlockerService;
        private readonly INotifier<BattleMap> _battleMap;
        private readonly ReinforcementZoneData _data;

        public AsteroidSpawnBlockers(ISpawnBlockerService spawnBlockerService, INotifier<BattleMap> battleMap,
            ReinforcementZoneData data)
        {
            _spawnBlockerService = spawnBlockerService;
            _battleMap = battleMap;
            _data = data;
        }

        public void Initialize() => _battleMap.AddObserver(this);

        public void LateDispose() => _battleMap.RemoveObserver(this);

        public void UpdateState(BattleMap battleMap)
        {
            foreach (MapObstacle obstacle in battleMap.Obstacles)
            {
                Vector3 extents = obstacle.WorldBounds.extents;
                float footprint = new Vector2(extents.x, extents.z).magnitude;
                _spawnBlockerService.Register(PlayerId.None, obstacle.transform,
                    footprint + _data.AsteroidSpawnBlockMargin);
            }
        }
    }
}
