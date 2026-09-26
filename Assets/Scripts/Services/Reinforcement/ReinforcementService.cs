using System;
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
using EmpireAtWar.Services.ReinforcementZones;
using EmpireAtWar.Services.StationFacing;
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
        ILateDisposable, IReinforcementChain, IObserver<BattleResult>
    {
        private readonly ReinforcementModel _model;
        private readonly PlayerFactionModel _playerFactionModel;
        private readonly ReinforcementData _data;
        private readonly InputServiceImpl _inputService;
        private readonly ICameraService _cameraService;
        private readonly ShipFactory _shipFactory;
        private readonly SquadronFactory _squadronFactory;
        private readonly MiningFacilityFactory _miningFacilityFactory;
        private readonly DefendPlatformFactory _defendPlatformFactory;
        private readonly IReinforcementZonesSystem _reinforcementZonesSystem;
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

        public ReinforcementService(
            ReinforcementModel model,
            PlayerFactionModel playerFactionModel,
            ReinforcementData data,
            InputServiceImpl inputService,
            ICameraService cameraService,
            ShipFactory shipFactory,
            SquadronFactory squadronFactory,
            MiningFacilityFactory miningFacilityFactory,
            DefendPlatformFactory defendPlatformFactory,
            IReinforcementZonesSystem reinforcementZonesSystem,
            FogOfWarSystem fogOfWarSystem,
            IStationFacingService stationFacingService,
            IEntityLocator entityLocator,
            INotifier<BattleResult> battleVictoryNotifier)
        {
            _model = model;
            _playerFactionModel = playerFactionModel;
            _data = data;
            _inputService = inputService;
            _cameraService = cameraService;
            _shipFactory = shipFactory;
            _squadronFactory = squadronFactory;
            _miningFacilityFactory = miningFacilityFactory;
            _defendPlatformFactory = defendPlatformFactory;
            _reinforcementZonesSystem = reinforcementZonesSystem;
            _fogOfWarSystem = fogOfWarSystem;
            _stationFacingService = stationFacingService;
            _entityLocator = entityLocator;
            _battleVictoryNotifier = battleVictoryNotifier;
        }

        public void Initialize()
        {
            _inputService.OnEndDrag += Interrupt;
            _battleVictoryNotifier.AddObserver(this);
        }

        public void LateDispose()
        {
            _inputService.OnEndDrag -= Interrupt;
            _battleVictoryNotifier.RemoveObserver(this);
        }

        public void UpdateState(BattleResult result)
        {
            _hasBattleEnded = true;
            CancelPlacement();
            _inputService.Block(true);
        }

        private void CancelPlacement()
        {
            if (!_model.IsTrySpawning)
            {
                return;
            }

            _model.IsTrySpawning = false;
            _spawnReinforcement.Destroy();
            _inputService.Block(false);
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
            bool canSpawn = _entityLocator.IsStationOperational(PlayerType.Player) &&
                _spawnReinforcement.CanSpawn && IsPlacementValid(spawnPosition);

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
                    ShipEntity ship = _shipFactory.Create(PlayerType.Player, _currentShipType, spawnPosition);
                    ship.OnRelease += HandleShipDestroying;
                    _model.AddUnitCapacity(_currentShipType);
                    break;
                case SpawnType.Squadron:
                    SquadronType squadronType = _currentSquadronType;
                    Squadron squadron = _squadronFactory.Create(PlayerType.Player, squadronType,
                        spawnPosition, _stationFacingService.GetRotation(PlayerType.Player));
                    squadron.Released += () => _model.RemoveUnitCapacity(squadronType);
                    _model.AddUnitCapacity(squadronType);
                    break;
                case SpawnType.MiningFacility:
                    MiningFacilityType facilityType = _currentFacilityType;
                    var facility = _miningFacilityFactory.Create(PlayerType.Player, facilityType, spawnPosition);
                    facility.OnRelease += () =>
                        _playerFactionModel.ReleaseStructure<MiningFacilityUnitRequest>(facilityType.ToString());
                    break;
                case SpawnType.DefendPlatform:
                    DefendPlatformType platformType = _currentPlatformType;
                    var platform = _defendPlatformFactory.Create(PlayerType.Player, platformType, spawnPosition);
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

        public void TrySpawnReinforcement(string id)
        {
            if (_hasBattleEnded || !_entityLocator.IsStationOperational(PlayerType.Player))
            {
                _model.InvokeSpawnShipEvent(false);
                return;
            }

            if (Enum.TryParse(id, out ShipType shipType))
            {
                TrySpawnShip(shipType);
            }
            else if (Enum.TryParse(id, out SquadronType squadronType))
            {
                TrySpawnSquadron(squadronType);
            }
            else if (Enum.TryParse(id, out MiningFacilityType facilityType))
            {
                StartSpawnSequence(SpawnType.MiningFacility);
                _currentFacilityType = facilityType;
                _spawnReinforcement = CreateSpawnView(_data.GetSpawnPrefab(facilityType));
            }
            else if (Enum.TryParse(id, out DefendPlatformType defendPlatformType))
            {
                StartSpawnSequence(SpawnType.DefendPlatform);
                _currentPlatformType = defendPlatformType;
                _spawnReinforcement = CreateSpawnView(_data.GetSpawnPrefab(defendPlatformType));
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
            spawnView.SetRotation(_stationFacingService.GetRotation(PlayerType.Player));
            return spawnView;
        }

        private void StartSpawnSequence(SpawnType spawnType)
        {
            _currentSpawnType = spawnType;
            _inputService.Block(true);
            _model.IsTrySpawning = true;
        }

        private bool IsPlacementValid(Vector3 position)
        {
            return _entityLocator.IsStationOperational(PlayerType.Player) &&
                (_currentSpawnType == SpawnType.Ship || _currentSpawnType == SpawnType.Squadron
                ? _reinforcementZonesSystem.IsPositionInOwnedZone(PlayerType.Player, position)
                : !_fogOfWarSystem.IsHidden(position) &&
                  !_reinforcementZonesSystem.IsPositionInAnyZone(position));
        }
    }
}
