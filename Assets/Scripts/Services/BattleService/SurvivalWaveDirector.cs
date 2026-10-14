using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Models.MiniMap;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SurvivalWaveDirector : ITickable
    {
        private readonly SurvivalModel _model;
        private readonly SurvivalWaveData _data;
        private readonly BattleDefinition _battle;
        private readonly SkirmishStateModel _state;
        private readonly BattleUnitSpawner _spawner;
        private readonly EdgeSpawnPointProvider _points;
        private readonly IEntityLocator _entities;
        private readonly IStationRegistry _stations;
        private readonly IUnitOrderService _orders;
        private readonly IMapModelObserver _map;
        private readonly MiniMapData _miniMap;
        private readonly PlayerId _enemy;
        private readonly PlayerId _human;
        private readonly Queue<UnitTypeId> _pending = new();
        private readonly HashSet<long> _commanded = new();
        private readonly List<IEntity> _newUnits = new();
        private int _wave;
        private int _spawned;
        private float _settleTime;

        public SurvivalWaveDirector(SurvivalModel model, SurvivalWaveData data, BattleDefinition battle,
            SkirmishStateModel state, BattleUnitSpawner spawner, EdgeSpawnPointProvider points,
            IEntityLocator entities, IStationRegistry stations, IUnitOrderService orders, IMapModelObserver map,
            MiniMapData miniMap)
        {
            _model = model; _data = data; _battle = battle; _state = state; _spawner = spawner;
            _points = points; _entities = entities; _stations = stations; _orders = orders; _map = map;
            _miniMap = miniMap;
            _enemy = battle.Players.Single(player => player.Role == PlayerRole.WaveAttacker).Slot.Id;
            _human = battle.Human.Slot.Id;
        }

        public void Tick()
        {
            if (_state.State != BattleState.Running) return;
            if (!_stations.TryGetLivingStation(_human, out _)) { _model.Lose(); return; }
            _model.Advance(Time.deltaTime);
            if (_model.Phase != SurvivalPhase.WaveActive) return;
            SurvivalWave wave = _data.GetWave(_battle.Survival.EnemyFaction, _model.WaveIndex);
            if (_wave != _model.WaveIndex)
            {
                _wave = _model.WaveIndex; _spawned = 0; _points.BeginWave(); _commanded.Clear();
                foreach (SurvivalWaveEntry entry in wave.Entries)
                    for (int count = 0; count < entry.Quantity; count++) _pending.Enqueue(entry.Unit);
            }
            if (_pending.Count > 0)
            {
                UnitTypeId unit = _pending.Peek();
                if (_points.TryGet(_enemy, unit, _spawned % wave.SpawnGroupCount, out Vector3 position))
                {
                    _spawner.Spawn(_enemy, unit, position);
                    _miniMap.AddMark(MarkType.Ship, position);
                    _pending.Dequeue(); _spawned++; _settleTime = 1f;
                }
            }
            _newUnits.Clear();
            int alive = 0;
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.Owner != _enemy || entity.HealthModel.IsDestroyed) continue;
                alive++;
                if (_commanded.Add(entity.Id)) _newUnits.Add(entity);
            }
            if (_newUnits.Count > 0) _orders.IssueAttackMove(_newUnits, _map.GetStationPosition(_human));
            _settleTime -= Time.deltaTime;
            if (_pending.Count == 0 && alive == 0 && _settleTime <= 0) _model.CompleteWave();
        }
    }
}
