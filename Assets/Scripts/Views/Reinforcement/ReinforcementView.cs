using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Patterns.Visitor;
using EmpireAtWar.Views.ViewImpl;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.Dotween;

namespace EmpireAtWar.Views.Reinforcement
{
    public interface IReinforcementVisitor:IVisitor<ISpawnShipUi>
    {
        void OnRelease(ISpawnShipUi spawnShipUi);
    }
    public class ReinforcementView:View<IReinforcementModelObserver, IReinforcementCommand>, IReinforcementVisitor
    {
        private const string UnitCapacityText = "Reinforcement: ";
        
        [SerializeField] private Transform spawnTransform;
        [SerializeField] private Button switchButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Canvas panelCanvas;
        [SerializeField] private Image signalImage;
        [SerializeField] private TextMeshProUGUI unitCapacityText;

        private Dictionary<ShipType, ISpawnShipUi> spawnShipUiDictionary = new Dictionary<ShipType, ISpawnShipUi>();
        private ISpawnShipUi currenSpawnShipUi;
        private Sequence fadeSequence;
        private Color originColor;

        protected override void OnInitialize()
        {
            originColor = signalImage.color;
            unitCapacityText.text = $"{UnitCapacityText}: 0/{Model.MaxUnitCapacity}";
            
            switchButton.onClick.AddListener(ActivateCanvas);
            closeButton.onClick.AddListener(DisableCanvas);

            Model.OnSpawnShip += HandleSpawning;
            Model.OnReinforcementAdded += AddUi;
            Model.OnCapacityChanged += UpdateCapacityData;
        }

        protected override void OnDispose()
        {
            switchButton.onClick.RemoveListener(ActivateCanvas);
            closeButton.onClick.RemoveListener(DisableCanvas);
            
            Model.OnSpawnShip -= HandleSpawning;
            Model.OnReinforcementAdded -= AddUi;
            Model.OnCapacityChanged -= UpdateCapacityData;
        }
        
        private void AddUi(ShipType shipType, FactionData factionData)
        {
            if (spawnShipUiDictionary.TryGetValue(shipType, out ISpawnShipUi shipUi))
            {
                shipUi.AddUnit();
            }
            else
            {
                ISpawnShipUi spawnShipUi = Instantiate(Model.ReinforcementButton, spawnTransform);
                spawnShipUi.Init(this, shipType, factionData);
                ActiveShipUnitUi(shipType, spawnShipUi);
                spawnShipUiDictionary.Add(spawnShipUi.ShipType, spawnShipUi);
            }
            PlayTweens();
        }

        private void PlayTweens()
        {
            if (fadeSequence.KillIfExist())
            {
                fadeSequence.Append(signalImage.DOColor(originColor, 0.1f));
            }
            fadeSequence = DOTween.Sequence();
            fadeSequence.Append(signalImage.DOColor(Color.green, 1f));
            fadeSequence.Append(signalImage.DOColor(originColor, 1f));
        }

        private void UpdateCapacityData(int capacity)
        {
            unitCapacityText.text = $"{UnitCapacityText}: {Model.CurrentUnitCapacity}/{Model.MaxUnitCapacity}";
            
            foreach (KeyValuePair<ShipType, ISpawnShipUi> keyValuePair in spawnShipUiDictionary)
            {
                ActiveShipUnitUi(keyValuePair.Key, keyValuePair.Value);
            }
        }
        
        private void HandleSpawning(bool success)
        {
            if (success)
            {
                currenSpawnShipUi.DecreaseUnitCount();
            }

            ActivateCanvas();
        }
        
        private void ActivateCanvas()
        {
            panelCanvas.enabled = !panelCanvas.enabled;
        }
        
        private void DisableCanvas()
        {
            panelCanvas.enabled = false;
        }

        public void Handle(ISpawnShipUi spawnShipUi)
        {
            if(Model.IsTrySpawning) return;

            DisableCanvas();
            currenSpawnShipUi = spawnShipUi;
            Command.TrySpawnShip(spawnShipUi.ShipType);
        }

        public void OnRelease(ISpawnShipUi spawnShipUi)
        {
            spawnShipUiDictionary.Remove(spawnShipUi.ShipType);
        }

        private void ActiveShipUnitUi(ShipType shipType, ISpawnShipUi shipUnitUi)
        {
            shipUnitUi.Activate(Model.CanSpawnUnit(shipType));
        }
    }
}