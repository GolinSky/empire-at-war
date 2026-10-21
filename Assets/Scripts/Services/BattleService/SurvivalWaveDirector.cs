using System.Collections.Generic;
using System.Linq;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.BaseEntity.EntityFacades;
using EmpireAtWar.Entities.BaseEntity.Orders;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Services.UnitOrders;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SurvivalWaveDirector : ITickable
    {
        private const float IDLE_RETRY_INTERVAL = 0.25f;

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
        private readonly PlayerId _enemy;
        private readonly PlayerId _human;
        private readonly Queue<UnitTypeId> _pending = new();
        private readonly HashSet<long> _commanded = new();
        private readonly List<IEntity> _newUnits = new();
        private int _wave;
        private int _spawned;
        private float _settleTime;
        private float _nextIdleRetryTime;

        public SurvivalWaveDirector(SurvivalModel model, SurvivalWaveData data, BattleDefinition battle,
            SkirmishStateModel state, BattleUnitSpawner spawner, EdgeSpawnPointProvider points,
            IEntityLocator entities, IStationRegistry stations, IUnitOrderService orders, IMapModelObserver map)
        {
            _model = model; _data = data; _battle = battle; _state = state; _spawner = spawner;
            _points = points; _entities = entities; _stations = stations; _orders = orders; _map = map;
            _enemy = battle.Players.Single(player => player.Role == PlayerRole.WaveAttacker).Slot.Id;
            _human = battle.Human.Slot.Id;
        }

        /// <summary>The wave whose units have been queued; it trails <see cref="SurvivalModel.WaveIndex"/> between waves.</summary>
        public int Wave => _wave;
        /// <summary>Units of the current wave still waiting for a spawn point, in spawn order.</summary>
        public IReadOnlyCollection<UnitTypeId> Pending => _pending;
        public int Spawned => _spawned;
        public float SettleTime => _settleTime;

        /// <summary>
        /// Loads a saved wave in progress so it is not queued again. Spawn edges are picked afresh: earlier
        /// units have left their spawn points.
        /// </summary>
        public void Restore(int wave, IEnumerable<UnitTypeId> pending, int spawned, float settleTime)
        {
            _wave = wave;
            _pending.Clear();
            foreach (UnitTypeId unit in pending) _pending.Enqueue(unit);
            _spawned = spawned;
            _settleTime = settleTime;
            if (_pending.Count > 0) _points.BeginWave();
        }

        /// <summary>Restored attackers already have their attack order; they are not ordered again.</summary>
        public void AdoptLivingAttackers()
        {
            foreach (IEntity entity in _entities.Entities)
                if (entity.Owner == _enemy && !entity.HealthModel.IsDestroyed) _commanded.Add(entity.Id);
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
                    _pending.Dequeue(); _spawned++; _settleTime = 1f;
                }
            }
            // Batch completed orders instead of rebuilding their formation and engagement every frame.
            bool retryIdle = Time.time >= _nextIdleRetryTime;
            if (retryIdle) _nextIdleRetryTime = Time.time + IDLE_RETRY_INTERVAL;
            _newUnits.Clear();
            int alive = 0;
            foreach (IEntity entity in _entities.Entities)
            {
                if (entity.Owner != _enemy || entity.HealthModel.IsDestroyed) continue;
                alive++;
                if (_commanded.Add(entity.Id) || retryIdle && IsIdleShip(entity)) _newUnits.Add(entity);
            }
            if (_newUnits.Count > 0) _orders.IssueAttackMove(_newUnits, _map.GetStationPosition(_human));
            _settleTime -= Time.deltaTime;
            if (_pending.Count == 0 && alive == 0 && _settleTime <= 0) _model.CompleteWave();
        }

        // An ability cast or a finished attack-move leaves a ship without an order; it resumes the assault.
        private static bool IsIdleShip(IEntity entity) =>
            entity.IsShip() &&
            entity.GetFacade<IUnitOrderObserverFacade>().CurrentOrder == UnitOrderType.None &&
            !entity.GetFacade<IAbilityFacingFacade>().IsAbilityFacing;
    }
}
