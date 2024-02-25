using DG.Tweening;
using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.ScriptUtils.Dotween;
using EmpireAtWar.Views.ViewImpl;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Reinforcement
{
    public class ReinforcementView:View<IReinforcementModelObserver, IReinforcementCommand>
    {
        [SerializeField] private Transform spawnTransform;
        [SerializeField] private Button switchButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Canvas panelCanvas;
        [SerializeField] private Image signalImage;
        
        private ReinforcementDraggable currentDraggable;
        private Sequence fadeSequence;
        private Color originColor;

        protected override void OnInitialize()
        {
            switchButton.onClick.AddListener(ActivateCanvas);
            closeButton.onClick.AddListener(DisableCanvas);

            Model.OnSpawnShip += HandleSpawning;
            Model.OnReinforcementAdded += AddUi;
            originColor = signalImage.color;
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
            
            if (fadeSequence.KillIfExist())
            {
                fadeSequence.Append(signalImage.DOColor(originColor, 0.1f));
            }
            fadeSequence = DOTween.Sequence();
            fadeSequence.Append(signalImage.DOColor(Color.green, 1f));
            fadeSequence.Append(signalImage.DOColor(originColor, 1f));
        }
        
        private void HandleSpawning(bool success)
        {
            if (success)
            {
                currentDraggable.Destroy();
            }

            ActivateCanvas();
        }
        
        private void SpawnShip(ShipType shipType, ReinforcementDraggable draggable)
        {
            if(Model.IsTrySpawning) return;

            DisableCanvas();
            currentDraggable = draggable;
            Command.TrySpawnShip(shipType);
        }
        
        private void ActivateCanvas()
        {
            panelCanvas.enabled = !panelCanvas.enabled;
        }
        
        private void DisableCanvas()
        {
            panelCanvas.enabled = false;
        }
    }
}