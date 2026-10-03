using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
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
        void Initialize();

        void Dispose();

        void SetModel(IReinforcementModelObserver model);

        void SetPresenter(IReinforcementPresenter presenter);

        void SetData(ReinforcementData data);

        void SetParent(Transform parent);

        void Show();

        void Hide();
    }

    public class ReinforcementUi : BaseUi, IReinforcementUi, IReinforcementVisitor, ITooltipHoverView
    {
        private IReinforcementModelObserver _model;
        private IReinforcementPresenter _presenter;
        private ISpawnShipUi _currentSpawnUnitUi;

        [SerializeField] private Transform spawnTransform;
        [SerializeField] private ScrollRect unitScroll;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private TextMeshProUGUI unitCapacityText;
        [SerializeField] private UnityEngine.UI.Image capacityFill;
        private readonly Dictionary<UnitLimitKey, ISpawnShipUi> _spawnUnitUiDictionary = new();
        private ReinforcementData _data;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_model == null || _presenter == null || _data == null)
            {
                throw new InvalidOperationException("Reinforcement UI dependencies must be set before initialization.");
            }

            UpdateCapacityData(0);

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

        private void OnDestroy()
        {
            Dispose();
        }

        private void AddUi(UnitRequest request)
        {
            UnitLimitKey key = UnitLimitKey.From(request);
            if (_spawnUnitUiDictionary.TryGetValue(key, out ISpawnShipUi shipUi))
            {
                shipUi.AddUnit();
            }
            else
            {
                ISpawnShipUi spawnShipUi = Instantiate(_data.ReinforcementButton, spawnTransform);
                spawnShipUi.Init(this, request, unitScroll);
                tooltipHover.Register(((SpawnShipUi)spawnShipUi).TooltipTrigger);
                _spawnUnitUiDictionary.Add(key, spawnShipUi);
                ActivateUnitUi(spawnShipUi);
            }
        }

        private void UpdateCapacityData(int capacity)
        {
            unitCapacityText.text = $"{capacity} / {_model.MaxUnitCapacity}";
            capacityFill.fillAmount = _model.MaxUnitCapacity > 0
                ? Mathf.Clamp01((float)capacity / _model.MaxUnitCapacity) : 0f;

            foreach (ISpawnShipUi spawnShipUi in _spawnUnitUiDictionary.Values)
            {
                ActivateUnitUi(spawnShipUi);
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
            _presenter.TrySpawnReinforcement(spawnShipUi.Request);
        }

        public void OnRelease(ISpawnShipUi spawnShipUi)
        {
            _spawnUnitUiDictionary.Remove(UnitLimitKey.From(spawnShipUi.Request));
        }

        // Structures have no unit capacity cost, so only ships and squadrons are gated.
        private void ActivateUnitUi(ISpawnShipUi spawnShipUi)
        {
            switch (spawnShipUi.Request)
            {
                case ShipUnitRequest shipUnitRequest:
                    spawnShipUi.Activate(_model.CanSpawnUnit(shipUnitRequest.Key));
                    break;
                case SquadronUnitRequest squadronUnitRequest:
                    spawnShipUi.Activate(_model.CanSpawnUnit(squadronUnitRequest.Key));
                    break;
            }
        }
    }
}
