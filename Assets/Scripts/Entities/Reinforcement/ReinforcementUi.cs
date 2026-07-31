using System;
using System.Collections.Generic;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Patterns.Visitor;
using EmpireAtWar.Presenters.Reinforcement;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Views.Reinforcement
{
    public interface IReinforcementVisitor : IVisitor<ISpawnShipUi>
    {
        void OnRelease(ISpawnShipUi spawnShipUi);
    }

    public interface IReinforcementUi
    {
        void SetModel(IReinforcementModelObserver model);
        void SetPresenter(IReinforcementPresenter presenter);
        void SetData(ReinforcementData data);
        void SetParent(Transform parent);
        void Show();
        void Hide();
        void Initialize();
        void Dispose();
    }

    public class ReinforcementUi : BaseUi, IReinforcementUi, IReinforcementVisitor
    {
        private const string UNIT_CAPACITY_TEXT = "Reinforcement";

        [SerializeField] private Transform spawnTransform;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private TextMeshProUGUI unitCapacityText;

        private readonly Dictionary<string, ISpawnShipUi> _spawnUnitUiDictionary = new();

        private IReinforcementModelObserver _model;
        private IReinforcementPresenter _presenter;
        private ReinforcementData _data;
        private ISpawnShipUi _currentSpawnUnitUi;
        private bool _isInitialized;

        public void SetModel(IReinforcementModelObserver model)
        {
            _model = model;
        }

        public void SetPresenter(IReinforcementPresenter presenter)
        {
            _presenter = presenter;
        }

        public void SetData(ReinforcementData data)
        {
            _data = data;
        }

        public void Initialize()
        {
            if (_model == null || _presenter == null || _data == null)
            {
                throw new InvalidOperationException("Reinforcement UI dependencies must be set before initialization.");
            }

            unitCapacityText.text = $"{UNIT_CAPACITY_TEXT}: 0/{_model.MaxUnitCapacity}";

            closeButton.onClick.AddListener(_presenter.Hide);

            _model.OnSpawnUnit += HandleSpawning;
            _model.OnReinforcementAdded += AddUi;
            _model.OnCapacityChanged += UpdateCapacityData;
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            closeButton.onClick.RemoveListener(_presenter.Hide);

            _model.OnSpawnUnit -= HandleSpawning;
            _model.OnReinforcementAdded -= AddUi;
            _model.OnCapacityChanged -= UpdateCapacityData;
            _isInitialized = false;
        }

        private void OnDestroy()
        {
            Dispose();
        }

        private void AddUi(string key, FactionData factionData)
        {
            if (_spawnUnitUiDictionary.TryGetValue(key, out ISpawnShipUi shipUi))
            {
                shipUi.AddUnit();
            }
            else
            {
                ISpawnShipUi spawnShipUi = Instantiate(_data.ReinforcementButton, spawnTransform);
                spawnShipUi.Init(this, key, factionData);
                _spawnUnitUiDictionary.Add(key, spawnShipUi);

                if (Enum.TryParse(key, out ShipType result))
                {
                    ActivateShipUnitUi(result, spawnShipUi);
                }
            }
        }

        private void UpdateCapacityData(int capacity)
        {
            unitCapacityText.text = $"{UNIT_CAPACITY_TEXT}: {capacity}/{_model.MaxUnitCapacity}";

            foreach (KeyValuePair<string, ISpawnShipUi> entry in _spawnUnitUiDictionary)
            {
                if (Enum.TryParse(entry.Key, out ShipType result))
                {
                    ActivateShipUnitUi(result, entry.Value);
                }
            }
        }

        private void HandleSpawning(bool success)
        {
            if (success)
            {
                _currentSpawnUnitUi.DecreaseUnitCount();
            }

            _presenter.Show();
        }

        public override void Show()
        {
            base.Show();
            SetPanelVisibility(true);
        }

        public override void Hide()
        {
            SetPanelVisibility(false);
            base.Hide();
        }

        private void SetPanelVisibility(bool isVisible)
        {
            if (panelCanvasGroup == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ReinforcementUi)} requires a bound panel {nameof(CanvasGroup)}.");
            }

            panelCanvasGroup.alpha = isVisible ? 1f : 0f;
            panelCanvasGroup.interactable = isVisible;
            panelCanvasGroup.blocksRaycasts = isVisible;
        }

        public void Handle(ISpawnShipUi spawnShipUi)
        {
            if (_model.IsTrySpawning)
            {
                return;
            }

            _presenter.Hide();
            _currentSpawnUnitUi = spawnShipUi;
            _presenter.TrySpawnReinforcement(spawnShipUi.UnitType);
        }

        public void OnRelease(ISpawnShipUi spawnShipUi)
        {
            _spawnUnitUiDictionary.Remove(spawnShipUi.UnitType);
        }

        private void ActivateShipUnitUi(ShipType shipType, ISpawnShipUi shipUnitUi)
        {
            shipUnitUi.Activate(_model.CanSpawnUnit(shipType));
        }
    }
}
