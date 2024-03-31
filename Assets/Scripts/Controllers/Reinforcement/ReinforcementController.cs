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
using EmpireAtWar.Views.DefendPlatform;
using EmpireAtWar.Views.MiningFacility;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Views.Ship;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;


namespace EmpireAtWar.Controllers.Reinforcement
{
    public interface IReinforcementChain:IChainHandler<UnitRequest> {}

    public class ReinforcementController : Controller<ReinforcementModel>, IReinforcementCommand, ITickable,
        IInitializable, ILateDisposable, IReinforcementChain
    {
        private readonly InputService inputService;
        private readonly ICameraService cameraService;
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly MiningFacilityFacade miningFacilityFacade;
        private readonly DefendPlatformFacade defendPlatformFacade;

        private IChainHandler<UnitRequest> nextChain;
        private UnitSpawnView spawnReinforcement;
        private ShipType currentShipType;
        private SpawnType currentSpawnType;
        private MiningFacilityType currentFacilityType;
        private DefendPlatformType currentPlatformType;

        public ReinforcementController(
            ReinforcementModel model,
            InputService inputService,
            ICameraService cameraService,
            ShipFacadeFactory shipFacadeFactory,
            MiningFacilityFacade miningFacilityFacade,
            DefendPlatformFacade defendPlatformFacade) : base(model)
        {
            this.inputService = inputService;
            this.cameraService = cameraService;
            this.shipFacadeFactory = shipFacadeFactory;
            this.miningFacilityFacade = miningFacilityFacade;
            this.defendPlatformFacade = defendPlatformFacade;
        }

        public void Initialize()
        {
            inputService.OnEndDrag += Interrupt;
        }
        
        public void LateDispose()
        {
            inputService.OnEndDrag -= Interrupt;
        }

        private void Interrupt(Vector2 screenPosition)
        {
            if (!Model.IsTrySpawning) return;

            Model.IsTrySpawning = false;
            Vector3 spawnPosition = cameraService.GetWorldPoint(screenPosition, spawnReinforcement.Position);
            bool canSpawn = spawnReinforcement.CanSpawn;
            
            if (canSpawn)
            {
                switch (currentSpawnType)
                {
                    case SpawnType.Ship:
                    {
                        ShipView ship = shipFacadeFactory.Create(PlayerType.Player, currentShipType, spawnPosition);
                        ship.OnRelease += HandleShipDestroying;
                        Model.AddUnitCapacity(currentShipType); 
                        break;
                    }
                    case SpawnType.MiningFacility:
                    {
                        miningFacilityFacade.Create(PlayerType.Player, currentFacilityType, spawnPosition);
                        break;
                    }
                    case SpawnType.DefendPlatform:
                    {
                        defendPlatformFacade.Create(PlayerType.Player, currentPlatformType, spawnPosition);
                        break;
                    }
                }
            }
            spawnReinforcement.Destroy();
            inputService.Block(false);
            Model.InvokeSpawnShipEvent(canSpawn);
        }

        private void HandleShipDestroying(ShipType shipType)
        {
            Model.RemoveUnitCapacity(shipType);
        }
        
        public void Tick()
        {
            if (!Model.IsTrySpawning) return;

            Vector3 position = cameraService.GetWorldPoint(inputService.TouchPosition, spawnReinforcement.Position);
            position.y = 0;
            spawnReinforcement.UpdatePosition(position);
        }

        public IChainHandler<UnitRequest> SetNext(IChainHandler<UnitRequest> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
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
          
            if (nextChain != null)
            {
                nextChain.Handle(request);
            }
        }

        public void TrySpawnReinforcement(string id)
        {
            if (Enum.TryParse(id, out ShipType shipType))
            {
                if (Model.CanSpawnUnit(shipType))
                {
                    StartSpawnSequence(SpawnType.Ship);
                    currentShipType = shipType;
                    spawnReinforcement = Object.Instantiate(Model.GetSpawnPrefab(shipType));
                }
            }
            else if (Enum.TryParse(id, out MiningFacilityType facilityType))
            {
                StartSpawnSequence(SpawnType.MiningFacility);
                currentFacilityType = facilityType;
                spawnReinforcement = Object.Instantiate(Model.GetSpawnPrefab(facilityType));
            }
            else if(Enum.TryParse(id, out DefendPlatformType defendPlatformType))
            {
                StartSpawnSequence(SpawnType.DefendPlatform);
                currentPlatformType = defendPlatformType;
                spawnReinforcement = Object.Instantiate(Model.GetSpawnPrefab(defendPlatformType));
            }
        }

        private void StartSpawnSequence(SpawnType spawnType)
        {
            currentSpawnType = spawnType;
            inputService.Block(true);
            Model.IsTrySpawning = true;
        }
    }
}