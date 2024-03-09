using System;
using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Patterns.ChainOfResponsibility;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Views.Reinforcement;
using EmpireAtWar.Views.Ship;
using LightWeightFramework.Controller;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;


namespace EmpireAtWar.Controllers.Reinforcement
{
    public interface IReinforcementChain:IChainHandler<UnitRequest>
    {
        
    }
    public class ReinforcementController : Controller<ReinforcementModel>, IReinforcementCommand, ITickable,
        IInitializable, ILateDisposable, IReinforcementChain
    {
        private readonly InputService inputService;
        private readonly ICameraService cameraService;
        private readonly ShipFacadeFactory shipFacadeFactory;

        private IChainHandler<UnitRequest> nextChain;
        private UnitSpawnView spawnReinforcement;
        private ShipType currentShipType;

        public ReinforcementController(
            ReinforcementModel model,
            InputService inputService,
            ICameraService cameraService,
            ShipFacadeFactory shipFacadeFactory) : base(model)
        {
            this.inputService = inputService;
            this.cameraService = cameraService;
            this.shipFacadeFactory = shipFacadeFactory;
        }

        public void Initialize()
        {
            inputService.OnEndDrag += Interrupt;
        }
        
        public void LateDispose()
        {
            inputService.OnEndDrag -= Interrupt;
        }

        private void AddReinforcement(ShipUnitRequest shipUnitRequest)
        {
            Model.AddReinforcement(shipUnitRequest);
        }

        private void Interrupt(Vector2 screenPosition)
        {
            if (!Model.IsTrySpawning) return;

            Model.IsTrySpawning = false;
            Vector3 spawnPosition = cameraService.GetWorldPoint(screenPosition, spawnReinforcement.Position);
            bool canSpawn = spawnReinforcement.CanSpawn;
            Model.InvokeSpawnShipEvent(canSpawn);
            if (canSpawn)
            {
                shipFacadeFactory.Create(PlayerType.Player, currentShipType, spawnPosition);
            }
            spawnReinforcement.Destroy();
            inputService.Block(false);
        }

        public void TrySpawnShip(ShipType shipType)
        {
            currentShipType = shipType;
            inputService.Block(true);
            Model.IsTrySpawning = true;
            spawnReinforcement = Object.Instantiate(Model.GetSpawnPrefab(shipType));
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
                    AddReinforcement(shipUnitRequest);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request));
            }
          
            if (nextChain != null)
            {
                nextChain.Handle(request);
            }
        }
    }
}