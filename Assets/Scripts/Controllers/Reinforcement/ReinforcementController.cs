using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Services.Camera;
using EmpireAtWar.Services.InputService;
using EmpireAtWar.Services.Reinforcement;
using EmpireAtWar.Views.Ship;
using LightWeightFramework.Controller;
using UnityEngine;
using WorkShop.LightWeightFramework.Command;
using Zenject;


namespace EmpireAtWar.Controllers.Reinforcement
{
    public class ReinforcementController : Controller<ReinforcementModel>, IReinforcementCommand, ITickable,
        IInitializable, ILateDisposable
    {
        private readonly InputService inputService;
        private readonly ICameraService cameraService;
        private readonly ShipFacadeFactory shipFacadeFactory;
        private readonly IReinforcementService reinforcementService;

        private ShipType currentShipType;
        
        public ReinforcementController(
            ReinforcementModel model,
            InputService inputService,
            ICameraService cameraService,
            ShipFacadeFactory shipFacadeFactory,
            IReinforcementService reinforcementService) : base(model)
        {
            this.inputService = inputService;
            this.cameraService = cameraService;
            this.shipFacadeFactory = shipFacadeFactory;
            this.reinforcementService = reinforcementService;
        }

        public void Initialize()
        {
            inputService.OnEndDrag += Interrupt;
            reinforcementService.OnReinforcementAdded += AddReinforcement;
        }
        
        public void LateDispose()
        {
            inputService.OnEndDrag -= Interrupt;
            reinforcementService.OnReinforcementAdded -= AddReinforcement;
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
            shipFacadeFactory.Create(PlayerType.Player, currentShipType, spawnPosition);
            Model.InvokeSpawnShipEvent(true);
            inputService.Block(false);
        }

        public void TrySpawnShip(ShipType shipType)
        {
            currentShipType = shipType;
            inputService.Block(true);
            Model.IsTrySpawning = true;
        }

        public void Tick()
        {
            if (!Model.IsTrySpawning) return;

            Vector3 position = cameraService.GetWorldPoint(inputService.TouchPosition);
            position.y = 0;
            Model.SpawnShipPosition = position;
        }
    }
}