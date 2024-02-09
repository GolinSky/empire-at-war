using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace EmpireAtWar.Views.Reinforcement
{
    public class ReinforcementView:View<IReinforcementModelObserver, IReinforcementCommand>, ITickable
    {
        [SerializeField] private Transform spawnTransform;
        [SerializeField] private Button switchButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Canvas panelCanvas;
        [SerializeField] private GameObject shipTest;
        
        private ReinforcementDraggable currentDraggable;

        protected override void OnInitialize()
        {
            switchButton.onClick.AddListener(ActivateCanvas);
            closeButton.onClick.AddListener(DisableCanvas);

            Model.OnSpawnShip += HandleSpawning;
            Model.OnReinforcementAdded += AddUi;
        }

        protected override void OnDispose()
        {
            switchButton.onClick.RemoveListener(ActivateCanvas);
            closeButton.onClick.RemoveListener(DisableCanvas);
            
            Model.OnSpawnShip -= HandleSpawning;
            Model.OnReinforcementAdded -= AddUi;
        }
        
        private void AddUi(ShipType shipType, Sprite sprite)
        {
            ReinforcementDraggable draggable = Instantiate(Model.ReinforcementButton, spawnTransform);
            draggable.Init(SpawnShip, shipType, sprite);
        }
        
        private void HandleSpawning(bool success)
        {
            if (success)
            {
                shipTest.SetActive(false);
                currentDraggable.Destroy();
            }
        }
        
        private void SpawnShip(ShipType shipType, ReinforcementDraggable draggable)
        {
            if(Model.IsTrySpawning) return;

            currentDraggable = draggable;
            Command.TrySpawnShip(shipType);
            shipTest.SetActive(true);
        }
        
        private void ActivateCanvas()
        {
            panelCanvas.enabled = !panelCanvas.enabled;
        }
        
        private void DisableCanvas()
        {
            panelCanvas.enabled = false;
        }

        public void Tick()
        {
            if(!Model.IsTrySpawning) return;

            shipTest.transform.position = Model.SpawnShipPosition;
        }
    }
}