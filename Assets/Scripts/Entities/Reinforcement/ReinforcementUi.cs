using EmpireAtWar.Components.Ui.Tooltip;
using System;
using System.Collections.Generic;
using EmpireAtWar.Controllers.Factions;
using EmpireAtWar.Models.Reinforcement;
using EmpireAtWar.Entities.TutorialUi;
using EmpireAtWar.Patterns.Visitor;
using EmpireAtWar.Services.Tutorial;
using EmpireAtWar.Ui.Base;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EmpireAtWar.Entities.Reinforcement
{
    public class ReinforcementUi : BaseUi, IReinforcementVisitor
    {
        private IReinforcementModelObserver _model;
        private IReinforcementPresenter _presenter;

        [SerializeField] private Transform spawnTransform;
        [SerializeField] private ScrollRect unitScroll;
        [SerializeField] private TooltipHoverView tooltipHover;
        [SerializeField] private Button closeButton;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private TextMeshProUGUI unitCapacityText;
        [SerializeField] private UnityEngine.UI.Image capacityFill;
        [Tooltip("Marks the first unit card and the card that got a unit last, for the tutorial's drag steps.")]
        [SerializeField] private TutorialAnchor tutorialAnchor;
        private readonly Dictionary<UnitLimitKey, ISpawnShipUi> _spawnUnitUiDictionary = new();
        private readonly Dictionary<UnitLimitKey, int> _counts = new();
        private UnitLimitKey _newestKey;
        private bool _hasNewest;
        private ReinforcementData _data;

        private bool _isInitialized;

        public TooltipHoverView TooltipHover => tooltipHover;

        public void Initialize()
        {
            if (_model == null || _presenter == null || _data == null)
            {
                throw new InvalidOperationException("Reinforcement UI dependencies must be set before initialization.");
            }

            UpdateCapacityData(_model.CurrentUnitCapacity);

            closeButton.onClick.AddListener(_presenter.Hide);

            _model.OnPlacementEnded += HandlePlacementEnded;
            _model.OnReserveCountChanged += UpdateReserve;
            _model.OnCapacityChanged += UpdateCapacityData;
            foreach (KeyValuePair<UnitRequest, int> entry in _model.Reserve)
                UpdateReserve(entry.Key, entry.Value);
            _isInitialized = true;
        }

        public void Dispose()
        {
            if (!_isInitialized)
            {
                return;
            }

            closeButton.onClick.RemoveListener(_presenter.Hide);

            _model.OnPlacementEnded -= HandlePlacementEnded;
            _model.OnReserveCountChanged -= UpdateReserve;
            _model.OnCapacityChanged -= UpdateCapacityData;
            _isInitialized = false;
        }

        public void SetModel(IReinforcementModelObserver model)
        {
            _model = model;
        }

        public void AssignPresenter(IReinforcementPresenter presenter)
        {
            if (_presenter != null)
                throw new InvalidOperationException("ReinforcementUi presenter can only be assigned once.");
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

        private void UpdateReserve(UnitRequest request, int count)
        {
            UnitLimitKey key = UnitLimitKey.From(request);
            _counts.TryGetValue(key, out int previous);
            _counts[key] = count;
            if (count > previous)
            {
                _newestKey = key;
                _hasNewest = true;
            }
            if (_spawnUnitUiDictionary.TryGetValue(key, out ISpawnShipUi shipUi))
            {
                shipUi.SetUnitCount(count);
            }
            else if (count > 0)
            {
                ISpawnShipUi spawnShipUi = Instantiate(_data.ReinforcementButton, spawnTransform);
                spawnShipUi.Init(this, request, unitScroll);
                tooltipHover.Register(((SpawnShipUi)spawnShipUi).TooltipTrigger);
                _spawnUnitUiDictionary.Add(key, spawnShipUi);
                spawnShipUi.SetUnitCount(count);
                ActivateUnitUi(spawnShipUi);
            }
            RefreshTutorialMarks();
        }

        private void RefreshTutorialMarks()
        {
            SpawnShipUi first = null;
            foreach (ISpawnShipUi shipUi in _spawnUnitUiDictionary.Values)
            {
                SpawnShipUi card = (SpawnShipUi)shipUi;
                if (first == null || card.transform.GetSiblingIndex() < first.transform.GetSiblingIndex()) first = card;
            }
            Mark(TutorialAnchorId.ReinforcementUnit, first);
            Mark(TutorialAnchorId.ReinforcementNewUnit,
                _hasNewest && _spawnUnitUiDictionary.TryGetValue(_newestKey, out ISpawnShipUi newest)
                    ? (SpawnShipUi)newest
                    : null);
        }

        private void Mark(TutorialAnchorId id, SpawnShipUi card)
        {
            if (card == null) tutorialAnchor.Unmark(id);
            else tutorialAnchor.Mark(id, (RectTransform)card.transform);
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

        private void HandlePlacementEnded()
        {
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

            _presenter.TrySpawnReinforcement(spawnShipUi.Request);
            // A rejected drag keeps the panel open instead of flickering it closed and open again.
            if (_model.IsTrySpawning)
            {
                _presenter.Hide();
            }
        }

        public void OnRelease(ISpawnShipUi spawnShipUi)
        {
            _spawnUnitUiDictionary.Remove(UnitLimitKey.From(spawnShipUi.Request));
            RefreshTutorialMarks();
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
