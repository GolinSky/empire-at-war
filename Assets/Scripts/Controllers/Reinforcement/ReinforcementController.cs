using System;
using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.DefendPlatform;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Ship;
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.Reinforcement;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;


namespace EmpireAtWar.Controllers.Reinforcement
{
    public class ReinforcementController : Controller<ReinforcementModel>, IReinforcementCommand, ITickable,
        IInitializable, ILateDisposable, IReinforcementChain
    {
        private readonly InputService _inputService;
        private readonly ICameraService _cameraService;
        private readonly ShipFacadeFactory _shipFacadeFactory;
        private readonly MiningFacilityFacade _miningFacilityFacade;
        private readonly DefendPlatformFacade _defendPlatformFacade;

        private IChainHandler<UnitRequest> _nextChain;
        private UnitSpawnView _spawnReinforcement;
        private ShipType _currentShipType;
        private SpawnType _currentSpawnType;
        private MiningFacilityType _currentFacilityType;
        private DefendPlatformType _currentPlatformType;

        public ReinforcementController(
            ReinforcementModel model,
            InputService inputService,
            ICameraService cameraService,
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade) : base(model)
        {
            _inputService = inputService;
            _cameraService = cameraService;
            _shipFacadeFactory = shipFacadeFactory;
            _miningFacilityFacade = miningFacilityFacade;
            _defendPlatformFacade = defendPlatformFacade;
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
            if (!Model.IsTrySpawning) return;

            Model.IsTrySpawning = false;
            Vector3 spawnPosition = _cameraService.GetWorldPoint(screenPosition, _spawnReinforcement.Position);
            bool canSpawn = _spawnReinforcement.CanSpawn;
            
            if (canSpawn)
            {
                switch (_currentSpawnType)
                {
                    case SpawnType.Ship:
                    {
                        ShipView ship = _shipFacadeFactory.Create(PlayerType.Player, _currentShipType, spawnPosition);
                        ship.OnRelease += HandleShipDestroying;
                        Model.AddUnitCapacity(_currentShipType); 
                        break;
                    }
                    case SpawnType.MiningFacility:
                    {
                        _miningFacilityFacade.Create(PlayerType.Player, _currentFacilityType, spawnPosition);
                        break;
                    }
                    case SpawnType.DefendPlatform:
                    {
                        _defendPlatformFacade.Create(PlayerType.Player, _currentPlatformType, spawnPosition);
                        break;
                    }
                }
            }
            _spawnReinforcement.Destroy();
            _inputService.Block(false);
            Model.InvokeSpawnShipEvent(canSpawn);
        }

        private void HandleShipDestroying(ShipType shipType)
        {
            Model.RemoveUnitCapacity(shipType);
        }
        
        public void Tick()
        {
            if (!Model.IsTrySpawning) return;

            Vector3 position = _cameraService.GetWorldPoint(_inputService.TouchPosition, _spawnReinforcement.Position);
            position.y = 0;
            _spawnReinforcement.UpdatePosition(position);
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
                    Model.UpdateShipData(shipUnitRequest);
                    Model.AddReinforcement(shipUnitRequest);
                    break;
                case MiningFacilityUnitRequest miningFacilityUnitRequest:
                    Model.AddReinforcement(miningFacilityUnitRequest);
                    break;
                case DefendPlatformUnitRequest defendPlatformUnitRequest:
                    Model.AddReinforcement(defendPlatformUnitRequest);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
          
            if (_nextChain != null)
            {
                _nextChain.Handle(request);
            }
        }

        public void TrySpawnReinforcement(string id)
        {
            if (Enum.TryParse(id, out ShipType shipType))
            {
                if (Model.CanSpawnUnit(shipType))
                {
                    StartSpawnSequence(SpawnType.Ship);
                    _currentShipType = shipType;
                    _spawnReinforcement = Object.Instantiate(Model.GetSpawnPrefab(shipType));
                }
            }
            else if (Enum.TryParse(id, out MiningFacilityType facilityType))
            {
                StartSpawnSequence(SpawnType.MiningFacility);
                _currentFacilityType = facilityType;
                _spawnReinforcement = Object.Instantiate(Model.GetSpawnPrefab(facilityType));
            }
            else if(Enum.TryParse(id, out DefendPlatformType defendPlatformType))
            {
                StartSpawnSequence(SpawnType.DefendPlatform);
                _currentPlatformType = defendPlatformType;
                _spawnReinforcement = Object.Instantiate(Model.GetSpawnPrefab(defendPlatformType));
            }
        }

        private void StartSpawnSequence(SpawnType spawnType)
        {
            _currentSpawnType = spawnType;
            _inputService.Block(true);
            Model.IsTrySpawning = true;
        }
    }
}