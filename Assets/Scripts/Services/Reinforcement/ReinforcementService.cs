using System;
using EmpireAtWar.Models.Players;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Entities.BaseEntity;
using EmpireAtWar.Entities.DefendPlatform;
using EmpireAtWar.Entities.Game;
using EmpireAtWar.Entities.MiningFacility;
using EmpireAtWar.Entities.Squadrons;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Mvc;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.CaptureSites;
using EmpireAtWar.Services.Input;
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.StationFacing;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.Reinforcement;
using UnityEngine;
using ViewComponents;
using Zenject;
using Object = UnityEngine.Object;
using ShipEntity = EmpireAtWar.Ship.Ship;

namespace EmpireAtWar.Services.Reinforcement
{
    public interface IReinforcementService
    {
        void TrySpawnReinforcement(UnitRequest request);
    }

    public class ReinforcementService : Service, IReinforcementService, ITickable, IInitializable,
        ILateDisposable, IReinforcementChain, IObserver<BattleResult>
    {
        private readonly ReinforcementModel _model;
        private readonly PlayerSlot _owner;
        private readonly PlayerFactionModel _playerFactionModel;
        private readonly ReinforcementData _data;
        private readonly IInputLock _inputLock;
        private readonly IPointerInput _pointer;
        private readonly ICameraService _cameraService;
        private readonly ShipFactory _shipFactory;
        private readonly SquadronFactory _squadronFactory;
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
        private readonly ICaptureSitesSystem _captureSites;
        private readonly FogOfWarSystem _fogOfWarSystem;
        private readonly IStationFacingService _stationFacingService;
        private readonly IEntityLocator _entityLocator;
        private readonly INotifier<BattleResult> _battleVictoryNotifier;

        private IChainHandler<UnitRequest> _nextChain;
        private UnitSpawnView _spawnReinforcement;
        private ShipType _currentShipType;
        private SquadronType _currentSquadronType;
        private SpawnType _currentSpawnType;
        private MiningFacilityType _currentFacilityType;
        private DefendPlatformType _currentPlatformType;
        private bool _hasBattleEnded;
        private IDisposable _placementLock;

        public ReinforcementService(
            ReinforcementModel model,
            PlayerFactionModel playerFactionModel,
            ReinforcementData data,
            IInputLock inputLock,
            IPointerInput pointer,
            ICameraService cameraService,
            ShipFactory shipFactory,
            SquadronFactory squadronFactory,
            MiningFacilityFactory miningFacilityFactory,
            DefendPlatformFactory defendPlatformFactory,
            IReinforcementZonesSystem reinforcementZonesSystem,
            ICaptureSitesSystem captureSites,
            FogOfWarSystem fogOfWarSystem,
            IStationFacingService stationFacingService,
            IEntityLocator entityLocator,
            INotifier<BattleResult> battleVictoryNotifier,
            PlayerSlot owner)
        {
            _owner = owner;
            _model = model;
            _playerFactionModel = playerFactionModel;
            _data = data;
            _inputLock = inputLock;
            _pointer = pointer;
            _cameraService = cameraService;
            _shipFactory = shipFactory;
            _squadronFactory = squadronFactory;
            _miningFacilityFactory = miningFacilityFactory;
            _defendPlatformFactory = defendPlatformFactory;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _captureSites = captureSites;
            _fogOfWarSystem = fogOfWarSystem;
            _stationFacingService = stationFacingService;
            _entityLocator = entityLocator;
            _battleVictoryNotifier = battleVictoryNotifier;
        }

        public void Initialize()
        {
            _pointer.PrimaryReleased += Interrupt;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            _pointer.PrimaryReleased -= Interrupt;
            _battleVictoryNotifier.RemoveObserver(this);
        }

        public void UpdateState(BattleResult result)
        {
            _hasBattleEnded = true;
            CancelPlacement();
            // The battle is over: gameplay input stays locked for the rest of the scene.
            _inputLock.Acquire();
        }

        private void CancelPlacement()
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            _model.IsTrySpawning = false;
            _spawnReinforcement.Destroy();
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
            Vector3 spawnPosition = _cameraService.GetWorldPoint(screenPosition, _spawnReinforcement.Position);
            bool canSpawn = _entityLocator.IsStationOperational(_owner.Id) &&
                _spawnReinforcement.CanSpawn && IsPlacementValid(spawnPosition);

            if (canSpawn)
            {
                SpawnReinforcement(spawnPosition);
            }

            _spawnReinforcement.Destroy();
            _placementLock.Dispose();
            _model.InvokeSpawnShipEvent(canSpawn);
        }

