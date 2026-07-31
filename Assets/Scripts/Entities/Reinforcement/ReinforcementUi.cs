using System;
using System.Collections.Generic;
using DG.Tweening;
using EmpireAtWar.Models.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Patterns.Visitor;
using EmpireAtWar.Presenters.Reinforcement;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utilities.ScriptUtils.Dotween;

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
        void Initialize();
        void Dispose();
    }

    public class ReinforcementUi : BaseUi, IReinforcementUi, IReinforcementVisitor
    {
        private const string UNIT_CAPACITY_TEXT = "Reinforcement";

        [SerializeField] private Transform spawnTransform;
        [SerializeField] private Button switchButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private Image signalImage;
        [SerializeField] private TextMeshProUGUI unitCapacityText;

        private readonly Dictionary<string, ISpawnShipUi> _spawnUnitUiDictionary = new();

        private IReinforcementModelObserver _model;
        private IReinforcementPresenter _presenter;
        private ReinforcementData _data;
        private ISpawnShipUi _currentSpawnUnitUi;
        private Sequence _fadeSequence;
        private Color _originColor;
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

            _originColor = signalImage.color;
            unitCapacityText.text = $"{UNIT_CAPACITY_TEXT}: 0/{_model.MaxUnitCapacity}";

            switchButton.onClick.AddListener(TogglePanel);
            closeButton.onClick.AddListener(HidePanel);

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

            switchButton.onClick.RemoveListener(TogglePanel);
            closeButton.onClick.RemoveListener(HidePanel);

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

            PlayTweens();
        }

        private void PlayTweens()
        {
            if (_fadeSequence.KillIfExist())
            {
                _fadeSequence.Append(signalImage.DOColor(_originColor, 0.1f));
            }

            _fadeSequence = DOTween.Sequence();
            _fadeSequence.Append(signalImage.DOColor(Color.green, 1f));
            _fadeSequence.Append(signalImage.DOColor(_originColor, 1f));
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

            TogglePanel();
        }

        private void TogglePanel()
        {
            if (panelCanvasGroup == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(ReinforcementUi)} requires a bound panel {nameof(CanvasGroup)}.");
            }

            SetPanelVisibility(!panelCanvasGroup.interactable);
        }

        private void HidePanel()
        {
            SetPanelVisibility(false);
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

            HidePanel();
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
