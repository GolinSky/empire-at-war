using System;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using ViewComponents;
using Zenject;
using Object = UnityEngine.Object;
using InputServiceImpl = EmpireAtWar.Services.InputService.InputService;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementService
    {
        void TrySpawnReinforcement(string id);
    }

    public class ReinforcementService : Service, IReinforcementService, ITickable, IInitializable,
        ILateDisposable, IReinforcementChain
    {
        private readonly ReinforcementModel _model;
        private readonly ReinforcementData _data;
        private readonly InputServiceImpl _inputService;
        private readonly ICameraService _cameraService;
        private readonly ShipFacadeFactory _shipFacadeFactory;
        private readonly MiningFacilityFacade _miningFacilityFacade;
        private readonly DefendPlatformFacade _defendPlatformFacade;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly FogOfWarSystem _fogOfWarSystem;

        private IChainHandler<UnitRequest> _nextChain;
        private UnitSpawnView _spawnReinforcement;
        private ShipType _currentShipType;
        private SpawnType _currentSpawnType;
        private MiningFacilityType _currentFacilityType;
        private DefendPlatformType _currentPlatformType;

        public ReinforcementService(
            ReinforcementModel model,
            ReinforcementData data,
            InputServiceImpl inputService,
            ICameraService cameraService,
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade,
            IReinforcementZonesSystem reinforcementZonesSystem,
            FogOfWarSystem fogOfWarSystem)
        {
            _model = model;
            _data = data;
            _inputService = inputService;
            _cameraService = cameraService;
            _shipFacadeFactory = shipFacadeFactory;
            _miningFacilityFacade = miningFacilityFacade;
            _defendPlatformFacade = defendPlatformFacade;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _fogOfWarSystem = fogOfWarSystem;
        }

        public void Initialize()
        {
            _inputService.OnEndDrag += Interrupt;
        }

        public void LateDispose()
        {
            _inputService.OnEndDrag -= Interrupt;
        }

        private void Interrupt(Vector2 screenPosition)
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            _model.IsTrySpawning = false;
            Vector3 spawnPosition = _cameraService.GetWorldPoint(screenPosition, _spawnReinforcement.Position);
            bool canSpawn = _spawnReinforcement.CanSpawn && IsPlacementValid(spawnPosition);

            if (canSpawn)
            {
                SpawnReinforcement(spawnPosition);
            }

            _spawnReinforcement.Destroy();
            _inputService.Block(false);
            _model.InvokeSpawnShipEvent(canSpawn);
        }

        private void SpawnReinforcement(Vector3 spawnPosition)
        {
            switch (_currentSpawnType)
            {
                case SpawnType.Ship:
                    ShipEntity ship = _shipFacadeFactory.Create(PlayerType.Player, _currentShipType, spawnPosition);
                    ship.OnRelease += HandleShipDestroying;
                    _model.AddUnitCapacity(_currentShipType);
                    break;
                case SpawnType.MiningFacility:
                    _miningFacilityFacade.Create(PlayerType.Player, _currentFacilityType, spawnPosition);
                    break;
                case SpawnType.DefendPlatform:
                    _defendPlatformFacade.Create(PlayerType.Player, _currentPlatformType, spawnPosition);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void HandleShipDestroying(ShipType shipType)
        {
            _model.RemoveUnitCapacity(shipType);
        }

        public void Tick()
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            Vector3 position = _cameraService.GetWorldPoint(_inputService.TouchPosition, _spawnReinforcement.Position);
            position.y = 0;
            _spawnReinforcement.UpdatePosition(position);
            _spawnReinforcement.SetPlacementValidity(IsPlacementValid(position));
        }

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            _nextChain = chainHandler;
            return _nextChain;
        }

        public void Handle(UnitRequest request)
        {
            switch (request)
            {
                case ShipUnitRequest shipUnitRequest:
                    _model.UpdateShipData(shipUnitRequest);
                    _model.AddReinforcement(shipUnitRequest);
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

            _nextChain?.Handle(request);
        }

        public void TrySpawnReinforcement(string id)
        {
            if (Enum.TryParse(id, out ShipType shipType))
            {
                TrySpawnShip(shipType);
            }
            else if (Enum.TryParse(id, out MiningFacilityType facilityType))
            {
                StartSpawnSequence(SpawnType.MiningFacility);
                _currentFacilityType = facilityType;
                _spawnReinforcement = Object.Instantiate(_data.GetSpawnPrefab(facilityType));
            }
            else if (Enum.TryParse(id, out DefendPlatformType defendPlatformType))
            {
                StartSpawnSequence(SpawnType.DefendPlatform);
                _currentPlatformType = defendPlatformType;
                _spawnReinforcement = Object.Instantiate(_data.GetSpawnPrefab(defendPlatformType));
            }
        }

        private void TrySpawnShip(ShipType shipType)
        {
            if (!_model.CanSpawnUnit(shipType))
            {
                return;
            }

            StartSpawnSequence(SpawnType.Ship);
            _currentShipType = shipType;
            _spawnReinforcement = Object.Instantiate(_data.GetSpawnPrefab(shipType));
        }

        private void StartSpawnSequence(SpawnType spawnType)
        {
            _currentSpawnType = spawnType;
            _inputService.Block(true);
            _model.IsTrySpawning = true;
        }

        private bool IsPlacementValid(Vector3 position)
        {
            return _currentSpawnType == SpawnType.Ship
                ? _reinforcementZonesSystem.IsPositionInOwnedZone(PlayerType.Player, position)
                : !_fogOfWarSystem.IsHidden(position) &&
                  !_reinforcementZonesSystem.IsPositionInAnyZone(position);
        }
    }
}
