using System;
using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Commands.Reinforcement;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.MiningFacility;
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

        private Dictionary<string, ISpawnShipUi> spawnUnitUiDictionary = new Dictionary<string, ISpawnShipUi>();
        private ISpawnShipUi currenSpawnUnitUi;
        private Sequence fadeSequence;
        private Color originColor;

        protected override void OnInitialize()
        {
            originColor = signalImage.color;
            unitCapacityText.text = $"{UnitCapacityText}: 0/{Model.MaxUnitCapacity}";
            
            switchButton.onClick.AddListener(ActivateCanvas);
            closeButton.onClick.AddListener(DisableCanvas);

            Model.OnSpawnUnit += HandleSpawning;
            Model.OnReinforcementAdded += AddUi;
            Model.OnFacilitiesAdded += AddUi;
            Model.OnCapacityChanged += UpdateCapacityData;
        }
  

        protected override void OnDispose()
        {
            switchButton.onClick.RemoveListener(ActivateCanvas);
            closeButton.onClick.RemoveListener(DisableCanvas);
            
            Model.OnSpawnUnit -= HandleSpawning;
            Model.OnReinforcementAdded -= AddUi;
            Model.OnFacilitiesAdded -= AddUi;
            Model.OnCapacityChanged -= UpdateCapacityData;
        }
        
        private void AddUi(MiningFacilityType miningFacilityType, FactionData factionData)
        {
            string key = miningFacilityType.ToString();

            if (spawnUnitUiDictionary.TryGetValue(key, out ISpawnShipUi shipUi))
            {
                shipUi.AddUnit();
            }
            else
            {
                ISpawnShipUi spawnShipUi = Instantiate(Model.ReinforcementButton, spawnTransform);
                spawnShipUi.Init(this, key, factionData);
                spawnUnitUiDictionary.Add(key, spawnShipUi);
            }
            PlayTweens();
        }
        
        private void AddUi(ShipType shipType, FactionData factionData)
        {
            string key = shipType.ToString();
            if (spawnUnitUiDictionary.TryGetValue(key, out ISpawnShipUi shipUi))
            {
                shipUi.AddUnit();
            }
            else
            {
                ISpawnShipUi spawnShipUi = Instantiate(Model.ReinforcementButton, spawnTransform);
                spawnShipUi.Init(this, key, factionData);
                ActiveShipUnitUi(shipType, spawnShipUi);
                spawnUnitUiDictionary.Add(key, spawnShipUi);
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
            
            foreach (KeyValuePair<string, ISpawnShipUi> keyValuePair in spawnUnitUiDictionary)
            {
                if (Enum.TryParse(keyValuePair.Key, out ShipType result))
                {
                    ActiveShipUnitUi(result, keyValuePair.Value);
                }
            }
        }
        
        private void HandleSpawning(bool success)
        {
            if (success)
            {
                currenSpawnUnitUi.DecreaseUnitCount();
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
            currenSpawnUnitUi = spawnShipUi;

            if (Enum.TryParse(spawnShipUi.UnitType, out ShipType result))
            {
                Command.TrySpawnShip(result);
            }
            else if (Enum.TryParse(spawnShipUi.UnitType, out MiningFacilityType facilityType))
            {
                Command.TrySpawnMiningFacility(facilityType);
            }
        }

        public void OnRelease(ISpawnShipUi spawnShipUi)
        {
            spawnUnitUiDictionary.Remove(spawnShipUi.UnitType);
        }

        private void ActiveShipUnitUi(ShipType shipType, ISpawnShipUi shipUnitUi)
        {
            shipUnitUi.Activate(Model.CanSpawnUnit(shipType));
        }
    }
}