using EmpireAtWar.Commands.Reinforcement;
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


namespace EmpireAtWar.Controllers.Reinforcement
{
    public interface IReinforcementChain:IChainHandler<ShipType>
    {
        
    }
    public class ReinforcementController : Controller<ReinforcementModel>, IReinforcementCommand, ITickable,
        IInitializable, ILateDisposable, IReinforcementChain
    {
        private readonly InputService inputService;
        private readonly ICameraService cameraService;
        private readonly ShipFacadeFactory shipFacadeFactory;

        private IChainHandler<ShipType> nextChain;
        private ShipSpawnView spawnReinforcement;
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

        private void AddReinforcement(ShipType shipType)
        {
            Model.AddReinforcement(shipType);
        }

        private void Interrupt(Vector2 screenPosition)
        {
            if (!Model.IsTrySpawning) return;

            Model.IsTrySpawning = false;
            Vector3 spawnPosition = cameraService.GetWorldPoint(screenPosition);
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

            Vector3 position = cameraService.GetWorldPoint(inputService.TouchPosition);
            position.y = 0;
            spawnReinforcement.UpdatePosition(position);
        }

        public IChainHandler<ShipType> SetNext(IChainHandler<ShipType> chainHandler)
        {
            nextChain = chainHandler;
            return nextChain;
        }

        public void Handle(ShipType request)
        {
            AddReinforcement(request);
            if (nextChain != null)
            {
                nextChain.Handle(request);
            }
        }
    }
}