        private void SpawnReinforcement(Vector3 spawnPosition)
        {
            switch (_currentSpawnType)
            {
                case SpawnType.Ship:
                    ShipEntity ship = _shipFactory.Create(_owner.Id, _currentShipType, spawnPosition);
                    ship.OnRelease += HandleShipDestroying;
                    _model.AddUnitCapacity(_currentShipType);
                    break;
                case SpawnType.Squadron:
                    SquadronType squadronType = _currentSquadronType;
                    Squadron squadron = _squadronFactory.Create(_owner.Id, squadronType,
                        spawnPosition, _stationFacingService.GetRotation(_owner.Id));
                    squadron.Released += () => _model.RemoveUnitCapacity(squadronType);
                    _model.AddUnitCapacity(squadronType);
                    break;
                case SpawnType.MiningFacility:
                    MiningFacilityType facilityType = _currentFacilityType;
                    var facility = _miningFacilityFactory.Create(_owner.Id, facilityType, spawnPosition);
                    facility.OnRelease += () =>
                        _playerFactionModel.ReleaseStructure<MiningFacilityUnitRequest>(facilityType.ToString());
                    break;
                case SpawnType.DefendPlatform:
                    DefendPlatformType platformType = _currentPlatformType;
                    var platform = _defendPlatformFactory.Create(_owner.Id, platformType, spawnPosition);
                    platform.OnRelease += () =>
                        _playerFactionModel.ReleaseStructure<DefendPlatformUnitRequest>(platformType.ToString());
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

            Vector3 position = _cameraService.GetWorldPoint(_pointer.Position, _spawnReinforcement.Position);
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

            _nextChain?.Handle(request);
        }

        public void TrySpawnReinforcement(UnitRequest request)
        {
            if (_hasBattleEnded || !_entityLocator.IsStationOperational(_owner.Id))
            {
                _model.InvokeSpawnShipEvent(false);
                return;
            }

            switch (request)
            {
                case ShipUnitRequest shipUnitRequest:
                    TrySpawnShip(shipUnitRequest.Key);
                    break;
                case SquadronUnitRequest squadronUnitRequest:
                    TrySpawnSquadron(squadronUnitRequest.Key);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    StartSpawnSequence(SpawnType.MiningFacility);
                    _currentFacilityType = miningFacilityUnitRequest.Key;
                    _spawnReinforcement = CreateSpawnView(_data.GetSpawnPrefab(_currentFacilityType));
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    StartSpawnSequence(SpawnType.DefendPlatform);
                    _currentPlatformType = defendPlatformUnitRequest.Key;
                    _spawnReinforcement = CreateSpawnView(_data.GetSpawnPrefab(_currentPlatformType));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
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
            _spawnReinforcement = CreateSpawnView(_data.GetSpawnPrefab(shipType));
        }

        private void TrySpawnSquadron(SquadronType squadronType)
        {
            if (!_model.CanSpawnUnit(squadronType))
            {
                _model.InvokeSpawnShipEvent(false);
                return;
            }

            StartSpawnSequence(SpawnType.Squadron);
            _currentSquadronType = squadronType;
            _spawnReinforcement = CreateSpawnView(_data.GetSpawnPrefab(squadronType));
        }

        private UnitSpawnView CreateSpawnView(UnitSpawnView prefab)
        {
            UnitSpawnView spawnView = Object.Instantiate(prefab);
            spawnView.UpdatePosition(spawnView.Position);
            spawnView.SetRotation(_stationFacingService.GetRotation(_owner.Id));
            return spawnView;
        }

        private void StartSpawnSequence(SpawnType spawnType)
        {
            // A placement that is still running would otherwise keep its input lock forever.
            CancelPlacement();
            _currentSpawnType = spawnType;
            _placementLock = _inputLock.Acquire();
            _model.IsTrySpawning = true;
        }

        private bool IsPlacementValid(Vector3 position)
        {
            return _entityLocator.IsStationOperational(_owner.Id) &&
                (_currentSpawnType == SpawnType.Ship
                ? _reinforcementZonesSystem.IsPositionInAlliedZone(_owner.Id, position) &&
                  _reinforcementZonesSystem.IsShipSpawnPositionClear(_currentShipType, position)
                : _currentSpawnType == SpawnType.Squadron
                ? _reinforcementZonesSystem.IsPositionInAlliedZone(_owner.Id, position)
                :!_fogOfWarSystem.IsHidden(position) &&
                  !_reinforcementZonesSystem.IsPositionInAnyZone(position) &&
                  !_captureSites.IsPositionInAnySite(position));
        }
    }
}
