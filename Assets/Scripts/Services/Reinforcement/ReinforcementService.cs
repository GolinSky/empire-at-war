using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Services.Stations;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Views.SpawnArea;
using UnityEngine;
using Zenject;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementService
    {
        void TrySpawnReinforcement(UnitRequest request);
    }

    /// <summary>Runs the drag-to-place flow: input lock, preview, validation and release.
    /// What may be placed where is decided by the request's <see cref="IReinforcementPlacement"/>.</summary>
    public class ReinforcementService : Service, IReinforcementService, ITickable, IInitializable,
        ILateDisposable, IReinforcementPool, IObserver<BattleResult>
    {
        private readonly IInputLock _inputLock;
        private readonly IPointerInput _pointerInput;
        private readonly ICameraService _cameraService;
        private readonly IStationRegistry _stationRegistry;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;
        private readonly ISpawnAreaOverlay _spawnAreaOverlay;
        private IDisposable _placementLock;
        private IReinforcementPlacement _placement;

        private readonly ReinforcementModel _model;
        private readonly ReinforcementPlacementFactory _placementFactory;
        private readonly PlayerSlot _owner;
        private UnitSpawnView _preview;
        private UnitRequest _currentRequest;

        private bool _hasBattleEnded;

        public ReinforcementService(
            IInputLock inputLock,
            IPointerInput pointerInput,
            ICameraService cameraService,
            IStationRegistry stationRegistry,
            INotifier<BattleResult> battleVictoryNotifier,
            ISpawnAreaOverlay spawnAreaOverlay,
            ReinforcementModel model,
            ReinforcementPlacementFactory placementFactory,
            PlayerSlot owner)
        {
            _owner = owner;
            _model = model;
            _placementFactory = placementFactory;
            _inputLock = inputLock;
            _pointerInput = pointerInput;
            _cameraService = cameraService;
            _stationRegistry = stationRegistry;
            _battleVictoryNotifier = battleVictoryNotifier;
            _spawnAreaOverlay = spawnAreaOverlay;
        }

        public void Initialize()
        {
            _pointerInput.PrimaryReleased += Interrupt;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            _pointerInput.PrimaryReleased -= Interrupt;
            _battleVictoryNotifier.RemoveObserver(this);
        }

        public void UpdateState(BattleResult result)
        {
            _hasBattleEnded = true;
            CancelPlacement();
            // The battle is over: gameplay input stays locked for the rest of the scene.
            _inputLock.Acquire();
        }

        public void Tick()
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            Vector3 position = _cameraService.GetWorldPoint(_pointerInput.Position, _preview.Position);
            position.y = 0;
            _preview.UpdatePosition(position);
            _preview.SetPlacementValidity(IsPlacementValid(position));
        }

        public void Add(UnitRequest request)
        {
            switch (request)
            {
                case ShipUnitRequest shipUnitRequest:
                    _model.UpdateShipData(shipUnitRequest);
                    _model.AddReinforcement(shipUnitRequest);
                    break;
                case SquadronUnitRequest squadronUnitRequest:
                    _model.UpdateSquadronData(squadronUnitRequest);
                    _model.AddReinforcement(squadronUnitRequest);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    _model.AddReinforcement(miningFacilityUnitRequest);
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    _model.AddReinforcement(defendPlatformUnitRequest);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
        }

        public void TrySpawnReinforcement(UnitRequest request)
        {
            if (_hasBattleEnded || !_stationRegistry.IsStationOperational(_owner.Id) ||
                !_placementFactory.TryCreate(request, out IReinforcementPlacement placement))
            {
                _model.InvokeSpawnShipEvent(false);
                return;
            }

            // A placement that is still running would otherwise keep its input lock forever.
            CancelPlacement();
            _currentRequest = request;
            _placement = placement;
            _placementLock = _inputLock.Acquire();
            _model.IsTrySpawning = true;
            _preview = placement.CreatePreview();
            _spawnAreaOverlay.Show(_owner.Id);
        }

        private void CancelPlacement()
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            _model.IsTrySpawning = false;
            _preview.Destroy();
            _spawnAreaOverlay.Hide();
            _placementLock.Dispose();
            _model.InvokeSpawnShipEvent(false);
        }

        private void Interrupt(Vector2 screenPosition)
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            _model.IsTrySpawning = false;
            Vector3 spawnPosition = _cameraService.GetWorldPoint(screenPosition, _preview.Position);
            bool canSpawn = _preview.CanSpawn && IsPlacementValid(spawnPosition);

            if (canSpawn)
            {
                _placement.Spawn(spawnPosition);
                _model.ConsumeReinforcement(_currentRequest);
            }

            _preview.Destroy();
            _spawnAreaOverlay.Hide();
            _placementLock.Dispose();
            _model.InvokeSpawnShipEvent(canSpawn);
        }

        private bool IsPlacementValid(Vector3 position)
        {
            return _stationRegistry.IsStationOperational(_owner.Id) && _placement.IsPositionValid(position);
        }
    }
}
