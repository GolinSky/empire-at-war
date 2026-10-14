using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using EmpireAtWar.Controllers.Game;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.Map;
using EmpireAtWar.Entities.Map.Generation;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Entities.Units;
using EmpireAtWar.Lifecycle;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Services.ShipSpawning;
using UnityEngine;

namespace EmpireAtWar.Services.Battle
{
    public sealed class SimulationDirector : ITickable, ILateDisposable
    {
        private readonly SimulationModel _model;
        private readonly SkirmishStateModel _state;
        private readonly SkirmishOrchestrator _flow;
        private readonly BattleUnitSpawner _spawner;
        private readonly IShipSpawnClearance _clearance;
        private readonly IEntityLocator _entities;
        private readonly SimulationTargeting _targeting;
        private readonly SimulationResetService _reset;
        private readonly IMapModelObserver _map;
        private readonly MapLayoutView _mapView;
        private readonly ICameraService _camera;
        private readonly CancellationTokenSource _cancellation = new();
        private readonly Queue<(PlayerId Owner, UnitTypeId Unit, Vector3 Position)> _pending = new();
        private bool _prepared;
        private bool _resetting;
        private float _settleTime;

        public SimulationDirector(SimulationModel model, SkirmishStateModel state, SkirmishOrchestrator flow,
            BattleUnitSpawner spawner, IShipSpawnClearance clearance, IEntityLocator entities,
            SimulationTargeting targeting, SimulationResetService reset,
            IMapModelObserver map, MapLayoutView mapView, ICameraService camera)
        {
            _model = model; _state = state; _flow = flow; _spawner = spawner; _clearance = clearance;
            _entities = entities; _targeting = targeting; _reset = reset;
            _map = map; _mapView = mapView; _camera = camera;
        }

        public void Tick()
        {
            if (_state.State != BattleState.Running || _resetting || _model.Result != null) return;
            if (!_prepared)
            {
                MapLayout layout = EmptyLayoutGenerator.Generate(_model.Setup,
                    EmptyLayoutGenerator.GetSpacing(_model.Setup.FleetA, _clearance),
                    EmptyLayoutGenerator.GetSpacing(_model.Setup.FleetB, _clearance));
                // Keep the shared range instance: minimap views and navigation cache it.
                _map.SizeRange.SetValue(layout.SizeRange);
                _mapView.UpdateBounds(_map.SizeRange);
                _camera.MoveTo(layout.GetStationPosition(new PlayerId(0)));
                PrepareFleet(0, _model.Setup.FleetA); PrepareFleet(1, _model.Setup.FleetB);
                _prepared = true;
                if ((_state.Speed == GameSpeed.Fast) != _model.Setup.FastSpeed) _flow.ToggleSpeedUp();
            }
            if (_pending.Count > 0)
            {
                var spawn = _pending.Dequeue();
                _spawner.Spawn(spawn.Owner, spawn.Unit, spawn.Position);
                _settleTime = 2f;
                return;
            }
            _settleTime -= Time.deltaTime;
            if (_settleTime > 0) return;
            _model.CompleteSpawning();
            var a = Living(0); var b = Living(1);
            _targeting.Command(a, b); _targeting.Command(b, a);
            _model.Advance(Time.deltaTime, a.Count, b.Count,
                a.Sum(entity => entity.HealthModel.Hull), b.Sum(entity => entity.HealthModel.Hull));
        }

        private List<IEntity> Living(int side) => _entities.Entities.Where(entity =>
            entity.Owner.Index == side && !entity.HealthModel.IsDestroyed && entity.HealthModel.HasUnits).ToList();

        private void PrepareFleet(int side, IReadOnlyList<SimulationFleetEntry> fleet)
        {
            int count = fleet.Sum(entry => entry.Quantity);
            int columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            float spacing = EmptyLayoutGenerator.GetSpacing(fleet, _clearance);
            int index = 0;
            foreach (SimulationFleetEntry entry in fleet)
                for (int i = 0; i < entry.Quantity; i++, index++)
                {
                    Vector3 position = EmptyLayoutGenerator.GetPosition(side, _model.Setup.Distance, index, columns, spacing);
                    _pending.Enqueue((new PlayerId(side), entry.Unit, position));
                }
        }

        public async Awaitable RestartAsync(SimulationSetup setup)
        {
            if (_resetting) return;
            _resetting = true;
            _flow.BeginSimulationRestart();
            try
            {
                await _reset.ResetAsync(setup, _cancellation.Token);
                _pending.Clear(); _targeting.Clear(); _prepared = false; _settleTime = 0;
                _model.Reset(setup);
                _flow.CompleteSimulationRestart(setup.FastSpeed);
            }
            finally { _resetting = false; }
        }

        public void LateDispose() { _cancellation.Cancel(); _cancellation.Dispose(); }
    }
}
